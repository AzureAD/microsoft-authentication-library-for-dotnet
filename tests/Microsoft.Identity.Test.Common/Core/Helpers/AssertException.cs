// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class AssertException
    {

        public static void DoesNotThrow(Action testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new AssertFailedException("DoesNotThrow failed.", ex);
            }
        }


        public static void DoesNotThrow(Func<object> testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new AssertFailedException("DoesNotThrow failed.", ex);
            }
        }


        public static TException Throws<TException>(Action testCode)
             where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }


        public static TException Throws<TException>(Action testCode, bool allowDerived)
             where TException : Exception
        {
            var exception = Recorder.Exception<TException>(testCode);

            if (exception == null)
            {
                throw new AssertFailedException("AssertExtensions.Throws failed. No exception occurred.");
            }

            CheckExceptionType<TException>(exception, allowDerived);

            return exception;
        }


        public static TException Throws<TException>(Func<object> testCode)
            where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }


        public static TException Throws<TException>(Func<object> testCode, bool allowDerived)
            where TException : Exception
        {
            var exception = Recorder.Exception<TException>(testCode);

            if (exception == null)
            {
                throw new AssertFailedException("AssertExtensions.Throws failed. No exception occurred.");
            }

            CheckExceptionType<TException>(exception, allowDerived);

            return exception;
        }

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
                throw new AssertFailedException("AssertExtensions.Throws failed. No exception occurred.");
            }

            if (exception is AggregateException aggEx)
            {
                if (aggEx.InnerException.GetType() == typeof(AssertFailedException))
                {
                    throw aggEx.InnerException;
                }

                var exceptionsMatching = aggEx.InnerExceptions.OfType<T>().ToList();

                if (!exceptionsMatching.Any())
                {
                    throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.Throws failed. Incorrect exception {0} occurred.", exception.GetType().Name), exception);
                }

                return exceptionsMatching.First();
            }

            CheckExceptionType<T>(exception, allowDerived);

            return (exception as T);
        }


        public static T TaskThrows<T>(Func<Task> testCode, bool allowDerived = false)
            where T : Exception
        {
            return TaskThrowsAsync<T>(testCode).GetAwaiter().GetResult();
        }

        public static void TaskDoesNotThrow(Func<Task> testCode)
        {
            var exception = Recorder.Exception(() => testCode().Wait());

            if (exception == null)
            {
                return;
            }

            throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.TaskDoesNotThrow failed. Incorrect exception {0} occurred.", exception.GetType().Name), exception);
        }

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

            throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.Throws failed. Incorrect exception {0} occurred.", exception.GetType().Name), exception);
        }


        private static void CheckExceptionType<TException>(Exception actualException, bool allowDerived)
        {
            Type expectedType = typeof(TException);

            string message = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "Checking exception:{0}\tType:{1}{0}\tToString: {2}{0}",
                Environment.NewLine,
                actualException.GetType().FullName,
                actualException.ToString());
            Debug.WriteLine(message);

            if (allowDerived)
            {
                if (!(actualException is TException))
                {
                    throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.Throws failed. Incorrect exception {0} occurred.", expectedType.Name),
                        actualException);
                }
            }
            else
            {
                if (!expectedType.Equals(actualException.GetType()))
                {
                    throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.Throws failed. Incorrect exception {0} occurred.", expectedType.Name),
                        actualException);
                }
            }
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
                    throw new AssertFailedException($"Expected to capture a {typeof(TException)} exception but got {e.GetType()}");
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
                    throw new AssertFailedException($"Expected to capture a {typeof(TException)} exception but got {e.GetType()}");
                }
            }
        }
    }
}

