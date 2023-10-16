// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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

        private const string BrokerErrorContext = "broker error context";
        private const string BrokerErrorTag = "0x123456";
        private const string BrokerErrorStatus = "broker error status";
        private const string BrokerErrorCode = "broker error code";
        private const string BrokerTelemetry = "{\"error-property1\": \"0\",\"error-property2\": \"abc\"}";

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MsalException_CanSerializeAndDeserializeRoundTrip(bool includeAdditionalExceptionData)
        {
            var ex = new MsalException(SomeErrorCode, SomeErrorMessage);

            if (includeAdditionalExceptionData)
            {
                ex.AdditionalExceptionData = new Dictionary<string, string>()
                {
                    { MsalException.BrokerErrorContext, BrokerErrorContext },
                    { MsalException.BrokerErrorTag, BrokerErrorTag },
                    { MsalException.BrokerErrorStatus, BrokerErrorStatus },
                    { MsalException.BrokerErrorCode, BrokerErrorCode },
                    { MsalException.BrokerTelemetry, BrokerTelemetry },
                };
            }

            SerializeDeserializeAndValidate(ex, typeof(MsalException), false, includeAdditionalExceptionData);
        }

        [TestMethod]
        public void MsalClientException_CanSerializeAndDeserializeRoundTrip()
        {
            var ex = new MsalClientException(SomeErrorCode, SomeErrorMessage);
            SerializeDeserializeAndValidate(ex, typeof(MsalClientException), false);
        }

        [TestMethod]
        public void MsalServiceException_CanSerializeAndDeserializeRoundTrip()
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
        public void MsalUiRequiredException_CanSerializeAndDeserializeRoundTrip()
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

        private void SerializeDeserializeAndValidate(MsalException ex, Type expectedType, bool isServiceExceptionDerived, bool includeAdditionalExceptionData = false)
        {
            string json = ex.ToJsonString();

            var exDeserialized = MsalException.FromJsonString(json);

            Assert.AreEqual(expectedType, exDeserialized.GetType());
            Assert.AreEqual(ex.ErrorCode, exDeserialized.ErrorCode);
            Assert.AreEqual(ex.Message, exDeserialized.Message);

            if (includeAdditionalExceptionData)
            {
                Assert.AreEqual(ex.AdditionalExceptionData[MsalException.BrokerErrorContext], exDeserialized.AdditionalExceptionData[MsalException.BrokerErrorContext]);
                Assert.AreEqual(ex.AdditionalExceptionData[MsalException.BrokerErrorTag], exDeserialized.AdditionalExceptionData[MsalException.BrokerErrorTag]);
                Assert.AreEqual(ex.AdditionalExceptionData[MsalException.BrokerErrorStatus], exDeserialized.AdditionalExceptionData[MsalException.BrokerErrorStatus]);
                Assert.AreEqual(ex.AdditionalExceptionData[MsalException.BrokerErrorCode], exDeserialized.AdditionalExceptionData[MsalException.BrokerErrorCode]);
                Assert.AreEqual(ex.AdditionalExceptionData[MsalException.BrokerTelemetry], exDeserialized.AdditionalExceptionData[MsalException.BrokerTelemetry]);
            }

            if (isServiceExceptionDerived)
            {
                var serviceEx = (MsalServiceException)exDeserialized;

                Assert.AreEqual(SomeClaims, serviceEx.Claims);
                Assert.AreEqual(SomeResponseBody, serviceEx.ResponseBody);
                Assert.AreEqual(SomeCorrelationId, serviceEx.CorrelationId);
                Assert.AreEqual(SomeSubError, serviceEx.SubError);
            }
        }
    }
}
