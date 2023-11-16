﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.TelemetryCore.OpenTelemetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public class OTelInstrumentationUtil
    {
        public static void VerifyActivity(int expectedTagCount, List<Activity> _exportedActivities)
        {
            Assert.AreEqual(1, _exportedActivities.Count);
            foreach (var activity in _exportedActivities)
            {
                Assert.AreEqual(OtelInstrumentation.ActivitySourceName, activity.Source.Name);
                Assert.AreEqual(expectedTagCount, activity.Tags.Count());
            }
        }

        public static void VerifyMetrics(int expectedMetricCount, List<Metric> exportedMetrics)
        {
            Assert.AreEqual(expectedMetricCount, exportedMetrics.Count);

            foreach (Metric exportedItem in exportedMetrics)
            {
                int expectedTagCount = 0;
                List<string> expectedTags = new List<string>();

                Assert.AreEqual(OtelInstrumentation.MeterName, exportedItem.MeterName);

                switch (exportedItem.Name)
                {
                    case "MsalSuccess":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTagCount = 6;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheInfoTelemetry);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;
                    case "MsalFailed":
                        Assert.AreEqual(MetricType.LongSum, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ErrorCode);

                        break;

                    case "MsalTotalDuration.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 5;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;

                    case "MsalTotalDurationInUs.1B":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 5;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);
                        expectedTags.Add(TelemetryConstants.TokenSource);
                        expectedTags.Add(TelemetryConstants.CacheLevel);

                        break;

                    case "MsalDurationInCache.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        break;

                    case "MsalDurationInHttp.1A":
                        Assert.AreEqual(MetricType.Histogram, exportedItem.MetricType);

                        expectedTagCount = 3;
                        expectedTags.Add(TelemetryConstants.MsalVersion);
                        expectedTags.Add(TelemetryConstants.Platform);
                        expectedTags.Add(TelemetryConstants.ApiId);

                        break;

                    default:
                        Assert.Fail("Unexpected metrics logged.");
                        break;
                }

                foreach (var metricPoint in exportedItem.GetMetricPoints())
                {
                    AssertTags(metricPoint.Tags, expectedTagCount, expectedTags);
                }
            }
        }

        private static void AssertTags(ReadOnlyTagCollection tags, int expectedTagCount, List<string> expectedTags)
        {
            Assert.AreEqual(expectedTagCount, tags.Count);
            IDictionary<string, object> tagDictionary = new Dictionary<string, object>();

            foreach (var tag in tags)
            {
                tagDictionary[tag.Key] = tag.Value;
            }

            foreach (var expectedTag in expectedTags)
            {
                Assert.IsNotNull(tagDictionary[expectedTag], $"Tag {expectedTag} is missing.");
            }
        }
    }
}
