using System;
using System.IO;
using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.Results;

namespace FlowTracker2Plugin
{
    public class Plugin : IFieldDataPlugin
    {
        public ParseFileResult ParseFile(Stream fileStream, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            try
            {
                var parser = new DataFileParser(logger, fieldDataResultsAppender);

                return parser.Parse(fileStream);
            }
            catch (Exception e)
            {
                LogException(logger, "Can't parse global context", e);
                throw;
            }
        }

        public ParseFileResult ParseFile(Stream fileStream, LocationInfo targetLocation, IFieldDataResultsAppender fieldDataResultsAppender, ILog logger)
        {
            try
            {
                var parser = new DataFileParser(logger, fieldDataResultsAppender);

                return parser.Parse(fileStream, targetLocation);
            }
            catch (Exception e)
            {
                LogException(logger, $"Can't parse location={targetLocation.LocationIdentifier} context", e);
                throw;
            }
        }

        private void LogException(ILog log, string message, Exception exception)
        {
            log.Error($"{message}: {exception.Message}\n{exception.StackTrace}");

            if (exception.InnerException != null)
            {
                LogException(log, "InnerException", exception.InnerException);
            }
        }

    }
}
