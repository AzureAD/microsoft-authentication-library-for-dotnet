using System;
using System.Threading;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// ADAL Log Levels
    /// </summary>
    public enum LogLevel
    {
        Information,
        Verbose,
        Warning,
        Error
    }

    /// <summary>
    /// Callback for capturing ADAL logs to custom logging schemes.
    /// </summary>
    public interface IAdalLogCallback
    {
        void Log(LogLevel level, string message);
    }

    /// <summary>
    /// This class is responsible for managing the callback state and its execution. 
    /// </summary>
    public sealed class LoggerCallbackHandler
    {
        private static readonly object LockObj = new object();

        private static IAdalLogCallback _localCallback;

        /// <summary>
        /// Callback implementation
        /// </summary>
        public static IAdalLogCallback Callback
        {
            set
            {
                lock (LockObj)
                {
                    _localCallback = value;
                }
            }
        }

        internal static void ExecuteCallback(LogLevel level, string message)
        {
            lock (LockObj)
            {
                _localCallback?.Log(level, message);
            }
        }
    }
}
