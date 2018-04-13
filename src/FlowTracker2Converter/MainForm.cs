using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using SonTek.Framework.Configuration;
using SonTek.Framework.Data;

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

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] paths)) return;

            foreach (var path in paths)
            {
                TryConvertFile(path);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
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

        private string ConvertToDis(DataFile dataFile, string sourcePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            var utcOffset = dataFile.HandheldInfo.Settings?.GetTimeSpan("LocalTimeOffsetFromUtc") ?? TimeSpan.Zero;
            var startTime = CreateDateTimeOffset(dataFile.Properties.StartTime, utcOffset);
            AppendValue(sb, "File_Name", Path.GetFileName(sourcePath));
            AppendValue(sb, "Start_Date_and_Time", $"{startTime:yyyy/MM/dd HH:mm:ss}");
            AppendValue(sb, "Site_Name", dataFile.Properties.SiteNumber);
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
            AppendValue(sb, "Unit_System", "Metric Units");
            AppendValue(sb, "Discharge_Equation", $"{equation}");
            AppendValue(sb, "Start_Edge", startEdge);
            AppendValue(sb, "#_Stations", $"{dataFile.Stations.Count}");
            AppendValue(sb, "Total_Width", $"{dataFile.Calculations.Width:F3} m");
            AppendValue(sb, "Total_Area", $"{dataFile.Calculations.Area:F3} m^2");
            AppendValue(sb, "Total_Discharge", $"{dataFile.Calculations.Discharge:F4} m^3/s");
            AppendValue(sb, "Mean_Depth", $"{dataFile.Calculations.Depth:F3} m");
            AppendValue(sb, "Mean_Velocity", $"{dataFile.Calculations.Velocity.X:F4} m/2");
            AppendValue(sb, "Mean_Temp", $"{dataFile.Calculations.Temperature:F2} deg C");

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

            var gageHeight = dataFile.Calculations.GaugeHeight;

            if (!double.IsNaN(gageHeight))
            {
                sb.AppendLine();
                sb.AppendLine("Supplemental_Data");
                sb.AppendLine(" Record        Date     Time   Location(ft)   Gauge_Height(ft)  Rated_Flow(cfs)  Comments");
                sb.AppendLine($"     01                   ()            ()            {gageHeight:F3}                 ()  ");
            }

            sb.AppendLine();
            sb.AppendLine("St  Clock     Loc  Depth   IceD %Dep  MeasD Npts Spike     Vel   SNR Angle    Verr Bnd    Temp CorrFact   MeanV   Area     Flow   %Q");
            sb.AppendLine("()     ()    (ft)   (ft)   (ft) (*D)   (ft)   ()    ()  (ft/s)  (dB) (deg)  (ft/s)  ()  (degF)    ()     (ft/s) (ft^2)    (cfs)  (%)");

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

                //              St      Clock                             Loc                             Depth      IceD        %Dep                         MeasD             Npts           Spike                                   Vel        SNR       Angle         Verr Bnd         Temp                           CorrFact                        MeanV            Area                  Flow     %Q
                sb.AppendLine($"{i,-2}  {time:HH:mm}  {station.Location,5:F2}  {station.GetEffectiveDepth():F3}  {ice:F3}  {method,3}  {station.GetFinalDepth():F3} {calc.Samples,4} {calc.Spikes,5}    {calc.MeanVelocityInVertical.X:F3} {snr,4:F1} {angle,4:F0}   {vErr:F4}   0  {temp,6:F2}    {station.CorrectionFactor,5:F2}  {calc.MeanPanelVelocity.X:F4}  {calc.Area:F3}   {calc.Discharge:F4}   {100 * calc.FractionOfTotalDischarge:F1}");
            }

            return sb.ToString();
        }

        private static DateTimeOffset CreateDateTimeOffset(DateTime dateTime, TimeSpan utcOffset)
        {
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
