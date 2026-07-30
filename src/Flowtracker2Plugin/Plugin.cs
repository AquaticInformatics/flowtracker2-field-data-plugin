using System;
using System.IO;
using System.Text;
using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.Results;

namespace FlowTracker2Plugin
{
    public class Plugin : IFieldDataPlugin
    {
        static Plugin()
        {
            // Included to avoid "System.NotSupportedException: No data is available for encoding 437." error in net10
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // SonTek's DataFileComplete method can cause an "AssemblyLoadContext is unloading or was already unloaded" error in net10
            // Keeps the UnitConverter type alive to avoid this error
            GC.KeepAlive(typeof(UnitConversion.UnitConverter));
        }

        public ParseFileResult ParseFile(Stream fileStream, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            var parser = new DataFileParser(logger, fieldDataResultsAppender);

            return parser.Parse(fileStream);
        }

        public ParseFileResult ParseFile(Stream fileStream, LocationInfo targetLocation, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            var parser = new DataFileParser(logger, fieldDataResultsAppender);

            return parser.Parse(fileStream, targetLocation);
        }
    }
}
