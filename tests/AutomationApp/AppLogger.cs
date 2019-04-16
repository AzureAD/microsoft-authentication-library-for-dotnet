// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Identity.Client;

namespace AutomationApp
{
    class AppLogger
    {
        private readonly StringBuilder _logCollector = new StringBuilder();

        public void Log(LogLevel level, string message, bool containsPii)
        {
            _logCollector.Append(message);
        }

        public string GetMsalLogs()
        {
            return _logCollector.ToString();
        }
    }
}
