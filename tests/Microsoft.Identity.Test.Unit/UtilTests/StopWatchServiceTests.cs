using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class StopWatchServiceTests
    {
        [TestMethod]
        public void MeasureCodeBlock()
        {
            MeasureDurationResult result = StopwatchService.MeasureCodeBlock(() => Thread.Sleep(50));

            Assert.IsGreaterThanOrEqualTo(50, result.Milliseconds, "Measured time is less than expected.");
            Assert.IsLessThan(100, result.Milliseconds, "Measured time is too high.");

            long diff = result.Microseconds - (result.Milliseconds * 1000);
            Assert.IsGreaterThanOrEqualTo(-1000, diff, "Microseconds is less than expected.");
            Assert.IsLessThan(1000, diff, "Microseconds is too high.");
        }

        [TestMethod]
        public async Task MeasureCodeBlockAsync()
        {
            MeasureDurationResult result = await StopwatchService.MeasureCodeBlockAsync(async () =>
            {
                await Task.Delay(50).ConfigureAwait(true);
            }).ConfigureAwait(true);

            Assert.IsGreaterThanOrEqualTo(50, result.Milliseconds, "Measured time is less than expected.");
            Assert.IsLessThan(100, result.Milliseconds, "Measured time is too high.");

            long diff = result.Microseconds - (result.Milliseconds * 1000);
            Assert.IsGreaterThanOrEqualTo(-1000, diff, "Microseconds is less than expected.");
            Assert.IsLessThan(1000, diff, "Microseconds is too high.");
        }

        [TestMethod]
        public async Task MeasureCodeBlockAsyncWithResult()
        {
            MeasureDurationResult<int> result = await StopwatchService.MeasureCodeBlockAsync(async () =>
            {
                await Task.Delay(50).ConfigureAwait(true);
                return 42;
            }).ConfigureAwait(true);

            Assert.AreEqual(42, result.Result, "Result is not as expected.");
            Assert.IsGreaterThanOrEqualTo(50, result.Milliseconds, "Measured time is less than expected.");
            Assert.IsLessThan(100, result.Milliseconds, "Measured time is too high.");

            long diff = result.Microseconds - (result.Milliseconds * 1000);
            Assert.IsGreaterThanOrEqualTo(-1000, diff, "Microseconds is less than expected.");
            Assert.IsLessThan(1000, diff, "Microseconds is too high.");
        }
    }
}
