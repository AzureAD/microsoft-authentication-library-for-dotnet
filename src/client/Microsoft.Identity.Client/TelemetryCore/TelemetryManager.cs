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

        internal readonly ConcurrentDictionary<string, List<EventBase>> _completedEvents =
            new ConcurrentDictionary<string, List<EventBase>>();

        internal readonly ConcurrentDictionary<EventKey, EventBase> _eventsInProgress =
            new ConcurrentDictionary<EventKey, EventBase>();

        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _eventCount =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        private EventBase _mostRecentStoppedApiEvent;
        private readonly object _mostRecentStoppedApiEventLockObj = new object();

        private readonly bool _onlySendFailureTelemetry;
        private readonly IPlatformProxy _platformProxy;
        private readonly IApplicationConfiguration _applicationConfiguration;
        public int SuccessfulSilentCallCount { get; set; } = 0;

        public TelemetryManager(
            IApplicationConfiguration applicationConfiguration,
            IPlatformProxy platformProxy,
            TelemetryCallback telemetryCallback,
            bool onlySendFailureTelemetry = false)
        {
            _mostRecentStoppedApiEvent = null;
            _applicationConfiguration = applicationConfiguration;
            _platformProxy = platformProxy;
            Callback = telemetryCallback;
            _onlySendFailureTelemetry = onlySendFailureTelemetry;
        }

        public TelemetryCallback Callback { get; }

        public TelemetryHelper CreateTelemetryHelper(EventBase eventToStart)
        {
            return new TelemetryHelper(this, eventToStart);
        }

        public void StartEvent(EventBase eventToStart)
        {        
            _eventsInProgress[new EventKey(eventToStart)] = eventToStart;
        }

        public void StopEvent(EventBase eventToStop)
        {
            var eventKey = new EventKey(eventToStop);

            // Locate the same name event in the EventsInProgress map
            _eventsInProgress.TryGetValue(eventKey, out EventBase eventStarted);

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

            if (_completedEvents.TryGetValue(eventToStop.CorrelationId, out List<EventBase> events))
            {
                events.Add(eventToStop);
            }
            else
            {
                _completedEvents.TryAdd(
                    eventToStop.CorrelationId,
                    new List<EventBase>
                    {
                        eventToStop
                    });
            }

            // Mark this event as no longer in progress
            _eventsInProgress.TryRemove(eventKey, out _);

            // Store the most recent API event we've stopped.
            if (eventToStop.TryGetValue(MsalTelemetryBlobEventNames.ApiIdConstStrKey, out _))
            {
                lock (_mostRecentStoppedApiEventLockObj)
                {
                    _mostRecentStoppedApiEvent = eventToStop;
                }
            }
        }

        public void Flush(string correlationId)
        {
            if (!_completedEvents.ContainsKey(correlationId))
            {
                // No completed Events returned for RequestId
                return;
            }

            _completedEvents[correlationId].AddRange(CollateOrphanedEvents(correlationId));
            _completedEvents.TryRemove(correlationId, out List<EventBase> eventsToFlush);
            _eventCount.TryRemove(correlationId, out ConcurrentDictionary<string, int> eventCountToFlush);

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

        public string FetchAndResetPreviousHttpTelemetryContent(EventBase eventBase)
        {
            lock (_mostRecentStoppedApiEventLockObj)
            {
                var httpTelemetryContent = new HttpTelemetryContent(_completedEvents, _mostRecentStoppedApiEvent);
                _mostRecentStoppedApiEvent = null;
                return httpTelemetryContent.GetCsvAsPrevious(SuccessfulSilentCallCount);
            }
        }

        public string FetchCurrentHttpTelemetryContent(EventBase eventBase)
        {
            var httpTelemetryContent = new HttpTelemetryContent(_eventsInProgress, eventBase);
            return httpTelemetryContent.GetCsvAsCurrent();
        }

        private IEnumerable<EventBase> CollateOrphanedEvents(string correlationId)
        {
            var orphanedEvents = new List<EventBase>();
            foreach (var key in _eventsInProgress.Keys)
            {
                if (string.Compare(key.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // The orphaned event already contains its own start time, we simply collect it
                    if (_eventsInProgress.TryRemove(key, out var orphan))
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

            if (!_eventCount.ContainsKey(eventToIncrement.CorrelationId))
            {
                _eventCount[eventToIncrement.CorrelationId] = new ConcurrentDictionary<string, int>();
                _eventCount[eventToIncrement.CorrelationId].TryAdd(eventName, 1);
            }
            else
            {
                _eventCount[eventToIncrement.CorrelationId].AddOrUpdate(eventName, 1, (key, count) => count + 1);
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
                if (other is null)
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
                    hash = (hash * HashingMultiplier) ^ (EventId is object ? EventId.GetHashCode() : 0);
                    hash = (hash * HashingMultiplier) ^ (CorrelationId is object ? CorrelationId.GetHashCode() : 0);
                    hash = (hash * HashingMultiplier) ^ (EventName is object ? EventName.GetHashCode() : 0);

                    return hash;
                }
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is null)
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
