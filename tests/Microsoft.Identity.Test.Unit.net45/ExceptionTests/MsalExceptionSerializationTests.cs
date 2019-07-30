// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        private const string SomeResponseBody = "the response body";
        private const string SomeSubError = "the_sub_error";
        
        private void SerializeDeserializeAndValidate(MsalException ex, Type expectedType, bool isServiceExceptionDerived)
        {
            string json = ex.ToJsonString();

            var exDeserialized = MsalException.FromJsonString(json);

            Assert.AreEqual(expectedType, exDeserialized.GetType());
            Assert.AreEqual(ex.ErrorCode, exDeserialized.ErrorCode);
            Assert.AreEqual(ex.Message, exDeserialized.Message);

            if (isServiceExceptionDerived)
            {
                var svcEx = (MsalServiceException)exDeserialized;

                Assert.AreEqual(SomeClaims, svcEx.Claims);
                Assert.AreEqual(SomeResponseBody, svcEx.ResponseBody);
                Assert.AreEqual(SomeCorrelationId, svcEx.CorrelationId);
                Assert.AreEqual(SomeSubError, svcEx.SubError);
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
                ResponseBody = SomeResponseBody,
                SubError = SomeSubError
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
                ResponseBody = SomeResponseBody,
                SubError = SomeSubError
            };

            SerializeDeserializeAndValidate(ex, typeof(MsalUiRequiredException), true);
        }
    }
}
