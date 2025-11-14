// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory(TestCategories.BuilderTests)]
    public class ConfidentialClientApplicationExtensibilityApiTests
    {
        private X509Certificate2 _certificate;

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _certificate?.Dispose();
        }

        #region WithCertificate Tests

        [TestMethod]
        public void WithCertificate_CallbackIsStored()
        {
            // Arrange
            bool callbackInvoked = false;
            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> certificateProvider = async (parameters) =>
            {
                callbackInvoked = true;
                return GetTestCertificate();
            };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certificateProvider)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.ClientCredentialCertificateProvider);
            Assert.IsFalse(callbackInvoked, "Certificate provider callback is not yet invoked.");
        }

        [TestMethod]
        public void WithCertificate_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithCertificate((Func<ClientCredentialExtensionParameters, Task<X509Certificate2>>)null)
                    .Build());

            Assert.AreEqual("certificateProvider", ex.ParamName);
        }

        [TestMethod]
        public void WithCertificate_AllowsMultipleCallbackRegistrations_LastOneWins()
        {
            // Arrange
            int firstCallbackInvoked = 0;
            int secondCallbackInvoked = 0;

            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> firstProvider = async (parameters) =>
            {
                firstCallbackInvoked++;
                return GetTestCertificate();
            };

            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> secondProvider = async (parameters) =>
            {
                secondCallbackInvoked++;
                return GetTestCertificate();
            };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(firstProvider)
                .WithCertificate(secondProvider)
                .BuildConcrete();

            // Assert - last one should be stored
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.ClientCredentialCertificateProvider);
            Assert.AreNotSame(firstProvider, config.ClientCredentialCertificateProvider);
        }

        #endregion

        #region OnMsalServiceFailure Tests

        [TestMethod]
        public void OnMsalServiceFailure_CallbackIsStored()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, MsalException, Task<bool>> onMsalServiceFailureCallback = async (parameters, ex) => false;

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .OnMsalServiceFailure(onMsalServiceFailureCallback)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.OnMsalServiceFailureCallback);
        }

        [TestMethod]
        public void OnMsalServiceFailure_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .OnMsalServiceFailure(null)
                    .Build());

            Assert.AreEqual("onMsalServiceFailureCallback", ex.ParamName);
        }

        [TestMethod]
        public void OnMsalServiceFailure_AllowsMultipleRegistrations_LastOneWins()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, MsalException, Task<bool>> firstPolicy = async (parameters, ex) => true;
            Func<ClientCredentialExtensionParameters, MsalException, Task<bool>> secondPolicy = async (parameters, ex) => false;

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .OnMsalServiceFailure(firstPolicy)
                .OnMsalServiceFailure(secondPolicy)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config, "AppConfig should be of type ApplicationConfiguration.");
            Assert.IsNotNull(config.OnMsalServiceFailureCallback);
            Assert.AreSame(secondPolicy, config.OnMsalServiceFailureCallback);
        }

        #endregion

        #region OnSuccess Tests

        [TestMethod]
        public void OnSuccess_CallbackIsStored()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, ExecutionResult, Task> onSuccessCallback = async (parameters, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .OnSuccess(onSuccessCallback)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.OnSuccessCallback);
        }

        [TestMethod]
        public void OnSuccess_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .OnSuccess(null)
                    .Build());

            Assert.AreEqual("onSuccessCallback", ex.ParamName);
        }

        [TestMethod]
        public void OnSuccess_AllowsMultipleRegistrations_LastOneWins()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, ExecutionResult, Task> firstObserver = async (parameters, result) => { };
            Func<ClientCredentialExtensionParameters, ExecutionResult, Task> secondObserver = async (parameters, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .OnSuccess(firstObserver)
                .OnSuccess(secondObserver)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config, "AppConfig is not of type ApplicationConfiguration.");
            Assert.IsNotNull(config.OnSuccessCallback);
            Assert.AreSame(secondObserver, config.OnSuccessCallback);
        }

        #endregion

        #region ExecutionResult Tests

        [TestMethod]
        public void ExecutionResult_CanBeCreated()
        {
            // Act
            var result = new ExecutionResult();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Successful);
            Assert.IsNull(result.Result);
            Assert.IsNull(result.Exception);
        }

        [TestMethod]
        public void ExecutionResult_PropertiesCanBeSet()
        {
            // Arrange
            var authResult = new AuthenticationResult(
                accessToken: "token",
                isExtendedLifeTimeToken: false,
                uniqueId: "unique_id",
                expiresOn: DateTimeOffset.UtcNow.AddHours(1),
                extendedExpiresOn: DateTimeOffset.UtcNow.AddHours(2),
                tenantId: TestConstants.TenantId,
                account: null,
                idToken: "id_token",
                scopes: new[] { "scope1" },
                correlationId: Guid.NewGuid(),
                tokenType: "Bearer",
                authenticationResultMetadata: null);

            var msalException = new MsalServiceException("error_code", "error_message");

            // Act - Success case
            var successResult = new ExecutionResult
            {
                Successful = true,
                Result = authResult,
                Exception = null
            };

            // Assert
            Assert.IsTrue(successResult.Successful);
            Assert.AreSame(authResult, successResult.Result);
            Assert.IsNull(successResult.Exception);

            // Act - Failure case
            var failureResult = new ExecutionResult
            {
                Successful = false,
                Result = null,
                Exception = msalException
            };

            // Assert
            Assert.IsFalse(failureResult.Successful);
            Assert.IsNull(failureResult.Result);
            Assert.AreSame(msalException, failureResult.Exception);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void AllThreeExtensibilityPoints_CanBeConfiguredTogether()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> certificateProvider = async (parameters) => GetTestCertificate();
            Func<ClientCredentialExtensionParameters, MsalException, Task<bool>> onMsalServiceFailure = async (parameters, ex) => false;
            Func<ClientCredentialExtensionParameters, ExecutionResult, Task> onSuccess = async (parameters, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certificateProvider)
                .OnMsalServiceFailure(onMsalServiceFailure)
                .OnSuccess(onSuccess)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config.OnMsalServiceFailureCallback);
            Assert.IsNotNull(config.OnSuccessCallback);
        }

        [TestMethod]
        public void ExtensibilityPoints_CanBeConfiguredInAnyOrder()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> certificateProvider = async (parameters) => GetTestCertificate();
            Func<ClientCredentialExtensionParameters, MsalException, Task<bool>> onMsalServiceFailure = async (parameters, ex) => false;
            Func<ClientCredentialExtensionParameters, ExecutionResult, Task> onSuccess = async (parameters, result) => { };

            // Act - Order: OnSuccess, OnMsalServiceFailure, Certificate
            var app1 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .OnSuccess(onSuccess)
                .OnMsalServiceFailure(onMsalServiceFailure)
                .WithCertificate(certificateProvider)
                .BuildConcrete();

            // Act - Order: OnMsalServiceFailure, Certificate, OnSuccess
            var app2 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .OnMsalServiceFailure(onMsalServiceFailure)
                .WithCertificate(certificateProvider)
                .OnSuccess(onSuccess)
                .BuildConcrete();

            // Assert
            var config1 = app1.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config1);
            Assert.IsNotNull(config1.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config1.OnMsalServiceFailureCallback);
            Assert.IsNotNull(config1.OnSuccessCallback);

            var config2 = app2.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config2, "app2.AppConfig should be of type ApplicationConfiguration");
            Assert.IsNotNull(config2.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config2.OnMsalServiceFailureCallback);
            Assert.IsNotNull(config2.OnSuccessCallback);
        }

        [TestMethod]
        public void WithCertificate_WorksWithOtherConfidentialClientOptions()
        {
            // Arrange
            Func<ClientCredentialExtensionParameters, Task<X509Certificate2>> certificateProvider = async (parameters) =>
            {
                Assert.AreEqual(TestConstants.ClientId, parameters.ClientId);
                Assert.AreEqual(TestConstants.AadTenantId, parameters.TenantId);
                return GetTestCertificate();
            };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AadAuthorityWithTestTenantId)
                .WithCertificate(certificateProvider)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull(app);

            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.ClientCredentialCertificateProvider);
        }

        #endregion

        #region Helper Methods

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi",
        Justification = "Test code only")]
        private X509Certificate2 GetTestCertificate()
        {
            if (_certificate == null)
            {
                _certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"),
                    TestConstants.TestCertPassword);
            }
            return _certificate;
        }

        #endregion
    }
}
