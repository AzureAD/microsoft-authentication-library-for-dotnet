using System;
using System.Threading;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// 
    /// </summary>
    public enum LogLevel
    {
        Information,
        Verbose,
        Warning,
        Error
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IMsalLogCallback
    {
        void Log(LogLevel level, string message);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class LoggerCallbackHandler
    {
        private static readonly object LockObj = new object();

        private static IMsalLogCallback _localCallback;

        /// <summary>
        /// 
        /// </summary>
        public static IMsalLogCallback Callback
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