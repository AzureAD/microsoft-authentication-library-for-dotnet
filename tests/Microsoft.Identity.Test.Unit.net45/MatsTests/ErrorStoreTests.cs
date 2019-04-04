// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.Mats.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.MatsTests
{
    [TestClass]
    public class ErrorStoreTests : AbstractMatsTest
    {
        [TestInitialize]
        public override void Setup() => base.Setup();

        [TestCleanup]
        public override void TearDown() => base.TearDown();

        [TestMethod]
        public void CreateSucceeds()
        {
            Assert.IsTrue(_errorStore != null);
            Assert.IsTrue(CheckError(new List<PropertyBagContents>()));
        }

        [TestMethod]
        public void ReportErrorAddsNewErrorToPropertyBag()
        {
            string anyErrorMessage = "Test message";
            ErrorType anyErrorType = ErrorType.Action;
            ErrorSeverity anyErrorSeverity = ErrorSeverity.Warning;

            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);

            var expectedErrors = new List<PropertyBagContents> 
            {
                CreateErrorPropertyBagContents(anyErrorType, anyErrorMessage, anyErrorSeverity)
            };

            Assert.IsTrue(CheckError(expectedErrors));
        }

        [TestMethod]
        public void GetEventsForUploadDeletesStoredErrors()
        {
            string anyErrorMessage = "Test message";
            ErrorType anyErrorType = ErrorType.Action;
            ErrorSeverity anyErrorSeverity = ErrorSeverity.Warning;

            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);

            var dummy = _errorStore.GetEventsForUpload();

            Assert.IsTrue(CheckError(new List<PropertyBagContents>()));
        }

        [TestMethod]
        public void ErrorStoreOnlyLogsAnErrorOnceIfGetEventsForUploadIsNotCalled()
        {
            string anyErrorMessage = "Test message";
            string anotherErrorMessage = "Test message 2";
            ErrorType anyErrorType = ErrorType.Action;
            ErrorSeverity anyErrorSeverity = ErrorSeverity.Warning;

            // Log the same error multiple times
            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);
            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);
            _errorStore.ReportError(anotherErrorMessage, anyErrorType, anyErrorSeverity);

            var errors = _errorStore.GetEventsForUpload().ToList();

            Assert.AreEqual(2, errors.Count());
        }

        [TestMethod]
        public void ErrorStoreLogsAnErrorAgainAfterGetEventsForUploadHasBeenCalled()
        {
            string anyErrorMessage = "Test message";
            ErrorType anyErrorType = ErrorType.Action;
            ErrorSeverity anyErrorSeverity = ErrorSeverity.Warning;

            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);
            var errors = _errorStore.GetEventsForUpload().ToList();
            Assert.AreEqual(1, errors.Count());

            _errorStore.ReportError(anyErrorMessage, anyErrorType, anyErrorSeverity);
            errors = _errorStore.GetEventsForUpload().ToList();
            Assert.AreEqual(1, errors.Count());
        }
    }
}
