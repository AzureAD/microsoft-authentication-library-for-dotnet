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
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

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

        public void BeginClientMetricsRecord(IHttpWebRequest request, CallState callState)
        {
            if (callState != null && callState.AuthorityType == AuthorityType.AAD)
            {
                AddClientMetricsHeadersToRequest(request);
                metricsTimer = Stopwatch.StartNew();
            }            
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

        private static void AddClientMetricsHeadersToRequest(IHttpWebRequest request)
        {
            lock (PendingClientMetricsLock)
            {
                if (pendingClientMetrics != null && NetworkPlugin.RequestCreationHelper.RecordClientMetrics)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    if (pendingClientMetrics.lastError != null)
                    {
                        headers[ClientMetricsHeaderLastError] = pendingClientMetrics.lastError;
                    }

                    headers[ClientMetricsHeaderLastRequest] = pendingClientMetrics.lastCorrelationId.ToString();
                    headers[ClientMetricsHeaderLastResponseTime] = pendingClientMetrics.lastResponseTime.ToString();
                    headers[ClientMetricsHeaderLastEndpoint] = pendingClientMetrics.lastEndpoint;

                    HttpHelper.AddHeadersToRequest(request, headers);
                    pendingClientMetrics = null;
                }
            }
        }
    }
}