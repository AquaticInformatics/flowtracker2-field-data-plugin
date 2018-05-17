using System.Collections.Generic;

namespace UnitConversion
{
    public class UnitConverter
    {
        public class Unit
        {
            public string MetricId { get; set; }
            public string ImperialId { get; set; }
            public double BaseMultiplier { get; set; }
            public double BaseOffset { get; set; }
        }

        public const string DistanceUnitGroup = "Distance";
        public const string AreaUnitGroup = "Area";
        public const string VelocityUnitGroup = "Velocity";
        public const string DischargeUnitGroup = "Discharge";
        public const string TemperatureUnitGroup = "Temperature";

        public static readonly Dictionary<string, Unit> Units = new Dictionary<string, Unit>
        {
            {DistanceUnitGroup, new Unit {MetricId = "m", ImperialId = "ft", BaseMultiplier = 0.3048}},
            {VelocityUnitGroup, new Unit {MetricId = "m/s", ImperialId = "ft/s", BaseMultiplier = 0.3048}},
            {AreaUnitGroup, new Unit {MetricId = "m^2", ImperialId = "ft^2", BaseMultiplier = 0.09290304}},
            {DischargeUnitGroup, new Unit {MetricId = "m^3/s", ImperialId = "ft^3/s", BaseMultiplier = 0.028316846592}},
            {TemperatureUnitGroup, new Unit {MetricId = "degC", ImperialId = "degF", BaseMultiplier = 5.0/9.0, BaseOffset = 32}},
        };

        public bool IsImperial { get; }

        public UnitConverter(bool isImperial)
        {
            IsImperial = isImperial;
        }

        public double ConvertDistance(double value)
        {
            return ConvertUnit(DistanceUnitGroup, value);
        }

        public double ConvertArea(double value)
        {
            return ConvertUnit(AreaUnitGroup, value);
        }

        public double ConvertVelocity(double value)
        {
            return ConvertUnit(VelocityUnitGroup, value);
        }

        public double ConvertDischarge(double value)
        {
            return ConvertUnit(DischargeUnitGroup, value);
        }

        public double ConvertTemperature(double value)
        {
            return ConvertUnit(TemperatureUnitGroup, value);
        }

        private double ConvertUnit(string unitGroup, double value)
        {
            var unit = Units[unitGroup];

            return !IsImperial || double.IsNaN(value)
                ? value
                : unit.BaseOffset + value / unit.BaseMultiplier;
        }
    }
}
