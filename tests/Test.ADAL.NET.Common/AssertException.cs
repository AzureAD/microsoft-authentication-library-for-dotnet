//----------------------------------------------------------------------
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Test.ADAL.Common
{
    public static class AssertException
    {
        [DebuggerStepThrough]
        public static void DoesNotThrow(Action testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new AssertFailedException("DoesNotThrow failed.", ex);
            }
        }

        [DebuggerStepThrough]
        public static void DoesNotThrow(Func<object> testCode)
        {
            var ex = Recorder.Exception<Exception>(testCode);
            if (ex != null)
            {
                throw new AssertFailedException("DoesNotThrow failed.", ex);
            }
        }

        [DebuggerStepThrough]
        public static TException Throws<TException>(Action testCode)
             where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        public static TException Throws<TException>(Func<object> testCode)
            where TException : Exception
        {
            return Throws<TException>(testCode, false);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        public static T TaskThrows<T>(Func<Task> testCode)
            where T : Exception
        {
            var exception = Recorder.Exception<AggregateException>(() => testCode().Wait());

            if (exception == null)
            {
                throw new AssertFailedException("AssertExtensions.Throws failed. No exception occurred.");
            }

            var exceptionsMatching = exception.InnerExceptions.OfType<T>().ToList();

            if (!exceptionsMatching.Any())
            {
                throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.Throws failed. Incorrect exception {0} occurred.", exception.GetType().Name), exception);
            }

            return exceptionsMatching.First();
        }

        [DebuggerStepThrough]
        public static void TaskDoesNotThrow(Func<Task> testCode)
        {
            var exception = Recorder.Exception<AggregateException>(() => testCode().Wait());

            if (exception == null)
            {
                return;
            }

            throw new AssertFailedException(string.Format(CultureInfo.CurrentCulture, "AssertExtensions.TaskDoesNotThrow failed. Incorrect exception {0} occurred.", exception.GetType().Name), exception);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
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
            [DebuggerStepThrough]
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
            }

            [DebuggerStepThrough]
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
            }
        }
    }
}

