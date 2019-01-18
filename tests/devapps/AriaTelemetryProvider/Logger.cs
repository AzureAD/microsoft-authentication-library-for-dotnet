using System;

namespace AriaTelemetryProvider
{
    internal class Logger
    {
        internal bool WriteToConsole { get; set; } = false;

        internal void Log(string message)
        {
            if (WriteToConsole)
            {
                Console.WriteLine(message);
            }
        }
    }
}
