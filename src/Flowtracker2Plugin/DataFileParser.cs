using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using FieldDataPluginFramework.DataModel.Meters;
using FieldDataPluginFramework.DataModel.Readings;
using FieldDataPluginFramework.DataModel.Verticals;
using FieldDataPluginFramework.Results;
using FieldDataPluginFramework.Units;
using ICSharpCode.SharpZipLib.Zip;
using SonTek.Framework.Configuration;
using SonTek.Framework.Data;
using SonTek.Globals.Common;
using UnitConversion;

namespace FlowTracker2Plugin
{
    public class DataFileParser
    {
        private readonly ILog _log;
        private readonly IFieldDataResultsAppender _resultsAppender;

        public DataFileParser(ILog log, IFieldDataResultsAppender resultsAppender)
        {
            _log = log;
            _resultsAppender = resultsAppender;
        }

        public ParseFileResult Parse(Stream stream)
        {
            return Parse(stream, null);
        }

        private DataFile DataFile { get; set; }
        private UnitSystem UnitSystem { get; set; }
        private UnitConverter UnitConverter { get; set; }

        public ParseFileResult Parse(Stream stream, LocationInfo locationInfo)
        {
            DataFile = GetDataFile(stream);

            if (DataFile == null)
                return ParseFileResult.CannotParse();

            if (locationInfo == null)
            {
                locationInfo = _resultsAppender.GetLocationByIdentifier(DataFile.Properties.SiteNumber);
            }

            return AppendResults(locationInfo);
        }

        private DataFile GetDataFile(Stream stream)
        {
            try
            {
                // The file content stream provided by the framework is not a file stream.
                // The SonTek framework can only parse files from disk
                // So copy the stream to a temporary file
                using (var tempFile = new TempFile())
                {
                    var tempPath = tempFile.ToString();
                    var byteCount = (int) stream.Length;

                    using (var reader = new BinaryReader(stream))
                    using (var writer = new BinaryWriter(new FileStream(tempPath, FileMode.Create)))
                    {
                        writer.Write(reader.ReadBytes(byteCount));
                        writer.Close();

                        var dataFile = new DataFileComplete(tempPath).GetDataFile();

                        _log.Info(
                            $"Loaded {dataFile.Configuration.DataCollectionMode}.{dataFile.Configuration.Discharge.DischargeEquation} measurement from {dataFile.HandheldInfo.SerialNumber}/{dataFile.HandheldInfo.CpuSerialNumber}/{dataFile.HandheldInfo.SoftwareVersion}");

                        return dataFile;
                    }
                }
            }
            catch (Exception exception)
            {
                if (IsNotAZipArchiveException(exception) || IsInvalidFlowTrackerArchive(exception))
                {
                    // Stop parsing silently on the expected failure cases
                    return null;
                }

                throw;
            }
        }

        private static bool IsNotAZipArchiveException(Exception exception)
        {
            return exception is ZipException &&
                   exception.Message.StartsWith("Wrong Local header signature:", StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsInvalidFlowTrackerArchive(Exception exception)
        {
            return exception is IOException &&
                   exception.Message.Equals("Not found", StringComparison.InvariantCultureIgnoreCase);
        }

        private void LogException(string message, Exception exception)
        {
            _log.Error($"{message}: {exception.Message}\n{exception.StackTrace}");

            if (exception.InnerException != null)
            {
               LogException("InnerException", exception.InnerException); 
            }
        }

        private ParseFileResult AppendResults(LocationInfo locationInfo)
        {
            try
            {
                UnitSystem = CreateUnitSystem();

                var visit = CreateVisit(locationInfo);

                var dischargeActivity = CreateDischargeActivity(visit);

                AddGageHeightMeasurement(dischargeActivity);

                var manualGauging = CreateManualGauging(dischargeActivity);

                ValidateStartAndEndStations(out var startStationType, out var endStationType);

                foreach (var station in DataFile.Stations)
                {
                    manualGauging.Verticals.Add(CreateVertical(station, startStationType, endStationType));
                }

                _resultsAppender.AddDischargeActivity(visit, dischargeActivity);

                AddTemperatureReadings(visit);

                return ParseFileResult.SuccessfullyParsedAndDataValid();
            }
            catch (Exception exception)
            {
                // Something has gone sideways rather hard. The framework won't log the exception's stack trace
                // so we explicitly do that here, to help track down any bugs.
                LogException("Parsing error", exception);
                return ParseFileResult.SuccessfullyParsedButDataInvalid(exception);
            }
        }

        private UnitSystem CreateUnitSystem()
        {
            UnitConverter = new UnitConverter(!IsMetric());

            return new UnitSystem
            {
                DistanceUnitId = GetUnitId(UnitConverter.DistanceUnitGroup),
                AreaUnitId = GetUnitId(UnitConverter.AreaUnitGroup),
                VelocityUnitId = GetUnitId(UnitConverter.VelocityUnitGroup),
                DischargeUnitId = GetUnitId(UnitConverter.DischargeUnitGroup),
            };
        }

        private string GetUnitId(string unitGroup)
        {
            var unit = UnitConverter.Units[unitGroup];

            return UnitConverter.IsImperial
                ? unit.ImperialId
                : unit.MetricId;
        }

        private static bool IsMetric()
        {
            return File.Exists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Aquatic Informatics\AQUARIUS Server\FieldDataPlugins\FlowTracker2\CreateMetricMeasurements.txt"));
        }

        private FieldVisitInfo CreateVisit(LocationInfo locationInfo)
        {
            var fieldVisitPeriod = new DateTimeInterval(DataFile.Properties.StartTime, DataFile.Properties.EndTime);
            var visitDetails = new FieldVisitDetails(fieldVisitPeriod)
            {
                Party = DataFile.Properties.Operator
            };

            return _resultsAppender.AddFieldVisit(locationInfo, visitDetails);
        }

        private DischargeActivity CreateDischargeActivity(FieldVisitInfo visit)
        {
            var dischargeActivityFactory = new DischargeActivityFactory(UnitSystem)
            {
                DefaultParty = DataFile.Properties.Operator
            };

            var dischargeActivity = dischargeActivityFactory.CreateDischargeActivity(
                new DateTimeInterval(visit.StartDate, visit.EndDate), UnitConverter.ConvertDischarge(DataFile.Calculations.Discharge));
            dischargeActivity.Comments = DataFile.Properties.Comment;

            return dischargeActivity;
        }

        private void ValidateStartAndEndStations(out StationType startStationType, out StationType endStationType)
        {
            startStationType = DataFile.Stations.First().StationType;
            endStationType = DataFile.Stations.Last().StationType;

            if (!ValidBankTypes.Contains(startStationType) || !ValidBankTypes.Contains(endStationType) || startStationType == endStationType)
                throw new Exception($"Measurements must start and end at a bank. StartStationType={startStationType} EndStationType={endStationType}");
        }

        private static readonly HashSet<StationType> ValidBankTypes = new HashSet<StationType>
        {
            StationType.RightBank,
            StationType.LeftBank
        };

        private void AddTemperatureReadings(FieldVisitInfo visit)
        {
            const string waterTemperatureParameterId = "TW";

            var temperatureUnitId = GetUnitId(UnitConverter.TemperatureUnitGroup);
            var temperatureValue = UnitConverter.ConvertTemperature(DataFile.Calculations.Temperature);

            var visitDuration = visit.EndDate - visit.StartDate;
            var midVisitTime = visit.StartDate + TimeSpan.FromTicks(visitDuration.Ticks / 2);

            var temperatureReading = new Reading(waterTemperatureParameterId,
                    new Measurement(temperatureValue, temperatureUnitId))
            {
                MeasurementDevice = new MeasurementDevice("SonTek", "ProbeModel", "ProbeSerial"),
                DateTimeOffset = midVisitTime
            };

            _resultsAppender.AddReading(visit, temperatureReading);
        }

        private ManualGaugingDischargeSection CreateManualGauging(DischargeActivity dischargeActivity)
        {
            var manualGaugingDischargeSectionFactory = new ManualGaugingDischargeSectionFactory(UnitSystem);

            var manualGauging =
                manualGaugingDischargeSectionFactory.CreateManualGaugingDischargeSection(
                    dischargeActivity.MeasurementPeriod,
                    UnitConverter.ConvertDischarge(dischargeActivity.Discharge.Value));

            manualGauging.AreaValue = UnitConverter.ConvertArea(DataFile.Calculations.Area);
            manualGauging.WidthValue = UnitConverter.ConvertDistance(DataFile.Calculations.Width);
            manualGauging.VelocityAverageValue = UnitConverter.ConvertVelocity(DataFile.Calculations.Velocity.X);
            manualGauging.StartPoint = DataFile.Stations.First().StationType == StationType.RightBank
                ? StartPointType.RightEdgeOfWater
                : StartPointType.LeftEdgeOfWater;
            manualGauging.VelocityObservationMethod = FindMostCommonVelocityMethod();
            manualGauging.DischargeMethod = CreateDischargeMethodType();

            dischargeActivity.ChannelMeasurements.Add(manualGauging);

            return manualGauging;
        }

        private void AddGageHeightMeasurement(DischargeActivity dischargeActivity)
        {
            var gaugeHeightMeasurements = DataFile.SupplementalData.Where(sd => !double.IsNaN(sd.GaugeHeight)).ToList();

            if (!gaugeHeightMeasurements.Any())
                return;

            foreach (var gaugeHeightMeasurement in gaugeHeightMeasurements)
            {
                dischargeActivity.GageHeightMeasurements.Add(
                    new GageHeightMeasurement(
                        new Measurement(UnitConverter.ConvertDistance(gaugeHeightMeasurement.GaugeHeight), UnitSystem.DistanceUnitId),
                        gaugeHeightMeasurement.Time));
            }
        }

        private DischargeMethodType CreateDischargeMethodType()
        {
            var dischargeEquation = DataFile.Configuration.Discharge.DischargeEquation;

            switch (dischargeEquation)
            {
                case DischargeEquation.MeanSection:
                    return DischargeMethodType.MeanSection;

                case DischargeEquation.MidSection:
                    return DischargeMethodType.MidSection;
            }

            throw new ArgumentException($"DischargeEquation='{dischargeEquation}' is not supported");
        }

        private Vertical CreateVertical(Station station, StationType startStationType, StationType endStationType)
        {
            var verticalType = station.StationType == startStationType
                ? VerticalType.StartEdgeNoWaterBefore
                : station.StationType == endStationType
                    ? VerticalType.EndEdgeNoWaterAfter
                    : VerticalType.MidRiver; // IslandEdge, OpenWater, and Ice all map to MidRiver

            var vertical = new Vertical
            {
                TaglinePosition = station.Location,
                Comments = station.Comment,
                MeasurementTime = station.CreationTime,
                EffectiveDepth = UnitConverter.ConvertDistance(station.GetEffectiveDepth()),
                SoundedDepth = UnitConverter.ConvertDistance(station.GetFinalDepth()),
                MeasurementConditionData = CreateMeasurementCondition(station),
                VelocityObservation = new VelocityObservation
                {
                    VelocityObservationMethod = GetPointVelocityObservationType(station.VelocityMethod),
                    MeterCalibration = CreateMeterCalibration(station),
                    MeanVelocity = UnitConverter.ConvertVelocity(station.Calculations.MeanVelocityInVertical.X),
                    DeploymentMethod = DeploymentMethodType.Unspecified,
                },
                FlowDirection = FlowDirectionType.Normal,
                VerticalType = verticalType,
                Segment = new Segment
                {
                    Width = UnitConverter.ConvertDistance(station.Calculations.Width),
                    Area = UnitConverter.ConvertArea(station.Calculations.Area),
                    Discharge = UnitConverter.ConvertDischarge(station.Calculations.Discharge),
                    TotalDischargePortion = 100 * station.Calculations.FractionOfTotalDischarge,
                    Velocity = UnitConverter.ConvertVelocity(station.Calculations.MeanVelocityInVertical.X)
                }
            };

            foreach (var pointMeasurement in station.PointMeasurements)
            {
                vertical.VelocityObservation.Observations.Add(new VelocityDepthObservation
                {
                    Depth = pointMeasurement.FractionalDepth * vertical.EffectiveDepth, // already unit-converted
                    Velocity = UnitConverter.ConvertVelocity(pointMeasurement.Calculations.Velocity.X),
                    ObservationInterval = (pointMeasurement.EndTime - pointMeasurement.StartTime).TotalSeconds,
                    RevolutionCount = 0
                });
            }

            if (!vertical.VelocityObservation.Observations.Any())
            {
                // IslandEdge stations or just plain surface points with no depth
                vertical.VelocityObservation.VelocityObservationMethod = PointVelocityObservationType.Surface;
                vertical.VelocityObservation.Observations.Add(new VelocityDepthObservation
                {
                    Depth = 0,
                    Velocity = 0,
                    ObservationInterval = 0,
                    RevolutionCount = 0
                });
            }

            return vertical;
        }

        private MeasurementConditionData CreateMeasurementCondition(Station station)
        {
            return station.StationType == StationType.Ice
                ? CreateIceCoveredData(station)
                : new OpenWaterData();
        }

        private MeasurementConditionData CreateIceCoveredData(Station station)
        {
            var iceThickness = double.IsNaN(station.IceThickness)
                ? 0
                : station.IceThickness;
            var waterSurfaceToBottomOfIce = double.IsNaN(station.WaterSurfaceToBottomOfIce)
                ? iceThickness
                : station.WaterSurfaceToBottomOfIce;
            var waterSurfaceToBottomOfSlush = double.IsNaN(station.WaterSurfaceToBottomOfSlush)
                ? waterSurfaceToBottomOfIce
                : station.WaterSurfaceToBottomOfSlush;

            return new IceCoveredData
            {
                WaterSurfaceToBottomOfIce = UnitConverter.ConvertDistance(waterSurfaceToBottomOfIce),
                WaterSurfaceToBottomOfSlush = UnitConverter.ConvertDistance(waterSurfaceToBottomOfSlush),
                IceThickness = UnitConverter.ConvertDistance(iceThickness)
            };
        }

        private MeterCalibration CreateMeterCalibration(Station station)
        {
            var point = station.PointMeasurements.FirstOrDefault();

            var meterCalibration = new MeterCalibration
            {
                MeterType = MeterType.Adv,
                Manufacturer = "SonTek",
                Model = "FlowTracker2",
                Configuration = $"{DataFile.HandheldInfo.SerialNumber}/{DataFile.HandheldInfo.CpuSerialNumber}",
                SoftwareVersion = point?.HandheldInfo.SoftwareVersion,
                FirmwareVersion = point?.ProbeInfo.FirmwareVersion,
                SerialNumber = point?.ProbeInfo.SerialNumber ?? DataFile.HandheldInfo.SerialNumber,
            };

            meterCalibration.Equations.Add(new MeterCalibrationEquation
            {
                InterceptUnitId = UnitSystem.DistanceUnitId
            });

            return meterCalibration;
        }

        private PointVelocityObservationType FindMostCommonVelocityMethod()
        {
            var velocityMethodCounts = new Dictionary<VelocityMethod, int>();

            foreach (var velocityMethod in DataFile.Stations.Select(station => station.VelocityMethod))
            {
                if (velocityMethodCounts.ContainsKey(velocityMethod))
                {
                    velocityMethodCounts[velocityMethod] += 1;
                }
                else
                {
                    velocityMethodCounts[velocityMethod] = 1;
                }
            }

            var maxVelocityMethod = velocityMethodCounts
                .First(kvp => kvp.Value == velocityMethodCounts.Max(kvp2 => kvp2.Value))
                .Key;

            return GetPointVelocityObservationType(maxVelocityMethod);
        }

        private PointVelocityObservationType GetPointVelocityObservationType(VelocityMethod velocityMethod)
        {
            return VelocityMethodMap.ContainsKey(velocityMethod)
                ? VelocityMethodMap[velocityMethod]
                : PointVelocityObservationType.Unknown;
        }

        private static readonly Dictionary<VelocityMethod, PointVelocityObservationType> VelocityMethodMap =
            new Dictionary<VelocityMethod, PointVelocityObservationType>
            {
                {VelocityMethod.FiveTenths, PointVelocityObservationType.OneAtPointFive},
                {VelocityMethod.SixTenths, PointVelocityObservationType.OneAtPointSix},
                {VelocityMethod.TwoTenthsEightTenths, PointVelocityObservationType.OneAtPointTwoAndPointEight},
                {VelocityMethod.TwoTenthsSixTenthsEightTenths, PointVelocityObservationType.OneAtPointTwoPointSixAndPointEight},
                {VelocityMethod.FivePoint, PointVelocityObservationType.FivePoint},
                {VelocityMethod.SixPoint, PointVelocityObservationType.SixPoint},
            };
    }
}
