//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class ClientMetricsEndpointType
    {
        public const string Token = "token";
        public const string UserRealmDiscovery = "user_realm";
        public const string InstanceDiscovery = "instance";
    }
    
    internal class ClientMetrics
    {
        private const string ClientMetricsHeaderLastError = "x-client-last-error";
        private const string ClientMetricsHeaderLastRequest = "x-client-last-request";
        private const string ClientMetricsHeaderLastResponseTime = "x-client-last-response-time";
        private const string ClientMetricsHeaderLastEndpoint = "x-client-last-endpoint";

        private static ClientMetrics pendingClientMetrics;
        private static readonly object PendingClientMetricsLock = new object();

        private Stopwatch metricsTimer;
        private string lastError;
        private Guid lastCorrelationId;
        private long lastResponseTime;
        private string lastEndpoint;

        public void BeginClientMetricsRecord(CallState callState)
        {
            if (callState != null && callState.AuthorityType == AuthorityType.AAD)
            {
                metricsTimer = Stopwatch.StartNew();
            }
        }

        public Dictionary<string, string> GetPreviousRequestRecord(CallState callState)
        {
            Dictionary<string, string> parameters;
            if (callState != null && callState.AuthorityType == AuthorityType.AAD)
            {
                parameters = GetClientMetricsParameters();
            }
            else
            {
                parameters = new Dictionary<string, string>();
            }

            return parameters;
        }

        public void EndClientMetricsRecord(string endpoint, CallState callState)
        {
            if (callState != null && callState.AuthorityType == AuthorityType.AAD && metricsTimer != null)
            {
                metricsTimer.Stop();
                lastResponseTime = metricsTimer.ElapsedMilliseconds;
                lastCorrelationId = callState.CorrelationId;
                lastEndpoint = endpoint;
                lock (PendingClientMetricsLock)
                {
                    if (pendingClientMetrics == null)
                    {
                        pendingClientMetrics = this;
                    }
                }
            }
        }

        public void SetLastError(string[] errorCodes)
        {
            lastError = (errorCodes != null) ? string.Join(",", errorCodes) : null;
        }

        private static Dictionary<string, string> GetClientMetricsParameters()
        {
            var parameters = new Dictionary<string, string>();
            lock (PendingClientMetricsLock)
            {
                if (pendingClientMetrics != null)
                {
                    if (pendingClientMetrics.lastError != null)
                    {
                        parameters[ClientMetricsHeaderLastError] = pendingClientMetrics.lastError;
                    }

                    parameters[ClientMetricsHeaderLastRequest] = pendingClientMetrics.lastCorrelationId.ToString();
                    parameters[ClientMetricsHeaderLastResponseTime] = pendingClientMetrics.lastResponseTime.ToString();
                    parameters[ClientMetricsHeaderLastEndpoint] = pendingClientMetrics.lastEndpoint;

                    pendingClientMetrics = null;
                }
            }

            return parameters;
        }
    }
}