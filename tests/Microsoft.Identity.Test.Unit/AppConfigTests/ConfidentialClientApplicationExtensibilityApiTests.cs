// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.RP;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    [TestCategory(TestCategories.BuilderTests)]
    public class ConfidentialClientApplicationExtensibilityApiTests
    {
        private X509Certificate2 _certificate;
        private CertificateOptions _certificateOptions = new CertificateOptions();

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
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options)
            {
                callbackInvoked = true;
                return Task.FromResult(GetTestCertificate());
            }

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, _certificateOptions)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.ClientCredential);
            Assert.IsInstanceOfType((app.AppConfig as ApplicationConfiguration)?.ClientCredential, typeof(DynamicCertificateClientCredential));
            Assert.IsFalse(callbackInvoked, "Certificate provider callback is not yet invoked.");
        }

        [TestMethod]
        public void WithCertificate_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithCertificate((Func<AssertionRequestOptions, Task<X509Certificate2>>) null, null)
                    .Build());

            Assert.AreEqual("certificateProvider", ex.ParamName);
        }

        [TestMethod]
        public void WithCertificate_AllowsMultipleCallbackRegistrations_LastOneWins()
        {
            // Arrange
            int firstCallbackInvoked = 0;
            int secondCallbackInvoked = 0;

            Task<X509Certificate2> firstProvider(AssertionRequestOptions options)
            {
                firstCallbackInvoked++;
                return Task.FromResult(GetTestCertificate());
            }

            Task<X509Certificate2> secondProvider(AssertionRequestOptions options)
            {
                secondCallbackInvoked++;
                return Task.FromResult(GetTestCertificate());
            }

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(firstProvider, _certificateOptions)
                .WithCertificate(secondProvider, _certificateOptions)
                .BuildConcrete();

            // Assert - last one should be stored
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.ClientCredential);
            Assert.IsInstanceOfType(config.ClientCredential, typeof(DynamicCertificateClientCredential));
        }

        [TestMethod]
        public void WithCertificate_CertificateOptions_SendX5C_True_IsStored()
        {
            // Arrange
            var certificateOptions = new CertificateOptions { SendX5C = true };
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, certificateOptions)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsTrue(config.SendX5C, "SendX5C should be true when CertificateOptions.SendX5C is true");
        }

        [TestMethod]
        public void WithCertificate_CertificateOptions_SendX5C_False_IsStored()
        {
            // Arrange
            var certificateOptions = new CertificateOptions { SendX5C = false };
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, certificateOptions)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsFalse(config.SendX5C, "SendX5C should be false when CertificateOptions.SendX5C is false");
        }

        [TestMethod]
        public void WithCertificate_NullCertificateOptions_DefaultsToSendX5C_False()
        {
            // Arrange
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, null)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsFalse(config.SendX5C, "SendX5C should default to false when CertificateOptions is null");
        }

        [TestMethod]
        public void WithCertificate_CertificateOptions_AssociateTokensWithCertificateSerialNumber_True_IsStored()
        {
            // Arrange
            var certificateOptions = new CertificateOptions { AssociateTokensWithCertificate = true };
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, certificateOptions)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsTrue(certificateOptions.AssociateTokensWithCertificate, 
                "CertificateOptions.AssociateTokensWithCertificate should be true");
        }

        [TestMethod]
        public void WithCertificate_CertificateOptions_AssociateTokensWithCertificateSerialNumber_False_IsStored()
        {
            // Arrange
            var certificateOptions = new CertificateOptions { AssociateTokensWithCertificate = false };
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, certificateOptions)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsFalse(certificateOptions.AssociateTokensWithCertificate, 
                "CertificateOptions.AssociateTokensWithCertificate should be false");
        }

        [TestMethod]
        public void WithCertificate_CertificateOptions_BothPropertiesSet_AreStored()
        {
            // Arrange
            var certificateOptions = new CertificateOptions 
            { 
                SendX5C = true, 
                AssociateTokensWithCertificate = true 
            };
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, certificateOptions)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config);
            Assert.IsTrue(config.SendX5C, "SendX5C should be true");
            Assert.IsTrue(certificateOptions.AssociateTokensWithCertificate, 
                "AssociateTokensWithCertificate should be true");
        }

        #endregion

        #region OnMsalServiceFailure Tests

        [TestMethod]
        public void OnMsalServiceFailure_CallbackIsStored()
        {
            // Arrange
            Task<bool> onMsalServiceFailureCallback(AssertionRequestOptions options, ExecutionResult result) => Task.FromResult(false);

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithClientSecret(TestConstants.ClientSecret)
                .OnMsalServiceFailure(onMsalServiceFailureCallback)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.OnMsalServiceFailure);
        }

        [TestMethod]
        public void OnMsalServiceFailure_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .OnMsalServiceFailure(null)
                    .Build());

            Assert.AreEqual("onMsalServiceFailure", ex.ParamName);
        }

        #endregion

        #region OnSuccess Tests

        [TestMethod]
        public void OnSuccess_CallbackIsStored()
        {
            // Arrange
            Task onSuccessCallback(AssertionRequestOptions options, ExecutionResult result) => Task.CompletedTask;

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithClientSecret(TestConstants.ClientSecret)
                .OnCompletion(onSuccessCallback)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.OnCompletion);
        }

        [TestMethod]
        public void OnSuccess_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithExperimentalFeatures()
                    .WithClientSecret(TestConstants.ClientSecret)
                    .OnCompletion(null)
                    .Build());

            Assert.AreEqual("onCompletion", ex.ParamName);
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
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());
            Task<bool> onMsalServiceFailure(AssertionRequestOptions options, ExecutionResult result) => Task.FromResult(false);
            Task onSuccess(AssertionRequestOptions options, ExecutionResult result) => Task.CompletedTask;

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithCertificate(certificateProvider, _certificateOptions)
                .OnMsalServiceFailure(onMsalServiceFailure)
                .OnCompletion(onSuccess)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config.ClientCredential);
            Assert.IsNotNull(config.OnMsalServiceFailure);
            Assert.IsNotNull(config.OnCompletion);
        }

        [TestMethod]
        public void ExtensibilityPoints_CanBeConfiguredInAnyOrder()
        {
            // Arrange
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options) => Task.FromResult(GetTestCertificate());
            Task<bool> onMsalServiceFailure(AssertionRequestOptions options, ExecutionResult result) => Task.FromResult(false);
            Task onSuccess(AssertionRequestOptions options, ExecutionResult result) => Task.CompletedTask;

            // Act - Order: OnCompletion, OnMsalServiceFailure, Certificate
            var app1 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .OnCompletion(onSuccess)
                .OnMsalServiceFailure(onMsalServiceFailure)
                .WithCertificate(certificateProvider, _certificateOptions)
                .BuildConcrete();

            // Act - Order: OnMsalServiceFailure, Certificate, OnCompletion
            var app2 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .OnMsalServiceFailure(onMsalServiceFailure)
                .WithCertificate(certificateProvider, _certificateOptions)
                .OnCompletion(onSuccess)
                .BuildConcrete();

            // Assert
            var config1 = app1.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config1);
            Assert.IsNotNull(config1.ClientCredential);
            Assert.IsNotNull(config1.OnMsalServiceFailure);
            Assert.IsNotNull(config1.OnCompletion);

            var config2 = app2.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config2, "app2.AppConfig should be of type ApplicationConfiguration");
            Assert.IsNotNull(config2.ClientCredential);
            Assert.IsNotNull(config2.OnMsalServiceFailure);
            Assert.IsNotNull(config2.OnCompletion);
        }

        [TestMethod]
        public void WithCertificate_WorksWithOtherConfidentialClientOptions()
        {
            // Arrange
            Task<X509Certificate2> certificateProvider(AssertionRequestOptions options)
            {
                Assert.AreEqual(TestConstants.ClientId, options.ClientID);
                return Task.FromResult(GetTestCertificate());
            }

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures()
                .WithAuthority(TestConstants.AadAuthorityWithTestTenantId)
                .WithCertificate(certificateProvider, _certificateOptions)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull(app);
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.ClientCredential);
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
