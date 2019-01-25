using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    public class LoggingConfig
    {
        public LogLevel LogLevel { get; }
        public bool IsPiiEnabled { get; }
        public LogCallback LogCallback { get; }

        internal LoggingConfig(LogCallback logCallback, bool isPiiEnabled, LogLevel logLevel)
        {
            LogLevel = logLevel;
            IsPiiEnabled = isPiiEnabled;
            LogCallback = logCallback;
        }

        public static LoggingConfigBuilder Create(LogCallback logCallback)
        {
            return new LoggingConfigBuilder(logCallback);
        }
    }

    public class LoggingConfigBuilder
    {
        private LogCallback _logCallback;
        private bool _isPiiEnabled = false;
        private LogLevel _logLevel = LogLevel.Info;

        public LoggingConfigBuilder(LogCallback logCallback)
        {
            _logCallback = logCallback ?? throw new ArgumentException(nameof(logCallback));
        }

        public LoggingConfigBuilder EnablePii(bool allowPiiMessages)
        {
            _isPiiEnabled = allowPiiMessages;
            return this;
        }

        public LoggingConfigBuilder WithLogLevel(LogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public LoggingConfig Build()
        {
            return new LoggingConfig(_logCallback, _isPiiEnabled, _logLevel);
        }
    }
}
