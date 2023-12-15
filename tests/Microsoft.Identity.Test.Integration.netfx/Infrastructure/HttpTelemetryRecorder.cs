// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public class HttpTelemetryRecorder
    {
        public List<string> ApiId { get; set; } = new List<string>();
        public List<string> ErrorCode { get; set; } = new List<string>();
        public bool ForceRefresh { get; set; }
        public string SilentCallSuccessfulCount { get; set; }
        public List<string> ApiIdAndCorrelationIds { get; set; } = new List<string>();

        public void CheckSchemaVersion(string telemetryCsv)
        {
            Assert.IsNotNull(telemetryCsv.StartsWith(TelemetryConstants.HttpTelemetrySchemaVersion));
        }

        public void SplitCurrentCsv(string telemetryCsv)
        {
            string[] splitCsv = telemetryCsv.Split('|');
            string[] splitApiIdAndCacheInfo = splitCsv[1].Split(',');
            ApiId.Add(splitApiIdAndCacheInfo[0]);
            Enum.TryParse(splitApiIdAndCacheInfo[1], out CacheRefreshReason cacheInfoTelemetry);
            ForceRefresh = CacheRefreshReason.ForceRefreshOrClaims == cacheInfoTelemetry;
        }

        public void SplitPreviousCsv(string telemetryCsv)
        {
            if (!string.IsNullOrEmpty(telemetryCsv))
            {
                string[] splitCsv = telemetryCsv.Split('|');
                SilentCallSuccessfulCount = splitCsv[1];

                ApiIdAndCorrelationIds.Add(splitCsv[2]);
                ErrorCode.Add(splitCsv[3]);
            }
        }
    }
}
