// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Mats
{
    internal class Mats : IMats
    {
        private readonly bool _isTelemetryDisabled;
        private readonly IErrorStore _errorStore;
        private readonly IUploader _uploader;
        private readonly IEventFilter _eventFilter;
        private readonly IActionStore _actionStore;
        private readonly IScenarioStore _scenarioStore;
        private readonly ContextStore _contextStore;
        private readonly int _osPlatformCode;
        private readonly ITelemetryDispatcher _telemetryDispatcher;
        private readonly bool _isScenarioUploadDisabled;
        private readonly object _lockObject = new object();

        public static IMats CreateMats(IPlatformProxy platformProxy, IMatsConfig matsConfig)
        {
            string dpti = platformProxy.GetDpti();
            string deviceNetworkState = platformProxy.GetDeviceNetworkState();
            int osPlatformCode = platformProxy.GetMatsOsPlatformCode();
            
            bool enableAggregation = true;
            IEventFilter eventFilter = new EventFilter(enableAggregation);
            var errorStore = new ErrorStore();
            var scenarioStore = new ScenarioStore(TimeConstants.ScenarioTimeoutMilliseconds, errorStore);

            var allowedScopes = new HashSet<string>();
            if (matsConfig.AllowedScopes != null)
            {
                foreach (string s in matsConfig.AllowedScopes)
                {
                    allowedScopes.Add(s);
                }
            }
            var allowedResources = new HashSet<string>();
            if (matsConfig.AllowedResources != null)
            {
                foreach (string s in matsConfig.AllowedResources)
                {
                    allowedResources.Add(s);
                }
            }

            var actionStore = new ActionStore(
                TimeConstants.ActionTimeoutMilliseconds, 
                TimeConstants.AggregationWindowMilliseconds, 
                errorStore,
                eventFilter,
                allowedScopes ,
                allowedResources);

            var contextStore = ContextStore.CreateContextStore(
                matsConfig.AudienceType, 
                matsConfig.AppName, 
                matsConfig.AppVer, 
                dpti, 
                deviceNetworkState, 
                matsConfig.SessionId, 
                osPlatformCode);

            var dispatcher = new TelemetryDispatcher(matsConfig.DispatchAction);
            IUploader uploader = new TelemetryUploader(dispatcher, platformProxy, matsConfig.AppName);

            // it's this way in mats c++
            bool isScenarioUploadDisabled = true;

            return new Mats(
                !IsDeviceEnabled(matsConfig.AudienceType, dpti),
                errorStore,
                uploader,
                eventFilter,
                osPlatformCode,
                actionStore,
                scenarioStore,
                contextStore,
                dispatcher,
                isScenarioUploadDisabled);
        }

        private Mats(
            bool isTelemetryDisabled,
            IErrorStore errorStore,
            IUploader uploader,
            IEventFilter eventFilter,
            int osPlatformCode,
            IActionStore actionStore,
            IScenarioStore scenarioStore,
            ContextStore contextStore,
            ITelemetryDispatcher telemetryDispatcher,
            bool isScenarioUploadDisabled)
        {
            _isTelemetryDisabled = isTelemetryDisabled;
            _errorStore = errorStore;
            _uploader = uploader;
            _eventFilter = eventFilter;
            _osPlatformCode = osPlatformCode;
            _actionStore = actionStore;
            _scenarioStore = scenarioStore;
            _contextStore = contextStore;
            _telemetryDispatcher = telemetryDispatcher;
            _isScenarioUploadDisabled = isScenarioUploadDisabled;
        }

        private static bool IsDeviceEnabled(MatsAudienceType audienceType, string dpti)
        {
            if (audienceType == MatsAudienceType.PreProduction)
            {
                // Pre-production should never be sampled
                return true;
            }
            return SampleUtils.ShouldEnableDevice(dpti);
        }

        public IScenarioHandle CreateScenario()
        {
            var scenario = _scenarioStore.CreateScenario();
            _uploader.Upload(GetEventsForUpload());
            return scenario;
        }

        public IActionHandle StartAction(IScenarioHandle scenario, string correlationId)
        {
            return StartActionWithResource(scenario, correlationId, string.Empty);
        }

        public IActionHandle StartActionWithResource(IScenarioHandle scenario, string correlationId, string resource)
        {
            return _actionStore.StartAdalAction((Scenario)scenario, correlationId, resource ?? string.Empty);
        }

        public void EndAction(IActionHandle action, AuthenticationResult authenticationResult)
        {
            // todo(mats): map contents of authentication result to appropriate telemetry values.
            AuthOutcome outcome = AuthOutcome.Succeeded;
            ErrorSource errorSource = ErrorSource.Service;
            string error = string.Empty;
            string errorDescription = string.Empty;

            EndAction(action, outcome, errorSource, error, errorDescription);
        }

        public void EndAction(IActionHandle action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription)
        {
            _actionStore.EndAdalAction((AdalAction)action, outcome, errorSource, error, errorDescription);
            _uploader.Upload(GetEventsForUpload());
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    UploadCompletedEvents();
                    UploadErrorEvents();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        private void UploadErrorEvents()
        {
            var errorPropertyBagContents = new List<PropertyBagContents>();
            var errorEvents = _errorStore.GetEventsForUpload();
            _contextStore.AddContext(errorEvents);
            foreach (var errorEvent in errorEvents)
            {
                errorPropertyBagContents.Add(errorEvent.GetContents());
            }

            _uploader.Upload(errorPropertyBagContents);
        }
        
        private void UploadCompletedEvents()
        {
            _uploader.Upload(GetEventsForUpload());
        }

        private IEnumerable<PropertyBagContents> GetEventsForUpload()
        {
            var retval = new List<PropertyBagContents>();

            lock (_lockObject)
            {
                var actionEvents = _actionStore.GetEventsForUpload();
                if (_isScenarioUploadDisabled)
                {
                    // stamp context on actions instead
                    _contextStore.AddContext(actionEvents);
                }

                foreach (var actionEvent in actionEvents)
                {
                    var contents = actionEvent.GetContents();
                    if (contents.StringProperties.TryGetValue(ScenarioPropertyNames.IdConstStrKey, out string scenarioId))
                    {
                        _scenarioStore.NotifyActionCompleted(scenarioId);
                        retval.Add(contents);
                    }
                    else
                    {
                        ReportError("Trying to upload an Action with no Scenario Id", ErrorType.Action, ErrorSeverity.LibraryError);
                    }
                }

                if (_isScenarioUploadDisabled)
                {
                    _scenarioStore.ClearCompletedScenarios();
                }
                else
                {
                    var scenarioEvents = _scenarioStore.GetEventsForUpload();
                    _contextStore.AddContext(scenarioEvents);
                    foreach (var scenarioEvent in scenarioEvents)
                    {
                        retval.Add(scenarioEvent.GetContents());
                    }
                    }
            }

            return retval;
        }

        private void ReportError(string errorMessage, ErrorType errorType, ErrorSeverity errorSeverity)
        {
            lock (_lockObject)
            {
                if (_isTelemetryDisabled)
                {
                    return;
                }
            }

            _errorStore.ReportError(errorMessage, errorType, errorSeverity);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion

        public void ProcessTelemetryBlob(Dictionary<string, string> blob)
        {
            _actionStore.ProcessAdalTelemetryBlob(blob);
        }
    }
}
