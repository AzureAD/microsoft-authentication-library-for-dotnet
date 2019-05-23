// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.MatsTests
{
    [TestClass]
    public class ActionAggregatorTests : AbstractMatsTest
    {
        [TestInitialize]
        public override void Setup() => base.Setup();

        [TestCleanup]
        public override void TearDown() => base.TearDown();

        [TestMethod]
        public void ExpectedPropertiesAggregateCorrectly()
        {
            var target = new ActionPropertyBag(_errorStore);
            var child = new ActionPropertyBag(_errorStore);

            // duration properties and names
            string durationMaxPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MaxConstStrSuffix;
            string durationMinPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MinConstStrSuffix;
            string durationSumPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.SumConstStrSuffix;
            long targetDuration = 100;
            long childDuration = 200;

            // cache event count properties and names
            string cacheEventCountMaxPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.MaxConstStrSuffix;
            string cacheEventCountMinPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.MinConstStrSuffix;
            string cacheEventCountSumPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.SumConstStrSuffix;
            int targetCacheEventCount = 100;
            int childCacheEventCount = 200;

            int expectedCount = 2;
            int startingCount = 1;

            // Add int64 duration properties and starting counts
            target.Add(durationMaxPropertyName, targetDuration);
            child.Add(durationMaxPropertyName, childDuration);
            target.Add(durationMinPropertyName, targetDuration);
            child.Add(durationMinPropertyName, childDuration);
            target.Add(durationSumPropertyName, targetDuration);
            child.Add(durationSumPropertyName, childDuration);
            target.Add(ActionPropertyNames.CountConstStrKey, startingCount);
            child.Add(ActionPropertyNames.CountConstStrKey, startingCount);

            // Add int32 cache event count properties
            target.Add(cacheEventCountMaxPropertyName, targetCacheEventCount);
            child.Add(cacheEventCountMaxPropertyName, childCacheEventCount);
            target.Add(cacheEventCountMinPropertyName, targetCacheEventCount);
            child.Add(cacheEventCountMinPropertyName, childCacheEventCount);
            target.Add(cacheEventCountSumPropertyName, targetCacheEventCount);
            child.Add(cacheEventCountSumPropertyName, childCacheEventCount);

            ActionAggregator.AggregateActions(target, child);
            var targetContents = target.GetContents();

            Assert.AreEqual(targetContents.Int64Properties[durationMaxPropertyName], childDuration);
            Assert.AreEqual(targetContents.Int64Properties[durationMinPropertyName], targetDuration);
            Assert.AreEqual(targetContents.Int64Properties[durationSumPropertyName], childDuration + targetDuration);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountMaxPropertyName], childCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountMinPropertyName], targetCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountSumPropertyName], childCacheEventCount + targetCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[ActionPropertyNames.CountConstStrKey], expectedCount);
        }

        [TestMethod]
        public void ChildPropertiesAddedToTarget()
        {
            var target = new ActionPropertyBag(_errorStore);
            var child = new ActionPropertyBag(_errorStore);

            // duration properties and names
            string durationMaxPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MaxConstStrSuffix;
            string durationMinPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MinConstStrSuffix;
            string durationSumPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.SumConstStrSuffix;
            long childDuration = 200;

            // cache event count properties and names
            string cacheEventCountMaxPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.MaxConstStrSuffix;
            string cacheEventCountMinPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.MinConstStrSuffix;
            string cacheEventCountSumPropertyName = MsalTelemetryBlobEventNames.CacheEventCountConstStrKey + ActionPropertyNames.SumConstStrSuffix;
            int childCacheEventCount = 200;

            int expectedCount = 2;
            int startingCount = 1;

            // Add int64 duration properties to child only, count to both
            child.Add(durationMaxPropertyName, childDuration);
            child.Add(durationMinPropertyName, childDuration);
            child.Add(durationSumPropertyName, childDuration);
            target.Add(ActionPropertyNames.CountConstStrKey, startingCount);
            child.Add(ActionPropertyNames.CountConstStrKey, startingCount);

            // Add int32 cache event count properties to child only
            child.Add(cacheEventCountMaxPropertyName, childCacheEventCount);
            child.Add(cacheEventCountMinPropertyName, childCacheEventCount);
            child.Add(cacheEventCountSumPropertyName, childCacheEventCount);

            ActionAggregator.AggregateActions(target, child);
            var targetContents = target.GetContents();

            Assert.AreEqual(targetContents.Int64Properties[durationMaxPropertyName], childDuration);
            Assert.AreEqual(targetContents.Int64Properties[durationMinPropertyName], childDuration);
            Assert.AreEqual(targetContents.Int64Properties[durationSumPropertyName], childDuration);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountMaxPropertyName], childCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountMinPropertyName], childCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[cacheEventCountSumPropertyName], childCacheEventCount);
            Assert.AreEqual(targetContents.IntProperties[ActionPropertyNames.CountConstStrKey], expectedCount);
        }

        [TestMethod]
        public void MissingPropertiesDoNotAggregate()
        {
            var target = new ActionPropertyBag(_errorStore);
            var child = new ActionPropertyBag(_errorStore);
            string basePropertyName = ActionPropertyNames.DurationConstStrKey;
            string averagePropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.AverageConstStrSuffix;
            string maxPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MaxConstStrSuffix;
            string minPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.MinConstStrSuffix;
            string sumPropertyName = ActionPropertyNames.DurationConstStrKey + ActionPropertyNames.SumConstStrSuffix;

            target.Add(basePropertyName, 100);
            child.Add(basePropertyName, 100);

            ActionAggregator.AggregateActions(target, child);
            var targetContents = target.GetContents();

            Assert.IsFalse(targetContents.IntProperties.ContainsKey(averagePropertyName));
            Assert.IsFalse(targetContents.IntProperties.ContainsKey(maxPropertyName));
            Assert.IsFalse(targetContents.IntProperties.ContainsKey(minPropertyName));
            Assert.IsFalse(targetContents.IntProperties.ContainsKey(sumPropertyName));
        }
    }
}
