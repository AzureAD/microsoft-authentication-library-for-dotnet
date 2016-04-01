//------------------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class ClientMetricsEndpointType
    {
        public const string DeviceCode = "device_code";
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
                    parameters[ClientMetricsHeaderLastResponseTime] = pendingClientMetrics.lastResponseTime.ToString(CultureInfo.CurrentCulture);
                    parameters[ClientMetricsHeaderLastEndpoint] = pendingClientMetrics.lastEndpoint;

                    pendingClientMetrics = null;
                }
            }

            return parameters;
        }
    }
}
