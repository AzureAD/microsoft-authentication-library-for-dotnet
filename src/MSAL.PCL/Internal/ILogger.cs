using System;

namespace Microsoft.Identity.Client.Internal
{
    public interface ILogger
    {
        void Error(string message);
        void Warning(string message);
        void Information(string message);
        void Verbose(string message);

        void Error(Exception ex);
        void Warning(Exception ex);
        void Information(Exception ex);
        void Verbose(Exception ex);
    }
}