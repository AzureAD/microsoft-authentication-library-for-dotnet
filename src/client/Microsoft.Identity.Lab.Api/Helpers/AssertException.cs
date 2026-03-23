// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// AssertException provides a set of assertion methods for verifying that specific exceptions are thrown or not thrown by a given piece of code. It includes methods for both synchronous and asynchronous code, allowing developers to easily test exception handling in their unit tests. The class uses a nested Recorder class to execute the test code and capture any exceptions that occur, providing detailed error messages when assertions fail.
    /// </summary>
    public static class AssertException
    {
        /// <summary>
        /// DoesNotThrow verifies that the provided test code does not throw any exceptions. If an exception is thrown, it captures the exception and throws an InvalidOperationException with a message indicating that the assertion failed and includes details of the exception that was thrown.
        /// </summary>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <exception cref="InvalidOperationException">Thrown if the test code throws an exception.</exception>
        public static void DoesNotThrow(Action testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new InvalidOperationException("DoesNotThrow failed - - an exception was thrown {ex}", ex);
            }
        }

        /// <summary>
        /// DoesNotThrow verifies that the provided test code does not throw any exceptions. If an exception is thrown, it captures the exception and throws an InvalidOperationException with a message indicating that the assertion failed and includes details of the exception that was thrown.
        /// </summary>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <exception cref="InvalidOperationException">Thrown if the test code throws an exception.</exception>
        public static void DoesNotThrow(Func<object> testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new InvalidOperationException($"DoesNotThrow failed - an exception was thrown {ex}", ex);
            }
        }

        /// <summary>
        /// Throws verifies that the provided test code throws an exception of type <typeparamref name="TException"/>. If no exception is thrown or a different exception type is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <returns>The exception that was thrown by the test code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the test code does not throw an exception or throws a different exception type.</exception>
        public static TException Throws<TException>(Action testCode)
             where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }

        /// <summary>
        /// Throws verifies that the provided test code throws an exception of type <typeparamref name="TException"/> or a derived type. If no exception is thrown or an incompatible exception type is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <param name="allowDerived">If true, allows exceptions derived from <typeparamref name="TException"/>. If false, requires an exact type match.</param>
        /// <returns>The exception that was thrown by the test code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the test code does not throw an exception or throws an incompatible exception type.</exception>
        public static TException Throws<TException>(Action testCode, bool allowDerived)
             where TException : Exception
        {
            var exception = Recorder.Exception<TException>(testCode);

            if (exception == null)
            {
                throw new InvalidOperationException("AssertExtensions.Throws failed. No exception occurred.");
            }

            CheckExceptionType<TException>(exception, allowDerived);

            return exception;
        }

        /// <summary>
        /// Throws verifies that the provided test code throws an exception of type <typeparamref name="TException"/>. If no exception is thrown or a different exception type is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <returns>The exception that was thrown by the test code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the test code does not throw an exception or throws a different exception type.</exception>
        public static TException Throws<TException>(Func<object> testCode)
            where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }

        /// <summary>
        /// Throws verifies that the provided test code throws an exception of type <typeparamref name="TException"/> or a derived type. If no exception is thrown or an incompatible exception type is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="TException">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The code to test for exceptions.</param>
        /// <param name="allowDerived">If true, allows exceptions derived from <typeparamref name="TException"/>. If false, requires an exact type match.</param>
        /// <returns>The exception that was thrown by the test code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the test code does not throw an exception or throws an incompatible exception type.</exception>
        public static TException Throws<TException>(Func<object> testCode, bool allowDerived)
            where TException : Exception
        {
            var exception = Recorder.Exception<TException>(testCode);

            if (exception == null)
            {
                throw new InvalidOperationException("AssertExtensions.Throws failed. No exception occurred.");
            }

            CheckExceptionType<TException>(exception, allowDerived);

            return exception;
        }

        /// <summary>
        /// TaskThrowsAsync verifies that the provided asynchronous test code throws an exception of type <typeparamref name="T"/> or a derived type. If no exception is thrown or an incompatible exception type is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="T">The type of exception expected to be thrown.</typeparam>
        /// <param name="testCode">The asynchronous code to test for exceptions.</param>
        /// <param name="allowDerived">If true, allows exceptions derived from <typeparamref name="T"/>. If false, requires an exact type match.</param>
        /// <returns>A task that represents the asynchronous operation, containing the exception that was thrown by the test code.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the test code does not throw an exception or throws an incompatible exception type.</exception>
        public static async Task<T> TaskThrowsAsync<T>(Func<Task> testCode, bool allowDerived = false)
            where T : Exception
        {
            Exception exception = null;
            try
            {
                await testCode().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception == null)
            {
                throw new InvalidOperationException("AssertExtensions.TaskThrowsAsync failed. No exception occurred.");
            }

            if (exception is AggregateException aggEx)
            {
                if (aggEx.InnerException.GetType() == typeof(InvalidOperationException))
                {
                    throw aggEx.InnerException;
                }

                var exceptionsMatching = aggEx.InnerExceptions.OfType<T>().ToList();

                if (!exceptionsMatching.Any())
                {
                    ThrowAssertFailedForExceptionMismatch(typeof(T), exception);
                }

                return exceptionsMatching.First();
            }

            CheckExceptionType<T>(exception, allowDerived);

            return (exception as T);
        }

        /// <summary>
        /// TaskDoesNotThrow verifies that the provided asynchronous test code does not throw any exceptions. If an exception is thrown, it captures the exception and throws an InvalidOperationException with a message indicating that the assertion failed and includes details of the exception that was thrown.
        /// </summary>
        /// <param name="testCode">The asynchronous code to test for exceptions.</param>
        /// <exception cref="InvalidOperationException">Thrown if the test code throws an exception.</exception>
        public static void TaskDoesNotThrow(Func<Task> testCode)
        {
            var exception = Recorder.Exception(() => testCode().Wait());

            if (exception == null)
            {
                return;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "AssertExtensions.TaskDoesNotThrow failed. Incorrect exception {0} occurred. Details {1}",
                    exception.GetType().Name, 
                    exception),
                exception);
        }

        /// <summary>
        /// TaskDoesNotThrow verifies that the provided asynchronous test code does not throw an exception of type <typeparamref name="T"/> or any derived type. If such an exception is thrown, an AssertFailedException is raised.
        /// </summary>
        /// <typeparam name="T">The type of exception to verify is not thrown.</typeparam>
        /// <param name="testCode">The asynchronous code to test for exceptions.</param>
        /// <exception cref="InvalidOperationException">Thrown if the test code throws an exception of type <typeparamref name="T"/> or a derived type.</exception>
        public static void TaskDoesNotThrow<T>(Func<Task> testCode) where T : Exception
        {
            var exception = Recorder.Exception<AggregateException>(() => testCode().Wait());

            if (exception == null)
            {
                return;
            }

            var exceptionsMatching = exception.InnerExceptions.OfType<T>().ToList();

            if (!exceptionsMatching.Any())
            {
                return;
            }

            ThrowAssertFailedForExceptionMismatch(typeof(T), exception);
        }

        private static void CheckExceptionType<TException>(Exception actualException, bool allowDerived)
        {
            Type expectedType = typeof(TException);

            if (allowDerived)
            {
                if (!(actualException is TException))
                {
                    ThrowAssertFailedForExceptionMismatch(expectedType, actualException);
                }
            }
            else
            {
                if (!expectedType.Equals(actualException.GetType()))
                {
                    ThrowAssertFailedForExceptionMismatch(expectedType, actualException);
                }
            }
        }

        private static void ThrowAssertFailedForExceptionMismatch(Type expectedExceptionType, Exception actualException)
        {
            Trace.TraceError("Exception match failed. Actual exception is: " + actualException);
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Exception types do not match. Expected: {0}  Actual: {1}. Actual exception details: {2}",
                    expectedExceptionType.Name,
                    actualException.GetType().Name, 
                    actualException),                
                actualException);
        }

        private static class Recorder
        {
            public static Exception Exception(Action code)
            {
                try
                {
                    code();
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            }

            public static TException Exception<TException>(Action code)
                where TException : Exception
            {
                try
                {
                    code();
                    return null;
                }
                catch (TException ex)
                {
                    return ex;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Expected to capture a {typeof(TException)} exception but got {e.GetType()}. Details {e}", 
                        e);
                }
            }

            public static TException Exception<TException>(Func<object> code)
                where TException : Exception
            {
                try
                {
                    code();
                    return null;
                }
                catch (TException ex)
                {
                    return ex;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(
                        $"Expected to capture a {typeof(TException)} exception but got {e.GetType()}. Details {e}", 
                        e);
                }
            }
        }
    }
}

