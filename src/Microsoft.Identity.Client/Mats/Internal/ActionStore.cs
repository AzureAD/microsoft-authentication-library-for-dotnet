// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class ActionStore : IActionStore
    {
        private readonly Dictionary<string, ActionPropertyBag> _actionIdToPropertyBag = new Dictionary<string, ActionPropertyBag>();
        private readonly object _lockActionIdToPropertyBag = new object();
        private readonly IErrorStore _errorStore;
        private readonly IEventFilter _eventFilter;
        private readonly int _maxActionDurationMillis;
        private readonly int _maxAggregationDurationMillis;
        private readonly HashSet<string> _telemetryAllowedScopes = new HashSet<string>();
        private readonly HashSet<string> _telemetryAllowedResources = new HashSet<string>();


        public ActionStore(
            int maxActionDurationMillis,
            int maxAggregationDurationMillis,
            IErrorStore errorStore,
            IEventFilter eventFilter,
            HashSet<string> telemetryAllowedScopes,
            HashSet<string> telemetryAllowedResources)
        {
            _maxActionDurationMillis = maxActionDurationMillis;
            _maxAggregationDurationMillis = maxAggregationDurationMillis;
            _errorStore = errorStore;
            _eventFilter = eventFilter;

            if (telemetryAllowedScopes != null)
            {
                foreach (string s in telemetryAllowedScopes)
                {
                    _telemetryAllowedScopes.Add(s);
                }
            }

            if (telemetryAllowedResources != null)
            {
                foreach (string s in telemetryAllowedResources)
                {
                    _telemetryAllowedResources.Add(s);
                }
            }
        }

        public void EndAdalAction(AdalAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription)
        {
            EndGenericAction(action.ActionId, MatsConverter.AsString(outcome), errorSource, error, errorDescription, string.Empty);
        }

        public void EndCustomInteractiveActionWithCancellation(CustomInteractiveAction action) => throw new NotImplementedException();
        public void EndCustomInteractiveActionWithFailure(CustomInteractiveAction action, ErrorSource errorSource, string error, string errorDescription) => throw new NotImplementedException();
        public void EndCustomInteractiveActionWithSuccess(CustomInteractiveAction action) => throw new NotImplementedException();
        public void EndInteractiveMsaActionWithCancellation(InteractiveMsaAction action, string accountCid) => throw new NotImplementedException();
        public void EndInteractiveMsaActionWithFailure(InteractiveMsaAction action, ErrorSource errorSource, string error, string errorDescription, string accountCid) => throw new NotImplementedException();
        public void EndInteractiveMsaActionWithSignin(InteractiveMsaAction action, string accountCid) => throw new NotImplementedException();
        public void EndNonInteractiveMsaActionWithFailure(NonInteractiveMsaAction action, ErrorSource errorSource, string error, string errorDescription, string accountCid) => throw new NotImplementedException();
        public void EndNonInteractiveMsaActionWithTokenRetrieval(NonInteractiveMsaAction action, string accountCid) => throw new NotImplementedException();
        public void EndWamActionWithCancellation(WamAction action, string wamTelemetryBatch) => throw new NotImplementedException();
        public void EndWamActionWithFailure(WamAction action, ErrorSource errorSource, string error, string errorDescription, string accountId, string tenantId, string wamTelemetryBatch) => throw new NotImplementedException();
        public void EndWamActionWithSuccess(WamAction action, string accountId, string tenantId, string wamTelemetryBatch) => throw new NotImplementedException();
        public IEnumerable<IPropertyBag> GetEventsForUpload() => throw new NotImplementedException();

        public void ProcessAdalTelemetryBlob(IDictionary<string, string> blob)
        {
            if (!blob.TryGetValue(AdalTelemetryBlobEventNames.AdalCorrelationIdConstStrKey, out string correlationId))
            {
                _errorStore.ReportError("No correlation ID found in telemetry blob", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            var propertyBags = GetAdalPropertyBagsForCorrelationId(correlationId);
            int numberOfPropertyBagsMatchingCorrelationId = propertyBags.Count;

            switch (numberOfPropertyBagsMatchingCorrelationId)
            {
            case 0:
                //[vadugar] ToDo: Is this an error?
                _errorStore.ReportError("No ADAL actions matched correlation ID", ErrorType.Action, ErrorSeverity.Warning);
                return;
            case 1:
                break;
            default:
                //[vadugar] ToDo: How should we handle this case?
                _errorStore.ReportError("Multiple ADAL actions matched correlation ID", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            var aggregatedProperties = GetAdalAggregatedProperties();
            var propertyBag = propertyBags[0];
            foreach (var blobEntry in blob)
            {
                if (string.Compare(blobEntry.Key, AdalTelemetryBlobEventNames.AdalCorrelationIdConstStrKey, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    continue;
                }

                // if this is a property we want to aggregte, add min/max/sum properties instead
                string normalizedPropertyName = NormalizeValidPropertyName(blobEntry.Key);
                if (aggregatedProperties.Contains(normalizedPropertyName) &&
                    int.TryParse(blobEntry.Value, out int blobIntValue))
                {
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.MaxConstStrSuffix, blobIntValue);
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.MinConstStrSuffix, blobIntValue);
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.SumConstStrSuffix, blobIntValue);
                }

                propertyBag.Add(blobEntry.Key, blobEntry.Value);
            }
        }

        private List<ActionPropertyBag> GetAdalPropertyBagsForCorrelationId(string correlationId)
        {
            var retval = new List<ActionPropertyBag>();
            lock (_lockActionIdToPropertyBag)
            {
                foreach (var propertyBag in _actionIdToPropertyBag.Values)
                {
                    var contents = propertyBag.GetContents();
                    var actionType = contents.StringProperties[ActionPropertyNames.ActionTypeConstStrKey];

                    string propertyBagCorrelationId = contents.StringProperties[ActionPropertyNames.CorrelationIdConstStrKey];
                    propertyBagCorrelationId = propertyBagCorrelationId.TrimCurlyBraces();
                    string correlationIdTrimmed = correlationId.TrimCurlyBraces();

                    if (string.Compare(actionType, MatsConverter.AsString(ActionType.Adal), StringComparison.OrdinalIgnoreCase) == 0 &&
                        string.Compare(propertyBagCorrelationId, correlationIdTrimmed, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        retval.Add(propertyBag);
                    }
                }
            }

            return retval;
        }

        private bool IsValidPropertyName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!MatsUtils.ContainsCharsThatAreEitherAlphaNumericOrDotsOrUnderscore(name))
            {
                return false;
            }

            return true;
        }

        private string NormalizeValidPropertyName(string name)
        {
            if (!IsValidPropertyName(name))
            {
                return string.Empty;
            }

            return name.Replace('.', '_');
        }

        private HashSet<string> GetAdalAggregatedProperties()
        {
            return new HashSet<string>
            {
                AdalTelemetryBlobEventNames.CacheEventCountConstStrKey,
                AdalTelemetryBlobEventNames.HttpEventCountTelemetryBatchKey,
                AdalTelemetryBlobEventNames.ResponseTimeConstStrKey,
            };
        }

        public AdalAction StartAdalAction(Scenario scenario, string correlationId, string resource)
        {
            var adalActionArtifacts = CreateGenericAction(scenario, correlationId, ActionType.Adal);
            adalActionArtifacts.PropertyBag.Add(ActionPropertyNames.IdentityServiceConstStrKey, MatsConverter.AsString(IdentityService.AAD));
            SetResourceProperty(adalActionArtifacts.PropertyBag, resource);
            return adalActionArtifacts.Action;
        }

        private void SetResourceProperty(ActionPropertyBag propertyBag, string resource)
        {
            if (_telemetryAllowedResources.Contains(resource))
            {
                propertyBag.Add(ActionPropertyNames.ResourceConstStrKey, resource);
            }
            else if (!string.IsNullOrEmpty(resource))
            {
                propertyBag.Add(ActionPropertyNames.ResourceConstStrKey, "ResourceRedacted");
            }
        }

        public CustomInteractiveAction StartCustomInteractiveAction(Scenario scenario, bool isBlockingUi, bool asksForCredentials, string correlationId, InteractiveAuthContainerType interactiveAuthContainerType, CustomIdentityService identityService) => throw new NotImplementedException();
        public InteractiveMsaAction StartInteractiveMsaAction(Scenario scenario, bool isBlockingUi, bool asksForCredentials, string correlationId, InteractiveAuthContainerType interactiveAuthContainerType, string scope) => throw new NotImplementedException();
        public NonInteractiveMsaAction StartNonInteractiveMsaAction(Scenario scenario, string correlationId, string scope) => throw new NotImplementedException();
        public WamAction StartWamAction(Scenario scenario, string correlationId, bool forcePrompt, WamIdentityService identityService, WamApi wamApi, string scope, string resource) => throw new NotImplementedException();

        // todo: what other action types would we get here?  does this need to be a generic function?
        // private ActionArtifacts<T> CreateGenericAction<T>(Scenario scenario, string correlationId, ActionType actionType)
        private ActionArtifacts<AdalAction> CreateGenericAction(Scenario scenario, string correlationId, ActionType actionType)
        {
            string actionId = MatsId.Create();
            AdalAction action = new AdalAction(actionId, scenario);

            string corrIdTrim = correlationId.TrimCurlyBraces();

            var propertyBag = new ActionPropertyBag(_errorStore);
            var startTimePoint = DateTime.UtcNow;
            propertyBag.Add(ActionPropertyNames.UploadIdConstStrKey, MatsId.Create());
            propertyBag.Add(ActionPropertyNames.ActionTypeConstStrKey, MatsConverter.AsString(actionType));
            propertyBag.Add(ScenarioPropertyNames.IdConstStrKey, scenario.ScenarioId);
            propertyBag.Add(ActionPropertyNames.CorrelationIdConstStrKey, corrIdTrim);
            propertyBag.Add(ActionPropertyNames.StartTimeConstStrKey, DateTimeUtils.GetMillisecondsSinceEpoch(startTimePoint));

            lock (_lockActionIdToPropertyBag)
            {
                _actionIdToPropertyBag[actionId] = propertyBag;
            }

            return new ActionArtifacts<AdalAction>(action, propertyBag);
        }

        private void EndGenericAction(string actionId, string outcome, ErrorSource errorSource, string error, string errorDescription, string accountCid)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                // This is an invalid action; we do not want to upload it.
                _errorStore.ReportError("Tried to end an action with an empty actionId", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            ActionPropertyBag propertyBag = GetActionPropertyBagFromId(actionId);

            if (propertyBag == null)
            {
                _errorStore.ReportError("Trying to end an action that doesn't exist or was already uploaded", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            if (propertyBag.ReadyForUpload)
            {
                return;
            }

            int startingCount = 1;
            var endTime = DateTime.UtcNow;
            propertyBag.Add(ActionPropertyNames.OutcomeConstStrKey, outcome);
            propertyBag.Add(ActionPropertyNames.FailureSourceConstStrKey, MatsConverter.AsString(errorSource));
            propertyBag.Add(ActionPropertyNames.FailureConstStrKey, error);
            propertyBag.Add(ActionPropertyNames.FailureDescriptionConstStrKey, errorDescription);
            propertyBag.Add(ActionPropertyNames.EndTimeConstStrKey, DateTimeUtils.GetMillisecondsSinceEpoch(endTime));
            // propertyBag->Add(ActionPropertyNames::getAccountIdConstStrKey(), accountCid);  Commenting this out for GDPR reasons; once pipeline supports this we can upload again.
            propertyBag.Add(ActionPropertyNames.CountConstStrKey, startingCount);
            PopulateDuration(propertyBag);

            //Check if should aggregate here
            var contents = propertyBag.GetContents();
            if (_eventFilter.ShouldAggregateAction(contents))
            {
                EndAggregatedAction(actionId, propertyBag);
            }
            else
            {
                propertyBag.ReadyForUpload = true;
            }

        }

        private void EndAggregatedAction(string actionId, ActionPropertyBag propertyBag)
        {
            lock (_lockActionIdToPropertyBag)
            {
                propertyBag.IsAggregable = true;

                bool shouldRemove = false;
                foreach (var targetPropertyBag in _actionIdToPropertyBag.Values)
                {
                    if (ActionComparer.IsEquivalentClass(targetPropertyBag, propertyBag))
                    {
                        ActionAggregator.AggregateActions(targetPropertyBag, propertyBag);
                        shouldRemove = true;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    _actionIdToPropertyBag.Remove(actionId);
                }
            }
        }

        private void PopulateDuration(ActionPropertyBag propertyBag)
        {
            var contents = propertyBag.GetContents();
            long startTime;
            long endTime;

            if (!contents.Int64Properties.TryGetValue(ActionPropertyNames.StartTimeConstStrKey, out startTime))
            {
                _errorStore.ReportError("Could not retrieve start time for duration calculation.", ErrorType.Action, ErrorSeverity.LibraryError);
                return;
            }

            if (!contents.Int64Properties.TryGetValue(ActionPropertyNames.EndTimeConstStrKey, out endTime))
            {
                _errorStore.ReportError("Could not retrieve end time for duration calculation.", ErrorType.Action, ErrorSeverity.LibraryError);
                return;
            }

            long duration = endTime - startTime;

            propertyBag.Add(ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.SumConstStrSuffix, duration);
            propertyBag.Add(ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MaxConstStrSuffix, duration);
            propertyBag.Add(ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MinConstStrSuffix, duration);
        }

        private ActionPropertyBag GetActionPropertyBagFromId(string actionId)
        {
            lock (_lockActionIdToPropertyBag)
            {
                if (_actionIdToPropertyBag.TryGetValue(actionId, out ActionPropertyBag propertyBag))
                {
                    return propertyBag;
                }
                return null;
            }
        }
    }
}
