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

using System;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class MsalExceptionSerializationTests
    {
        private const string SomeErrorCode = "some_error_code";
        private const string SomeErrorMessage = "Some error message.";

        private const string SomeClaims = "here are some claims";
        private const string SomeCorrelationId = "the correlation id";
        private const string SomeSubError = "the_sub_error";
        private const string SomeResponseBody = "the response body";

        private void SerializeDeserializeAndValidate(MsalException ex, Type expectedType, bool isServiceExceptionDerived)
        {
            string json = ex.ToJsonString();

            var exDeserialized = MsalException.FromJsonString(json);

            Assert.AreEqual(expectedType, exDeserialized.GetType());
            Assert.AreEqual(SomeErrorCode, ex.ErrorCode);
            Assert.AreEqual(SomeErrorMessage, ex.Message);

            if (isServiceExceptionDerived)
            {
                var svcEx = (MsalServiceException)exDeserialized;

                Assert.AreEqual(SomeClaims, svcEx.Claims);
                Assert.AreEqual(SomeResponseBody, svcEx.ResponseBody);
                Assert.AreEqual(SomeSubError, svcEx.SubError);
                Assert.AreEqual(SomeCorrelationId, svcEx.CorrelationId);
            }
        }

        [TestMethod]
        public void MsalExceptionCanSerializeAndDeserializeRoundTrip()
        {
            var ex = new MsalException(SomeErrorCode, SomeErrorMessage);
            SerializeDeserializeAndValidate(ex, typeof(MsalException), false);
        }

        [TestMethod]
        public void MsalServiceExceptionCanSerializeAndDeserializeRoundTrip()
        {
            var ex = new MsalServiceException(SomeErrorCode, SomeErrorMessage)
            {
                Claims = SomeClaims,
                CorrelationId = SomeCorrelationId,
                SubError = SomeSubError,
                ResponseBody = SomeResponseBody
            };

            SerializeDeserializeAndValidate(ex, typeof(MsalServiceException), true);
        }

        [TestMethod]
        public void MsalClientExceptionCanSerializeAndDeserializeRoundTrip()
        {
            var ex = new MsalClientException(SomeErrorCode, SomeErrorMessage);
            SerializeDeserializeAndValidate(ex, typeof(MsalClientException), false);
        }

        [TestMethod]
        public void MsalUiRequiredExceptionCanSerializeAndDeserializeRoundTrip()
        {
            var ex = new MsalUiRequiredException(SomeErrorCode, SomeErrorMessage)
            {
                Claims = SomeClaims,
                CorrelationId = SomeCorrelationId,
                SubError = SomeSubError,
                ResponseBody = SomeResponseBody
            };

            SerializeDeserializeAndValidate(ex, typeof(MsalUiRequiredException), true);
        }
    }
}
