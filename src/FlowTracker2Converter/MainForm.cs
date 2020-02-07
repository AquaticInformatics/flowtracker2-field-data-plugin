using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Microsoft.Win32;
using SonTek.Framework.Configuration;
using SonTek.Framework.Data;
using UnitConversion;

namespace FlowTracker2Converter
{
    public partial class MainForm : Form
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MainForm()
        {
            InitializeComponent();

            // ReSharper disable once VirtualMemberCallInConstructor
            Text = $@"FlowTracker2Converter v{GetExecutingFileVersion()}";
        }

        private static string GetExecutingFileVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            return fileVersionInfo.FileVersion;
        }

        private void Info(string message)
        {
            Log.Info(message);
            WriteLine($"INFO: {message}");
        }

        private void Warn(string message)
        {
            Log.Warn(message);
            WriteLine($"WARN: {message}");
        }

        private void Error(string message)
        {
            Log.Error(message);
            WriteLine($"ERROR: {message}");
        }

        private void Error(Exception exception)
        {
            Log.Error(exception);
            WriteLine($"ERROR: {exception.Message}\n{exception.StackTrace}");
        }

        private void WriteLine(string message)
        {
            var text = outputTextBox.Text;

            if (!string.IsNullOrEmpty(text))
                text += "\r\n";

            text += message;

            outputTextBox.Text = text;
            KeepOutputVisible();
        }

        private void KeepOutputVisible()
        {
            outputTextBox.SelectionStart = outputTextBox.TextLength;
            outputTextBox.SelectionLength = 0;
            outputTextBox.ScrollToCaret();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            outputTextBox.Text = string.Empty;
            KeepOutputVisible();
        }

        private const string ConverterKeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Aquatic Informatics\FlowTracker2Converter";
        private const string LicenseAgreementValueName = "LicenseAgreement";

        private bool IsLicenseAccepted()
        {
            return !string.IsNullOrEmpty((string)Registry.GetValue(
                ConverterKeyPath,
                LicenseAgreementValueName,
                null));
        }

        private void SetLicenseAcceptanceStatus(bool accepted)
        {
            Registry.SetValue(
                ConverterKeyPath,
                LicenseAgreementValueName,
                accepted ? "Accepted" : string.Empty);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            licenseCheckBox.Checked = IsLicenseAccepted();

            UpdateLicensedControls();
        }

        private void licenseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SetLicenseAcceptanceStatus(licenseCheckBox.Checked);

            UpdateLicensedControls();
        }

        private void UpdateLicensedControls()
        {
            if (licenseCheckBox.Checked)
            {
                convertButton.Enabled = true;
                return;
            }

            Warn("You will need to accept the license terms to use this tool.");
            convertButton.Enabled = false;
        }

        private void viewLicenseButton_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/AquaticInformatics/flowtracker2-field-data-plugin/blob/master/src/FlowTracker2Converter/Readme.md");
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!licenseCheckBox.Checked) return;

            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] paths)) return;

            foreach (var path in paths)
            {
                TryConvertFile(path);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effect = licenseCheckBox.Checked && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Link
                : DragDropEffects.None;
        }

        private void OnConvertClicked(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                Multiselect = true,
                Filter = @"FlowTracker2 files (*.ft)|*.ft|All Files(*.*)|*.*",
                Title = @"Select the FlowTracker2 file to convert"
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var path in fileDialog.FileNames)
                {
                    TryConvertFile(path);
                }
            }
        }

        private void TryConvertFile(string path)
        {
            try
            {
                ConvertFile(path);
            }
            catch (ExpectedException exception)
            {
                Error(exception.Message);
            }
            catch (Exception exception)
            {
                Error(exception);
            }
        }

        private void ConvertFile(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new ExpectedException($"File '{sourcePath} does not exist.");

            Info($"Loading '{sourcePath}' ...");

            var dataFile = LoadDataFile(sourcePath);

            var targetPath = Path.ChangeExtension(sourcePath, ".dis");

            if (File.Exists(targetPath))
            {
                if (!ConfirmAction($"Overwrite existing file?\n\n{targetPath}"))
                    return;

                Warn($"Overwriting existing file '{targetPath}.");
            }

            var text = ConvertToDis(dataFile, sourcePath);

            // ReSharper disable once AssignNullToNotNullAttribute
            File.WriteAllText(targetPath, text);

            Info($"Successfully converted '{targetPath}'.");
        }

        private DataFile LoadDataFile(string path)
        {
            try
            {
                return new DataFileComplete(path)
                    .GetDataFile();
            }
            catch (ZipException)
            {
                // Not a ZIP archive
                throw new ExpectedException($"'{path}' is not a FlowTracker2 file.");
            }
            catch (IOException)
            {
                // Not a FlowTracker2 archive
                throw new ExpectedException($"'{path}' is not a FlowTracker2 file.");
            }
        }

        private bool ConfirmAction(string message)
        {
            var result = MessageBox.Show(this, message, @"Are you sure?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        private bool IsMetric(DataFile dataFile)
        {
            const string metricUnits = "Metric";

            return dataFile.HandheldInfo.Settings?.GetString("Units", metricUnits)
                       .Equals(metricUnits, StringComparison.InvariantCultureIgnoreCase)
                   ?? true;
        }

        private string ConvertToDis(DataFile dataFile, string sourcePath)
        {
            var sb = new StringBuilder();

            var isImperial = !IsMetric(dataFile);
            var converter = new UnitConverter(isImperial);

            var distanceUnits = GetUnitId(converter, UnitConverter.DistanceUnitGroup);
            var areaUnits = GetUnitId(converter, UnitConverter.AreaUnitGroup);
            var velocityUnits = GetUnitId(converter, UnitConverter.VelocityUnitGroup);
            var dischargeUnits = GetUnitId(converter, UnitConverter.DischargeUnitGroup);
            var temperatureUnits = GetUnitId(converter, UnitConverter.TemperatureUnitGroup);

            var totalDischarge = converter.ConvertDischarge(dataFile.Calculations.Discharge);

            sb.AppendLine();
            var utcOffset = dataFile.HandheldInfo.Settings?.GetTimeSpan("LocalTimeOffsetFromUtc") ?? TimeSpan.Zero;
            var startTime = CreateDateTimeOffset(dataFile.Stations.First().CreationTime, utcOffset);
            AppendValue(sb, "File_Name", Path.GetFileName(sourcePath));
            AppendValue(sb, "Start_Date_and_Time", $"{startTime:yyyy/MM/dd HH:mm:ss}");
            AppendValue(sb, "Site_Name", string.IsNullOrEmpty(dataFile.Properties.SiteNumber) ? "Unknown" : dataFile.Properties.SiteNumber);
            AppendValue(sb, "Operator(s)", dataFile.Properties.Operator);
            AppendValue(sb, "Sensor_Type", $"{dataFile.Properties.CalculationsEngine}");
            AppendValue(sb, "Serial_#", $"{dataFile.HandheldInfo.SerialNumber}/{dataFile.HandheldInfo.CpuSerialNumber}");
            AppendValue(sb, "Software_Ver", $"FlowTracker2Converter {GetExecutingFileVersion()}");

            var equation = dataFile.Configuration.Discharge.DischargeEquation == DischargeEquation.MidSection
                ? "Mid-Section"
                : "Mean-Section";
            var startEdge = dataFile.Stations.First().StationType == StationType.LeftBank
                ? "LEW"
                : "REW";
            AppendValue(sb, "Unit_System", isImperial? "English Units" : "Metric Units");
            AppendValue(sb, "Discharge_Equation", $"{equation}");
            AppendValue(sb, "Start_Edge", startEdge);
            AppendValue(sb, "#_Stations", $"{dataFile.Stations.Count}");
            AppendValue(sb, "Total_Width", $"{converter.ConvertDistance(dataFile.Calculations.Width):F3} {distanceUnits}");
            AppendValue(sb, "Total_Area", $"{converter.ConvertArea(dataFile.Calculations.Area):F3} {areaUnits}");
            AppendValue(sb, "Total_Discharge", $"{totalDischarge:F4} {dischargeUnits}");
            AppendValue(sb, "Mean_Depth", $"{converter.ConvertDistance(dataFile.Calculations.Depth):F3} {distanceUnits}");
            AppendValue(sb, "Mean_Velocity", $"{converter.ConvertVelocity(dataFile.Calculations.Velocity.X):F4} {velocityUnits}");
            AppendValue(sb, "Mean_Temp", $"{converter.ConvertTemperature(dataFile.Calculations.Temperature):F2} {temperatureUnits}");

            sb.AppendLine();
            sb.AppendLine("Discharge_Uncertainty_(ISO)");
            AppendPercentage(sb, "Overall", dataFile.Calculations.UncertaintyIso.Overall);
            AppendPercentage(sb, "Accuracy", dataFile.Calculations.UncertaintyIso.Accuracy);
            AppendPercentage(sb, "Depth", dataFile.Calculations.UncertaintyIso.Depth);
            AppendPercentage(sb, "Velocity", dataFile.Calculations.UncertaintyIso.Velocity);
            AppendPercentage(sb, "Width", dataFile.Calculations.UncertaintyIso.Width);
            AppendPercentage(sb, "Method", dataFile.Calculations.UncertaintyIso.Method);
            AppendPercentage(sb, "#_Stations", dataFile.Calculations.UncertaintyIso.NumberOfStations);

            sb.AppendLine();
            sb.AppendLine("Discharge_Uncertainty_(Statistical)");
            AppendPercentage(sb, "Overall", dataFile.Calculations.UncertaintyIve.Overall);
            AppendPercentage(sb, "Accuracy", dataFile.Calculations.UncertaintyIve.Accuracy);
            AppendPercentage(sb, "Depth", dataFile.Calculations.UncertaintyIve.Depth);
            AppendPercentage(sb, "Velocity", dataFile.Calculations.UncertaintyIve.Velocity);
            AppendPercentage(sb, "Width", dataFile.Calculations.UncertaintyIve.Width);

            var gaugeHeightMeasurements = dataFile.SupplementalData.Where(sd => !double.IsNaN(sd.GaugeHeight)).ToList();

            if (gaugeHeightMeasurements.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Supplemental_Data");

                var gaugeHeightTable = new TextTable(7, 12, 9, 14, 19, 18, 10);

                gaugeHeightTable.AddRow(
                    "Record",
                    "Date",
                    "Time",
                    $"Location({distanceUnits})",
                    $"Gauge_Height({distanceUnits})",
                    $"Rated_Flow({dischargeUnits})",
                    "Comments");

                for(var i = 0; i < gaugeHeightMeasurements.Count; ++i)
                {
                    var gaugeHeight = gaugeHeightMeasurements[i];
                    var ratedFlow = !double.IsNaN(gaugeHeight.RatedDischarge)
                        ? $"{converter.ConvertDischarge(gaugeHeight.RatedDischarge):F3}"
                        : "()";
                    var dummyLocation = "()";
                    var gaugeHeightTime = CreateDateTimeOffset(gaugeHeight.Time, utcOffset);

                    gaugeHeightTable.AddRow(
                        $"{1+i}",
                        $"{gaugeHeightTime:yyyy/MM/dd}",
                        $"{gaugeHeightTime:HH:mm:ss}",
                        dummyLocation,
                        $"{converter.ConvertDistance(gaugeHeight.GaugeHeight):F3}",
                        ratedFlow);
                }

                sb.Append(gaugeHeightTable.Format());
            }

            sb.AppendLine();
            var stationTable = new TextTable(-2, 7, 8, 7, 7, 5, 7, 5, 6, 8, 6, 6, 8, 4, 8, 9, 8, 7, 9, 5);

            stationTable.AddRow(
                "St",
                "Clock",
                "Loc",
                "Depth",
                "IceD",
                "%Dep",
                "MeasD",
                "Npts",
                "Spike",
                "Vel",
                "SNR",
                "Angle",
                "Verr",
                "Bnd",
                "Temp",
                "CorrFact",
                "MeanV",
                "Area",
                "Flow",
                "%Q");

            stationTable.AddRow(
                "()",
                "()",
                $"({distanceUnits})",
                $"({distanceUnits})",
                $"({distanceUnits})",
                "(*D)",
                $"({distanceUnits})",
                "()",
                "()",
                $"({velocityUnits})",
                "(dB)",
                "(deg)",
                $"({velocityUnits})",
                "()",
                $"({temperatureUnits})",
                "()",
                $"({velocityUnits})",
                $"({areaUnits})",
                $"({dischargeUnits})",
                "(%)");

            for (var i = 0; i < dataFile.Stations.Count; ++i)
            {
                var station = dataFile.Stations[i];
                var time = CreateDateTimeOffset(station.CreationTime, utcOffset);

                if (!VelocityMethods.TryGetValue(station.VelocityMethod, out var method))
                {
                    method = "0.0"; // This gets treated as "Other" by 3.X
                }

                var calc = station.Calculations;
                var ice = Sanitize(station.IceThickness);
                var angle = Sanitize(calc.VelocityAngle);
                var snr = Sanitize(calc.Snr.Beam1);
                var vErr = Sanitize(calc.VelocityStandardError.X);
                var temp = Sanitize(calc.Temperature);
                var dummyBand = "0";

                var effectiveDepth = converter.ConvertDistance(station.GetEffectiveDepth());
                var fractionalDepth = double.TryParse(method, NumberStyles.Any, CultureInfo.InvariantCulture, out var fraction)
                        ? fraction
                        : 0;

                var segmentDischarge = converter.ConvertDischarge(calc.Discharge);
                var totalDischargePortion = double.IsNaN(calc.FractionOfTotalDischarge)
                    ? 100 * segmentDischarge / totalDischarge
                    : 100 * calc.FractionOfTotalDischarge;

                stationTable.AddRow(
                    $"{i:D2}",
                    $"{time:HH:mm}",
                    $"{converter.ConvertDistance(station.Location):F2}",
                    $"{effectiveDepth:F3}",
                    $"{converter.ConvertDistance(ice):F3}",
                    $"{method}",
                    $"{effectiveDepth - fractionalDepth*effectiveDepth:F3}",
                    $"{calc.Samples}",
                    $"{calc.Spikes}",
                    $"{converter.ConvertVelocity(calc.MeanVelocityInVertical.X):F3}",
                    $"{snr:F1}",
                    $"{angle:F0}",
                    $"{vErr:F4}",
                    $"{dummyBand}",
                    $"{converter.ConvertTemperature(temp):F2}",
                    $"{station.CorrectionFactor:F2}",
                    $"{converter.ConvertVelocity(calc.MeanPanelVelocity.X):F4}",
                    $"{converter.ConvertArea(calc.Area):F3}",
                    $"{segmentDischarge:F4}",
                    $"{totalDischargePortion:F1}");
            }

            sb.Append(stationTable.Format());

            return sb.ToString();
        }

        private static string GetUnitId(UnitConverter converter, string unitGroup)
        {
            var unit = UnitConverter.Units[unitGroup];

            return converter.IsImperial
                ? unit.ImperialId
                : unit.MetricId;
        }

        private static DateTimeOffset CreateDateTimeOffset(DateTime dateTime, TimeSpan utcOffset)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return new DateTimeOffset(dateTime) + utcOffset;

            // If not UTC, then assume the location local time
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), utcOffset);
        }

        private static readonly Dictionary<VelocityMethod, string> VelocityMethods =
            new Dictionary<VelocityMethod, string>
            {
                {VelocityMethod.FiveTenths, "0.5"},
                {VelocityMethod.SixTenths, "0.6"},
                {VelocityMethod.TwoTenthsEightTenths, "o2"},
                {VelocityMethod.TwoTenthsSixTenthsEightTenths, "o4"},
            };

        private static double Sanitize(double value)
        {
            return double.IsNaN(value)
                ? 0.0
                : value;
        }

        private void AppendValue(StringBuilder sb, string name, string value = null)
        {
            sb.AppendLine($"{name,-34}{value}");
        }

        private void AppendPercentage(StringBuilder sb, string name, double value)
        {
            AppendValue(sb, name, $"{100 * value:F1} %");
        }
    }
}
