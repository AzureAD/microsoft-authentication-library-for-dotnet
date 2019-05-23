// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
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

        public ActionStore(
            int maxActionDurationMillis,
            int maxAggregationDurationMillis,
            IErrorStore errorStore,
            IEventFilter eventFilter,
            HashSet<string> telemetryAllowedScopes)
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
        }

        public void EndMsalAction(MatsAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription)
        {
            EndGenericAction(action.ActionId, MatsConverter.AsString(outcome), errorSource, error, errorDescription, string.Empty);
        }

        public IEnumerable<IPropertyBag> GetEventsForUpload()
        {
            lock (_lockActionIdToPropertyBag)
            {
                var keysToRemove = new List<string>();
                var retval = new List<PropertyBag>();

                foreach (var kvp in _actionIdToPropertyBag)
                {
                    var propertyBag = kvp.Value;

                    if (!propertyBag.ReadyForUpload)
                    {
                        var contents = propertyBag.GetContents();
                        if (!contents.Int64Properties.TryGetValue(ActionPropertyNames.StartTimeConstStrKey, out long startTime))
                        {
                            _errorStore.ReportError("No start time on action", ErrorType.Action, ErrorSeverity.LibraryError);
                            continue;
                        }

                        long currentTimeInMs = DateTimeUtils.GetMillisecondsSinceEpoch(DateTime.UtcNow);
                        long durationInMs = currentTimeInMs - startTime;

                        if (propertyBag.IsAggregable && durationInMs > _maxActionDurationMillis)
                        {
                            propertyBag.ReadyForUpload = true;
                        }
                        else if (durationInMs > _maxAggregationDurationMillis)  // todo: report bug in C++ code where they're doing microseconds around this value instead of milliseconds
                        {
                            propertyBag.Add(ActionPropertyNames.EndTimeConstStrKey, currentTimeInMs);
                            propertyBag.Add(ActionPropertyNames.OutcomeConstStrKey, MatsConverter.AsString(AuthOutcome.Incomplete));
                            propertyBag.ReadyForUpload = true;
                        }
                        else
                        {
                            // not ready for upload...
                            continue;
                        }
                    }

                    retval.Add(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }

                foreach (string key in keysToRemove)
                {
                    _actionIdToPropertyBag.Remove(key);
                }

                return retval;
            }
        }

        public void ProcessMsalTelemetryBlob(IDictionary<string, string> blob)
        {
            if (!blob.TryGetValue(MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey, out string correlationId))
            {
                _errorStore.ReportError("No correlation ID found in telemetry blob", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            var propertyBags = GetMsalPropertyBagsForCorrelationId(correlationId);
            int numberOfPropertyBagsMatchingCorrelationId = propertyBags.Count;

            switch (numberOfPropertyBagsMatchingCorrelationId)
            {
            case 0:
                //[vadugar] ToDo: Is this an error?
                _errorStore.ReportError("No MSAL actions matched correlation ID", ErrorType.Action, ErrorSeverity.Warning);
                return;
            case 1:
                break;
            default:
                //[vadugar] ToDo: How should we handle this case?
                _errorStore.ReportError("Multiple MSAL actions matched correlation ID", ErrorType.Action, ErrorSeverity.Warning);
                return;
            }

            var aggregatedProperties = GetMsalAggregatedProperties();
            var propertyBag = propertyBags[0];
            foreach (var kvp in blob)
            {
                if (string.Compare(kvp.Key, MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    continue;
                }

                // if this is a property we want to aggregte, add min/max/sum properties instead
                string normalizedPropertyName = NormalizeValidPropertyName(kvp.Key);
                if (aggregatedProperties.Contains(normalizedPropertyName) &&
                    int.TryParse(kvp.Value, out int blobIntValue))
                {
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.MaxConstStrSuffix, blobIntValue);
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.MinConstStrSuffix, blobIntValue);
                    propertyBag.Add(normalizedPropertyName + ActionPropertyNames.SumConstStrSuffix, blobIntValue);
                }

                propertyBag.Add(kvp.Key, kvp.Value);
            }
        }

        private List<ActionPropertyBag> GetMsalPropertyBagsForCorrelationId(string correlationId)
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

                    if (string.Compare(actionType, MatsConverter.AsString(ActionType.Msal), StringComparison.OrdinalIgnoreCase) == 0 &&
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

        private HashSet<string> GetMsalAggregatedProperties()
        {
            return new HashSet<string>
            {
                MsalTelemetryBlobEventNames.CacheEventCountConstStrKey,
                MsalTelemetryBlobEventNames.HttpEventCountTelemetryBatchKey,
                MsalTelemetryBlobEventNames.ResponseTimeConstStrKey,
            };
        }

        public MatsAction StartMsalAction(MatsScenario scenario, string correlationId, IEnumerable<string> scopes)
        {
            var actionArtifacts = CreateGenericAction(scenario, correlationId, ActionType.Msal);
            actionArtifacts.PropertyBag.Add(ActionPropertyNames.IdentityServiceConstStrKey, MatsConverter.AsString(IdentityService.AAD));
            SetScopesProperty(actionArtifacts.PropertyBag, scopes);
            return actionArtifacts.Action;
        }

        private void SetScopesProperty(ActionPropertyBag propertyBag, IEnumerable<string> scopes)
        {
            // TODO(mats): how is this supposed to work?  Should we get multiple properties somehow, one per scope?  or do we send up all scopes in a space-delimited string (will make querying hard on the backend)
            foreach (string scope in scopes)
            {
                if (_telemetryAllowedScopes.Contains(scope))
                {
                    propertyBag.Add(ActionPropertyNames.ScopeConstStrKey, scope);
                }
                else if (!string.IsNullOrEmpty(scope))
                {
                    propertyBag.Add(ActionPropertyNames.ScopeConstStrKey, "ScopeRedacted");
                }
            }
        }

        private ActionArtifacts CreateGenericAction(MatsScenario scenario, string correlationId, ActionType actionType)
        {
            string actionId = MatsId.Create();
            MatsAction action = new MatsAction(actionId, scenario, correlationId);

            string corrIdTrim = correlationId.TrimCurlyBraces();

            var propertyBag = new ActionPropertyBag(_errorStore);
            var startTimePoint = DateTime.UtcNow;
            propertyBag.Add(ActionPropertyNames.UploadIdConstStrKey, MatsId.Create());
            propertyBag.Add(ActionPropertyNames.ActionTypeConstStrKey, MatsConverter.AsString(actionType));
            propertyBag.Add(ScenarioPropertyNames.IdConstStrKey, scenario?.ScenarioId);
            propertyBag.Add(ActionPropertyNames.CorrelationIdConstStrKey, corrIdTrim);
            propertyBag.Add(ActionPropertyNames.StartTimeConstStrKey, DateTimeUtils.GetMillisecondsSinceEpoch(startTimePoint));

            lock (_lockActionIdToPropertyBag)
            {
                _actionIdToPropertyBag[actionId] = propertyBag;
            }

            return new ActionArtifacts(action, propertyBag);
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
