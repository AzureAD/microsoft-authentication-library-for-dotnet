using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public abstract class Logger
    {
        #region Logging Enums
        public enum LogLevel
        {
            Error,
            Warning,
            Info,
            Verbose
        }

        public enum EventType
        {
            Error,
            Warning,
            Info,
            Verbose
        }
        #endregion

        #region LogMessages as per Ryan's design doc

        internal abstract void Error(RequestContext context, string message, EventType logEvent);
        internal abstract void ErrorPii(RequestContext context, string message, EventType logEvent);
        internal abstract void Warning(RequestContext context, string message, EventType logEvent);
        internal abstract void WarningPii(RequestContext context, string message, EventType logEvent);
        internal abstract void Info(RequestContext context, string message, EventType logEvent);
        internal abstract void InfoPii(RequestContext context, string message, EventType logEvent);
        internal abstract void Verbose(RequestContext context, string message, EventType logEvent);
        internal abstract void VerbosePii(RequestContext context, string message, EventType logEvent);

        #endregion

        //readonly fields uninitialized, later assigned in the constructor
        private readonly List<EventType> _errorEventTypes;
        private readonly List<EventType> _warningEventTypes;
        private readonly List<EventType> _infoEventTypes;
        private readonly List<EventType> _verboseEventTypes;

        protected Logger()
        {
            var errorList = new List<EventType>()
            {
                EventType.Error
            };

            var warningList = new List<EventType>()
            {
                EventType.Warning
            };
            warningList.AddRange(errorList);

            var infoList = new List<EventType>()
            {
                EventType.Info
            };
            infoList.AddRange(warningList);

            var verboseList = new List<EventType>()
            {
                EventType.Verbose
            };
            verboseList.AddRange(infoList);

            _errorEventTypes = errorList;
            _warningEventTypes = warningList;
            _infoEventTypes = infoList;
            _verboseEventTypes = verboseList;
        }

        internal void LogMessage(RequestContext context, string logMessage, EventType logEvent)
        {
            //appLogLevel is set by client
            LogLevel appLogLevel = LogLevel.Info;

            switch (appLogLevel)
            {
                case LogLevel.Error:
                    if (_errorEventTypes.Contains(logEvent))
                    {
                        LogMessageHelper(context, logMessage, logEvent);
                    }
                    break;

                case LogLevel.Warning:
                    if (_warningEventTypes.Contains(logEvent))
                    {
                        LogMessageHelper(context, logMessage, logEvent);
                    }
                    break;

                case LogLevel.Info:
                    if (_infoEventTypes.Contains(logEvent))
                    {
                        LogMessageHelper(context, logMessage, logEvent);
                    }
                    break;

                case LogLevel.Verbose:
                    if (_verboseEventTypes.Contains(logEvent))
                    {
                        LogMessageHelper(context, logMessage, logEvent);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LogMessageHelper(RequestContext context, string logMessage, EventType logEvent)
        {
            switch (logEvent)
            {
                case EventType.Error:
                    Error(context, logMessage, logEvent);
                    break;

                case EventType.Warning:
                    Warning(context, logMessage, logEvent);
                    break;

                case EventType.Info:
                    Info(context, logMessage, logEvent);
                    break;

                case EventType.Verbose:
                    Verbose(context, logMessage, logEvent);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(logEvent), logEvent, null);
            }
        }

        internal void LogMessage(RequestContext context, Exception ex, EventType logEvent)
        {
            LogMessage(context, ex.ToString(), logEvent);
        }
    }
}