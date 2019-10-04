using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    /// <summary>
    /// Tests copied and adapted from https://github.com/microsoft/RetryOperationHelper/tree/master/RetryOperationHelperUnitTests
    /// </summary>
    [TestClass]
    public class RetryOperationHelperTest
    {
        [TestMethod]
        public void TestExecuteWithRetry_OperationSucceeds()
        {
            //Arrange
            const int numberOfRetriesToAttempt = 3;
            const int numberOfFailuresToSimulate = 2;

            var operationSimulator = new OperationSimulator(numberOfFailuresToSimulate);
            Func<Task<bool>> func = () => operationSimulator.SimulateOperationWithFailuresAsync();

            //Act
            var result = RetryOperationHelper.ExecuteWithRetryAsync(func, numberOfRetriesToAttempt).Result;

            //Assert 
            Assert.AreEqual(result, true);
        }

        [TestMethod]
        public void TestExecuteWithRetry_OperationFails()
        {
            //Arrange
            const int numberOfRetriesToAttempt = 3;
            const int numberOfFailuresToSimulate = 3;

            var operationSimulator = new OperationSimulator(numberOfFailuresToSimulate);
            Func<Task<bool>> func = () => operationSimulator.SimulateOperationWithFailuresAsync();

            //Act
            Exception ex = AssertException.Throws<AggregateException>(
                () => RetryOperationHelper.ExecuteWithRetryAsync(func, numberOfRetriesToAttempt).Wait());

            Assert.AreEqual(ex.InnerException.Message, "OperationSimulator: Simulating Operation Failure");
        }

        [TestMethod]
        public void TestExecuteWithRetry_OperationFails_VerifyOnFailureActionIsCalled()
        {
            //Arrange
            const int numberOfRetriesToAttempt = 3;
            const int numberOfFailuresToSimulate = 3;
            TimeSpan retryTimeSpan = new TimeSpan(0, 0, 0, 1);

            var operationSimulator = new OperationSimulator(numberOfFailuresToSimulate);
            Func<Task<bool>> func = () => operationSimulator.SimulateOperationWithFailuresAsync();
            Action<int, Exception> actionUponFailure = new Action<int, Exception>(operationSimulator.ThrowException);

            //Act
            
            Exception ex = AssertException.Throws<AggregateException>(
                () => RetryOperationHelper.ExecuteWithRetryAsync(func, numberOfRetriesToAttempt, retryTimeSpan, actionUponFailure).Wait());
            Assert.AreEqual(ex.InnerException.Message, "OperationSimulator: ThrowException: Exception thrown to identify method");
        }
    }

    /// <summary>
    /// A class which simulates a given number of failures before succeeding
    /// </summary>
    public class OperationSimulator
    {
        private readonly int _numberOfFailuresToSimulateBeforeSuccess;
        private int _currentNumberOfFailuresSimulated = 0;

        public OperationSimulator(int numberOfFailuresToSimulateBeforeSuccess)
        {
            this._numberOfFailuresToSimulateBeforeSuccess = numberOfFailuresToSimulateBeforeSuccess;
        }

        public async Task<bool> SimulateOperationWithFailuresAsync()
        {
            if (_currentNumberOfFailuresSimulated < _numberOfFailuresToSimulateBeforeSuccess)
            {
                _currentNumberOfFailuresSimulated++;
                throw new InvalidOperationException("OperationSimulator: Simulating Operation Failure");
            }

            await Task.Delay(10).ConfigureAwait(false);

            return true;
        }

        public void ThrowException(int numberOfPreviousFailures, Exception exception)
        {
            throw new InvalidOperationException("OperationSimulator: ThrowException: Exception thrown to identify method");
        }
    }
}
