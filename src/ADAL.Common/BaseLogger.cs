using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Common
{
    internal abstract class BaseLogger : ILogger
    {
        internal string PrepareLogMessage(CallState callState, string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args) + (callState != null ? (". Correlation ID: " + callState.CorrelationId) : string.Empty);
        }

        public abstract void Verbose(CallState callState, string format, params object[] args);
        public abstract void Information(CallState callState, string format, params object[] args);
        public abstract void Warning(CallState callState, string format, params object[] args);
        public abstract void Error(CallState callState, string format, params object[] args);
    }
}
