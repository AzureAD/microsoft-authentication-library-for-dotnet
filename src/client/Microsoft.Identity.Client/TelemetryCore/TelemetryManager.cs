// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal class TelemetryManager : ITelemetryManager
    {
        private const string MsalCacheEventValuePrefix = "msal.token";
        private const string MsalCacheEventName = "msal.cache_event";

        internal readonly ConcurrentDictionary<string, List<EventBase>> CompletedEvents =
            new ConcurrentDictionary<string, List<EventBase>>();

        internal readonly ConcurrentDictionary<EventKey, EventBase> EventsInProgress =
            new ConcurrentDictionary<EventKey, EventBase>();

        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> EventCount =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        private readonly bool _onlySendFailureTelemetry;
        private readonly IPlatformProxy _platformProxy;
        private readonly IApplicationConfiguration _applicationConfiguration;
        internal readonly HttpTelemetryContent _currentTelemetryPayload;
        internal readonly HttpTelemetryContent _previousTelemetryPayload;

        public TelemetryManager(
            IApplicationConfiguration applicationConfiguration,
            IPlatformProxy platformProxy,
            TelemetryCallback telemetryCallback,
            bool onlySendFailureTelemetry = false)
        {
            _applicationConfiguration = applicationConfiguration;
            _platformProxy = platformProxy;
            Callback = telemetryCallback;
            _onlySendFailureTelemetry = onlySendFailureTelemetry;
            _currentTelemetryPayload = new HttpTelemetryContent();
            _previousTelemetryPayload = new HttpTelemetryContent();
        }

        public TelemetryCallback Callback { get; }

        public TelemetryHelper CreateTelemetryHelper(EventBase eventToStart)
        {
            return new TelemetryHelper(this, eventToStart);
        }

        private bool HasReceiver()
        {
            return Callback != null;
        }

        public void StartEvent(EventBase eventToStart)
        {
            ProcessEventsForCurrentHttpTelemetryContent(eventToStart);

            if (!HasReceiver())
            {
                return;
            }

            EventsInProgress[new EventKey(eventToStart)] = eventToStart;
        }

        public void StopEvent(EventBase eventToStop)
        {
            ProcessEventsForPreviousHttpTelemetryContent(eventToStop); //try movnig up to start

            if (!HasReceiver())
            {
                return;
            }

            var eventKey = new EventKey(eventToStop);

            // Locate the same name event in the EventsInProgress map
            EventBase eventStarted = null;
            EventsInProgress.TryGetValue(eventKey, out eventStarted);

            // If we did not get anything back from the dictionary, most likely its a bug that StopEvent
            // was called without a corresponding StartEvent
            if (null == eventStarted)
            {
                // Stop Event called without a corresponding start_event.
                return;
            }

            // Set execution time properties on the event and increment the event count.
            eventToStop.Stop();
            IncrementEventCount(eventToStop);

            if (CompletedEvents.TryGetValue(eventToStop.CorrelationId, out List<EventBase> events))
            {
                events.Add(eventToStop);
            }
            else
            {
                CompletedEvents.TryAdd(
                    eventToStop.CorrelationId,
                    new List<EventBase>
                    {
                        eventToStop
                    });
            }

            // Mark this event as no longer in progress
            EventsInProgress.TryRemove(eventKey, out var dummy);
        }

        public void Flush(string correlationId)
        {
            if (!HasReceiver())
            {
                return;
            }

            if (!CompletedEvents.ContainsKey(correlationId))
            {
                // No completed Events returned for RequestId
                return;
            }

            CompletedEvents[correlationId].AddRange(CollateOrphanedEvents(correlationId));
            CompletedEvents.TryRemove(correlationId, out List<EventBase> eventsToFlush);
            EventCount.TryRemove(correlationId, out ConcurrentDictionary<string, int> eventCountToFlush);

            // Check all events, and if the ApiEvent was successful, don't dispatch.
            if (_onlySendFailureTelemetry && eventsToFlush.Any(ev => ev is ApiEvent a && a.WasSuccessful))
            {
                eventsToFlush.Clear();
            }

            if (!eventsToFlush.Any())
            {
                return;
            }

            eventsToFlush.Insert(0, new DefaultEvent(_platformProxy, correlationId, _applicationConfiguration.ClientId, eventCountToFlush ?? new ConcurrentDictionary<string, int>()));
            Callback?.Invoke(eventsToFlush.Cast<Dictionary<string, string>>().ToList());
        }

        public string FetchAndResetPreviousHttpTelemetryContent()
        {
            if (!string.IsNullOrEmpty(_previousTelemetryPayload.ApiId))
            {
                string csv = CreateCSVForPreviousHttpTelem();
                _currentTelemetryPayload.ResetLastErrorCode();
                return csv;
            }
            else
            {
                return string.Empty;
            }
        }

        public string FetchAndResetCurrentHttpTelemetryContent()
        {
            if (!string.IsNullOrEmpty(_currentTelemetryPayload.ApiId))
            {               
                string csv = CreateCSVForCurrentHttpTelem();
                _currentTelemetryPayload.ResetLastErrorCode();
                return csv;
            }
            else
            {
                return string.Empty;
            }
        }

        private string CreateCSVForPreviousHttpTelem()
        {
            // csv expected format:
            // 1|api_id, correlation_id, last_error_code
            string[] myValues = new string[] {
                _previousTelemetryPayload.ApiId,
                _previousTelemetryPayload.CorrelationId,
                _previousTelemetryPayload.LastErrorCode};

            string csvString = string.Join(",", myValues);
            csvString = TelemetryConstants.HttpTelemetrySchemaVersion1 + csvString;
            return csvString;
        }

        private string CreateCSVForCurrentHttpTelem()
        {
            // csv expected format:
            // 1|api_id|platform_config
            string csvString = TelemetryConstants.HttpTelemetrySchemaVersion1 + _currentTelemetryPayload.ApiId;
            return csvString;
        }

        private void ProcessEventsForCurrentHttpTelemetryContent(EventBase events)
        {
            foreach (var ev in events)
            {
                if (string.Equals(ev.Key, MsalTelemetryBlobEventNames.ApiIdConstStrKey))
                {
                    _currentTelemetryPayload.ApiId = ev.Value;
                }

                if (string.Equals(ev.Key, MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey))
                {
                    _currentTelemetryPayload.CorrelationId = ev.Value;
                }
            }
        }

        private void ProcessEventsForPreviousHttpTelemetryContent(EventBase events)
        {
            foreach (var ev in events)
            {
                if (string.Equals(ev.Key, MsalTelemetryBlobEventNames.ApiIdConstStrKey))
                {
                    _previousTelemetryPayload.ApiId = ev.Value;
                }

                if (string.Equals(ev.Key, MsalTelemetryBlobEventNames.ApiErrorCodeConstStrKey))
                {
                    _previousTelemetryPayload.LastErrorCode = ev.Value;
                }

                if (string.Equals(ev.Key, MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey))
                {
                    _previousTelemetryPayload.CorrelationId = ev.Value;
                }
            }
        }

        private IEnumerable<EventBase> CollateOrphanedEvents(string correlationId)
        {
            var orphanedEvents = new List<EventBase>();
            foreach (var key in EventsInProgress.Keys)
            {
                if (string.Compare(key.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // The orphaned event already contains its own start time, we simply collect it
                    if (EventsInProgress.TryRemove(key, out var orphan))
                    {
                        IncrementEventCount(orphan);
                        orphanedEvents.Add(orphan);
                    }
                }
            }

            return orphanedEvents;
        }

        private void IncrementEventCount(EventBase eventToIncrement)
        {
            string eventName;
            if (eventToIncrement[EventBase.EventNameKey].Substring(0, 10) == MsalCacheEventValuePrefix)
            {
                eventName = MsalCacheEventName;
            }
            else
            {
                eventName = eventToIncrement[EventBase.EventNameKey];
            }

            if (!EventCount.ContainsKey(eventToIncrement.CorrelationId))
            {
                EventCount[eventToIncrement.CorrelationId] = new ConcurrentDictionary<string, int>();
                EventCount[eventToIncrement.CorrelationId].TryAdd(eventName, 1);
            }
            else
            {
                EventCount[eventToIncrement.CorrelationId].AddOrUpdate(eventName, 1, (key, count) => count + 1);
            }
        }

        internal class EventKey : IEquatable<EventKey>
        {
            public EventKey(EventBase eventBase)
            {
                CorrelationId = eventBase.CorrelationId;
                EventId = eventBase.EventId;
                EventName = eventBase[EventBase.EventNameKey];
            }

            public string CorrelationId { get; }
            public string EventId { get; }
            public string EventName { get; }

            /// <inheritdoc />
            public bool Equals(EventKey other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return string.Equals(CorrelationId, other.CorrelationId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(EventId, other.EventId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(EventName, other.EventName, StringComparison.OrdinalIgnoreCase);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    // Choose large primes to avoid hashing collisions
                    const int HashingBase = (int)2166136261;
                    const int HashingMultiplier = 16777619;

                    int hash = HashingBase;
                    hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, EventId) ? EventId.GetHashCode() : 0);
                    hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, CorrelationId) ? CorrelationId.GetHashCode() : 0);
                    hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, EventName) ? EventName.GetHashCode() : 0);

                    return hash;
                }
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((EventKey)obj);
            }

            public static bool operator ==(EventKey left, EventKey right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(EventKey left, EventKey right)
            {
                return !Equals(left, right);
            }
        }
    }
}
