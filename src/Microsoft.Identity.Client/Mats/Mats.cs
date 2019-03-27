// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.Identity.Client.Mats.Internal.Constants;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Mats
{
    internal class Mats : IMats
    {
        private readonly IErrorStore _errorStore;
        private readonly IUploader _uploader;
        private readonly IActionStore _actionStore;
        private readonly IScenarioStore _scenarioStore;
        private readonly ContextStore _contextStore;
        private readonly bool _isScenarioUploadDisabled;
        private readonly object _lockObject = new object();

        public static IMats CreateMats(IApplicationConfiguration applicationConfiguration, IPlatformProxy platformProxy, IMatsConfig matsConfig)
        {
            string dpti = platformProxy.GetDpti();

            if (!IsDeviceEnabled(matsConfig.AudienceType, dpti))
            {
                return null;
            }

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

            var actionStore = new ActionStore(
                TimeConstants.ActionTimeoutMilliseconds,
                TimeConstants.AggregationWindowMilliseconds,
                errorStore,
                eventFilter,
                allowedScopes);

            var contextStore = ContextStore.CreateContextStore(
                matsConfig.AudienceType,
                matsConfig.AppName,
                matsConfig.AppVer,
                dpti,
                deviceNetworkState,
                matsConfig.SessionId,
                osPlatformCode);

            IUploader uploader = new TelemetryUploader(matsConfig.DispatchAction, platformProxy, matsConfig.AppName);

            // it's this way in mats c++
            bool isScenarioUploadDisabled = true;

            return new Mats(
                applicationConfiguration,
                platformProxy,
                errorStore,
                uploader,
                actionStore,
                scenarioStore,
                contextStore,
                isScenarioUploadDisabled);
        }

        private Mats(
            IApplicationConfiguration applicationConfiguration,
            IPlatformProxy platformProxy,
            IErrorStore errorStore,
            IUploader uploader,
            IActionStore actionStore,
            IScenarioStore scenarioStore,
            ContextStore contextStore,
            bool isScenarioUploadDisabled)
        {
            TelemetryManager = new TelemetryManager(applicationConfiguration, platformProxy, ProcessTelemetryCallback);

            _errorStore = errorStore;
            _uploader = uploader;
            _actionStore = actionStore;
            _scenarioStore = scenarioStore;
            _contextStore = contextStore;
            _isScenarioUploadDisabled = isScenarioUploadDisabled;
        }

        public ITelemetryManager TelemetryManager { get; }

        private static bool IsDeviceEnabled(MatsAudienceType audienceType, string dpti)
        {
            if (audienceType == MatsAudienceType.PreProduction)
            {
                // Pre-production should never be sampled
                return true;
            }
            return SampleUtils.ShouldEnableDevice(dpti);
        }

        public MatsScenario CreateScenario()
        {
            var scenario = _scenarioStore.CreateScenario();
            _uploader.Upload(GetEventsForUpload());
            return scenario;
        }

        public MatsAction StartAction(MatsScenario scenario, string correlationId)
        {
            return StartActionWithScopes(scenario, correlationId, null);
        }

        public MatsAction StartActionWithScopes(MatsScenario scenario, string correlationId, IEnumerable<string> scopes)
        {
            return _actionStore.StartMsalAction(scenario, correlationId, scopes ?? new List<string>());
        }

        public void EndAction(MatsAction action, AuthenticationResult authenticationResult)
        {
            // todo(mats): map contents of authentication result to appropriate telemetry values.
            AuthOutcome outcome = AuthOutcome.Succeeded;
            ErrorSource errorSource = ErrorSource.Service;
            string error = string.Empty;
            string errorDescription = string.Empty;

            EndAction(action, outcome, errorSource, error, errorDescription);
        }

        public void EndAction(MatsAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription)
        {
            TelemetryManager.Flush(action.TelemetryCorrelationId);
            _actionStore.EndMsalAction(action, outcome, errorSource, error, errorDescription);
            _uploader.Upload(GetEventsForUpload());
        }

        public void EndAction(MatsAction action, Exception ex)
        {
            AuthOutcome outcome = AuthOutcome.Failed;
            ErrorSource errorSource = ErrorSource.AuthSdk;
            string error = ex.Message;
            string errorDescription = ex.Message;

            switch (ex)
            {
            case MsalUiRequiredException uiEx:
                errorSource = ErrorSource.Service;
                error = uiEx.ErrorCode;
                break;
            case MsalServiceException svcEx:
                errorSource = ErrorSource.Service;
                error = svcEx.ErrorCode;
                break;
            case MsalClientException cliEx:
                errorSource = ErrorSource.Client;
                error = cliEx.ErrorCode;
                break;
            case MsalException msalEx:
                errorSource = ErrorSource.AuthSdk;
                error = msalEx.ErrorCode;
                break;
            default:
                errorSource = ErrorSource.AuthSdk;
                break;
            }

            EndAction(action, outcome, errorSource, error, errorDescription);
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
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion

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
            _errorStore.ReportError(errorMessage, errorType, errorSeverity);
        }

        public void ProcessTelemetryCallback(List<Dictionary<string, string>> events)
        {
            foreach (var dict in events)
            {
                _actionStore.ProcessMsalTelemetryBlob(dict);
            }
        }
    }
}
