using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.netcore.AzureTests
{
    [TestClass]
    public class ExponentialBackoffTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        [TestCategory("ExponentialBackoffTests")]
        public async Task ExponentialBackoffTestShouldThrowTooManyRetriesAsync()
        {
            var subject = new ExponentialBackoff(1, 1, 2);
            await subject.DelayAsync().ConfigureAwait(false);
            await Assert.ThrowsExceptionAsync<TooManyRetryAttemptsException>(async () => await subject.DelayAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ExponentialBackoffTests")]
        public void ExponentialBackoffTestShouldProgressExponentially()
        {
            var maxDelay = 500000;
            var maxLog = (int)Math.Ceiling(Math.Log(maxDelay, 2));
            var subject = new ExponentialBackoff(20, 1, maxDelay);
            for(var i = 0; i < maxLog; i++)
            {
                Assert.AreEqual(TimeSpan.FromMilliseconds(Math.Pow(2, i)), subject.GetDelay(i));
            }

            for(var i = maxLog; i < 5000; i++)
            {
                Assert.AreEqual(TimeSpan.FromMilliseconds(maxDelay), subject.GetDelay(i));
            }
        }

        [TestMethod]
        [TestCategory("ExponentialBackoffTests")]
        public void ExponentialBackoffTestShouldThrowIfInitialDelayIsLessThanOrEqualToZero()
        {
            Assert.ThrowsException<ArgumentException>(() => new ExponentialBackoff(20, 0, 0));
            Assert.ThrowsException<ArgumentException>(() => new ExponentialBackoff(20, -1, 0));
        }
    }
}
