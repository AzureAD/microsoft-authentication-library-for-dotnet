// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Core.Telemetry
{
    internal class TelemetryManager : ITelemetryManager,
                                      ITelemetry
    {
        private const string MsalCacheEventValuePrefix = "msal.token"; 
        private const string MsalCacheEventName = "msal.cache_event";

        private readonly object _lockObj = new object();

        internal readonly ConcurrentDictionary<string, List<EventBase>> CompletedEvents =
            new ConcurrentDictionary<string, List<EventBase>>();

        internal readonly ConcurrentDictionary<EventKey, EventBase> EventsInProgress =
            new ConcurrentDictionary<EventKey, EventBase>();

        internal ConcurrentDictionary<string, ConcurrentDictionary<string, int>> EventCount = 
            new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        private ITelemetryReceiver _telemetryReceiver;

        public TelemetryManager(ITelemetryReceiver telemetryReceiver = null)
        {
            _telemetryReceiver = telemetryReceiver;
        }

        void ITelemetry.StartEvent(string requestId, EventBase eventToStart)
        {
            StartEvent(requestId, eventToStart);
        }

        void ITelemetry.StopEvent(string requestId, EventBase eventToStop)
        {
            StopEvent(requestId, eventToStop);
        }

        void ITelemetry.Flush(string requestId, string clientId)
        {
            Flush(requestId, clientId);
        }

        public ITelemetryReceiver TelemetryReceiver
        {
            get
            {
                lock (_lockObj)
                {
                    return _telemetryReceiver;
                }
            }
            set
            {
                lock (_lockObj)
                {
                    _telemetryReceiver = value;
                }
            }
        }

        /// <inheritdoc />
        public string GenerateNewRequestId()
        {
            return Guid.NewGuid().ToString();
        }

        public TelemetryHelper CreateTelemetryHelper(
            string requestId,
            string clientId,
            EventBase eventToStart,
            bool shouldFlush = false)
        {
            return new TelemetryHelper(
                this,
                requestId,
                clientId,
                eventToStart,
                shouldFlush);
        }

        private bool HasReceiver()
        {
            lock (_lockObj)
            {
                return _telemetryReceiver != null;
            }
        }

        internal void StartEvent(string requestId, EventBase eventToStart)
        {
            if (!HasReceiver() || string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            EventsInProgress[new EventKey(requestId, eventToStart)] = eventToStart;
        }

        internal void StopEvent(string requestId, EventBase eventToStop)
        {
            if (!HasReceiver() || string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            var eventKey = new EventKey(requestId, eventToStop);

            // Locate the same name event in the EventsInProgress map
            EventBase eventStarted = null;
            if (EventsInProgress.ContainsKey(eventKey))
            {
                eventStarted = EventsInProgress[eventKey];
            }

            // If we did not get anything back from the dictionary, most likely its a bug that StopEvent
            // was called without a corresponding StartEvent
            if (null == eventStarted)
            {
                // Stop Event called without a corresponding start_event.
                return;
            }

            // Set execution time properties on the event adn increment the event count.
            eventToStop.Stop();
            IncrementEventCount(requestId, eventToStop);

            if (!CompletedEvents.ContainsKey(requestId))
            {
                // if this is the first event associated to this
                // RequestId we need to initialize a new List to hold
                // all of sibling events
                var events = new List<EventBase>
                {
                    eventToStop
                };
                CompletedEvents.TryAdd(requestId, events);
            }
            else
            {
                // if this event shares a RequestId with other events
                // just add it to the List
                if (CompletedEvents.TryGetValue(requestId, out List<EventBase> events))
                {
                    events.Add(eventToStop);
                }
            }

            // Mark this event as no longer in progress
            EventsInProgress.TryRemove(eventKey, out var dummy);
        }

        internal void Flush(string requestId, string clientId)
        {
            if (!HasReceiver())
            {
                return;
            }

            if (!CompletedEvents.ContainsKey(requestId))
            {
                // No completed Events returned for RequestId
                return;
            }

            CompletedEvents[requestId].AddRange(CollateOrphanedEvents(requestId));
            CompletedEvents.TryRemove(requestId, out List<EventBase> eventsToFlush);
            EventCount.TryRemove(requestId, out ConcurrentDictionary<string, int> eventCountToFlush);

            bool onlySendFailureTelemetry;
            lock (_lockObj)
            {
                onlySendFailureTelemetry = _telemetryReceiver?.OnlySendFailureTelemetry ?? false;
            }

            // Check all events, and if the ApiEvent was successful, don't dispatch.
            if (onlySendFailureTelemetry && eventsToFlush.Any(ev => ev is ApiEvent a && a.WasSuccessful))
            {
                eventsToFlush.Clear();
            }

            if (eventsToFlush.Count <= 0)
            {
                return;
            }

            if (eventCountToFlush != null)
            {
                eventsToFlush.Insert(0, new DefaultEvent(clientId, eventCountToFlush));
            }
            else
            {
                eventsToFlush.Insert(0, new DefaultEvent(clientId, new ConcurrentDictionary<string, int>()));
            }

            lock (_lockObj)
            {
                _telemetryReceiver?.HandleTelemetryEvents(eventsToFlush.Cast<Dictionary<string, string>>().ToList());
            }
        }

        private IEnumerable<EventBase> CollateOrphanedEvents(string requestId)
        {
            var orphanedEvents = new List<EventBase>();
            foreach (var key in EventsInProgress.Keys)
            {
                if (string.Compare(key.RequestId, requestId, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // The orphaned event already contains its own start time, we simply collect it
                    if (EventsInProgress.TryRemove(key, out var orphan))
                    {
                        IncrementEventCount(requestId, orphan);
                        orphanedEvents.Add(orphan);
                    }
                }
            }

            return orphanedEvents;
        }

        private void IncrementEventCount(string requestId, EventBase eventToIncrement) 
        { 
            string eventName; 
            if (eventToIncrement[EventBase.EventNameKey].Substring(0,10) == MsalCacheEventValuePrefix) 
            { 
                eventName = MsalCacheEventName; 
            } 
            else 
            { 
                eventName = eventToIncrement[EventBase.EventNameKey]; 
            } 

            if (!EventCount.ContainsKey(requestId)) 
            { 
                EventCount[requestId] = new ConcurrentDictionary<string, int>(); 
                EventCount[requestId].TryAdd(eventName, 1); 
            } 
            else 
            { 
                EventCount[requestId].AddOrUpdate(eventName, 1, (key, count) => count + 1); 
            } 
        } 

        internal class EventKey : IEquatable<EventKey>
        {
            public EventKey(string requestId, EventBase eventBase)
            {
                RequestId = requestId;
                EventName = eventBase[EventBase.EventNameKey];
            }

            public string RequestId { get; }
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

                return string.Equals(RequestId, other.RequestId, StringComparison.OrdinalIgnoreCase) && 
                       string.Equals(EventName, other.EventName, StringComparison.OrdinalIgnoreCase);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((RequestId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(RequestId) : 0) * 397) ^
                            (EventName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(EventName) : 0);
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