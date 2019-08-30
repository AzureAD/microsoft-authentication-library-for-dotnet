// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal enum EventType
    {
        Scenario,
        Action,
        LibraryError
    }

    internal class ScenarioStore : IScenarioStore
    {
        private readonly Dictionary<string, ScenarioHolder> _scenarioIdToPropertyBag;
        private readonly object _lockScenarioIdToPropertyBag = new object();
        private readonly IErrorStore _errorStore;
        private readonly int _maxInactiveScenarioDurationMillis;

        public ScenarioStore(int maxInactiveScenarioDurationMillis, IErrorStore errorStore)
        {
            _scenarioIdToPropertyBag = new Dictionary<string, ScenarioHolder>();
            _maxInactiveScenarioDurationMillis = maxInactiveScenarioDurationMillis;
            _errorStore = errorStore;
        }

        public MatsScenario CreateScenario()
        {
            string scenarioId = MatsId.Create();

            var propertyBag = new PropertyBag(EventType.Scenario, _errorStore);
            propertyBag.Add(ScenarioPropertyNames.UploadIdConstStrKey, MatsId.Create());
            propertyBag.Add(ScenarioPropertyNames.IdConstStrKey, scenarioId);

            lock (_lockScenarioIdToPropertyBag)
            {
                _scenarioIdToPropertyBag[scenarioId] = new ScenarioHolder(propertyBag);
            }

            return new MatsScenario(scenarioId, 0);
        }

        public IEnumerable<IPropertyBag> GetEventsForUpload()
        {
            lock (_lockScenarioIdToPropertyBag)
            {
                var retVal = new List<IPropertyBag>();
                var keysToDelete = new List<string>();

                foreach (var kvp in _scenarioIdToPropertyBag)
                {
                    var scenarioHolder = kvp.Value;
                    if (scenarioHolder.ShouldUpload)
                    {
                        retVal.Add(scenarioHolder.PropertyBag);
                        keysToDelete.Add(kvp.Key);
                    }
                    else
                    {
                        DateTime currentTime = DateTime.UtcNow;
                        TimeSpan duration = currentTime - scenarioHolder.StartTime;
                        if (duration.TotalMilliseconds > _maxInactiveScenarioDurationMillis)
                        {
                            keysToDelete.Add(kvp.Key);
                        }
                    }
                }

                foreach (string key in keysToDelete)
                {
                    _scenarioIdToPropertyBag.Remove(key);
                }

                return retVal;
            }
        }

        public void ClearCompletedScenarios()
        {
            // Explicitly ignore the return list, we're just clearing them out.
            GetEventsForUpload();
        }

        public void NotifyActionCompleted(string scenarioId)
        {
            lock (_lockScenarioIdToPropertyBag)
            {
                if (!string.IsNullOrEmpty(scenarioId) && _scenarioIdToPropertyBag.TryGetValue(scenarioId, out ScenarioHolder scenarioHolder))
                {
                    scenarioHolder.ShouldUpload = true;
                }
            }
        }
    }
}
