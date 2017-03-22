using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WinFormsAutomationApp
{
    class LoggerCallbackImpl : IAdalLogCallback
    {
        private StringBuilder logCollector = new StringBuilder();
        public void Log(LogLevel level, string message)
        {
            logCollector.AppendLine(message);
        }

        public string GetAdalLogs()
        {
            return logCollector.ToString();
        }
    }
}
