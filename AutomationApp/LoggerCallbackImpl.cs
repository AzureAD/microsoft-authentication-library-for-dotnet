using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    class LoggerCallbackImpl : ILoggerCallback
    {
        private readonly StringBuilder _logCollector = new StringBuilder();

        public void Log(Logger.LogLevel level, string message, bool containsPii)
        {
            _logCollector.Append(message);
        }

        public string GetMsalLogs()
        {
            return _logCollector.ToString();
        }
    }
}
