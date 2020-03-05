// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.Infrastructure
{
    public class HttpTelemetryRecorder
    {
        public List<string> ApiId { get; set; } = new List<string>();
        public List<string> CorrelationIdPrevious { get; set; } = new List<string>();
        public List<string> ApiIdPrevious { get; set; } = new List<string>();
        public List<string> ErrorCode { get; set; } = new List<string>();
        public string ForceRefresh { get; set; }
        public string SilentCallSuccessfulCount { get; set; }

        public void CheckSchemaVersion(string telemetryCsv)
        {
            Assert.AreEqual(
                TelemetryConstants.HttpTelemetrySchemaVersion2,
                 telemetryCsv.StartsWith(TelemetryConstants.HttpTelemetrySchemaVersion2));
        }

        public void SplitCurrentCsv(string telemetryCsv)
        {
            string[] splitCsv = telemetryCsv.Split('|');
            string[] splitApiIdAndForceRefresh = splitCsv[1].Split(',');
            ApiId.Add(splitApiIdAndForceRefresh[0]);
            string forceRefresh = splitApiIdAndForceRefresh[1];
            ForceRefresh = forceRefresh;
        }

        public void SplitPreviousCsv(string telemetryCsv)
        {
            if (!string.IsNullOrEmpty(telemetryCsv))
            {
                string[] splitCsv = telemetryCsv.Split('|');
                SilentCallSuccessfulCount = splitCsv[1];

                if (splitCsv[2] == string.Empty)
                {
                    return;
                }

                string[] splitFailedRequests = splitCsv[2].Split(',');
                ApiIdPrevious.Add(splitFailedRequests[1]);
                CorrelationIdPrevious.Add(splitFailedRequests[2]);
                ErrorCode.Add(splitCsv[2]);
            }
        }
    }
}
