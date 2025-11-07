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
    public class ConfidentialClientApplicationExtensibilityTests
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
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) =>
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
            .WithCertificate((Func<IAppConfig, X509Certificate2>)null)
            .Build());

            Assert.AreEqual("certificateProvider", ex.ParamName);
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        public void WithCertificate_ThrowsWhenBothStaticAndDynamicCertificateConfigured()
        {
            // Arrange
            var staticCert = GetTestCertificate();
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) => GetTestCertificate();

            // Act & Assert
            var ex = Assert.ThrowsException<MsalClientException>(() =>
                ConfidentialClientApplicationBuilder
                  .Create(TestConstants.ClientId)
                     .WithCertificate(staticCert)
                      .WithCertificate(certificateProvider)
              .Build());

            Assert.AreEqual(MsalError.InvalidClientCredentialConfiguration, ex.ErrorCode);
            Assert.IsTrue(ex.Message.Contains("Choose one approach"));
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        public void WithCertificate_ThrowsWhenDynamicAndThenStaticCertificateConfigured()
        {
            // Arrange
            var staticCert = GetTestCertificate();
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) => GetTestCertificate();

            // Act & Assert
            var ex = Assert.ThrowsException<MsalClientException>(() =>
                ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certificateProvider)
                .WithCertificate(staticCert)
                .Build());

            Assert.AreEqual(MsalError.InvalidClientCredentialConfiguration, ex.ErrorCode);
        }

        [TestMethod]
        public void WithCertificate_AllowsMultipleCallbackRegistrations_LastOneWins()
        {
            // Arrange
            int firstCallbackInvoked = 0;
            int secondCallbackInvoked = 0;

            Func<IAppConfig, X509Certificate2> firstProvider = (config) =>
            {
                firstCallbackInvoked++;
                return GetTestCertificate();
            };

            Func<IAppConfig, X509Certificate2> secondProvider = (config) =>
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
            Assert.IsNotNull(config.ClientCredentialCertificateProvider);
            Assert.AreNotSame(firstProvider, config.ClientCredentialCertificateProvider);
        }

        #endregion

        #region WithRetry Tests

        [TestMethod]
        public void WithRetry_CallbackIsStored()
        {
            // Arrange
            Func<IAppConfig, MsalException, bool> retryPolicy = (config, ex) => false;

            // Act
            var app = ConfidentialClientApplicationBuilder
                 .Create(TestConstants.ClientId)
                 .WithClientSecret(TestConstants.ClientSecret)
                       .WithRetry(retryPolicy)
                 .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.RetryPolicy);
        }

        [TestMethod]
        public void WithRetry_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
              ConfidentialClientApplicationBuilder
                 .Create(TestConstants.ClientId)
            .WithClientSecret(TestConstants.ClientSecret)
                  .WithRetry(null)
                  .Build());

            Assert.AreEqual("retryPolicy", ex.ParamName);
        }

        [TestMethod]
        public void WithRetry_AllowsMultipleRegistrations_LastOneWins()
        {
            // Arrange
            Func<IAppConfig, MsalException, bool> firstPolicy = (config, ex) => true;
            Func<IAppConfig, MsalException, bool> secondPolicy = (config, ex) => false;

            // Act
            var app = ConfidentialClientApplicationBuilder
       .Create(TestConstants.ClientId)
                 .WithClientSecret(TestConstants.ClientSecret)
                 .WithRetry(firstPolicy)
                    .WithRetry(secondPolicy)
                    .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config.RetryPolicy);
            Assert.AreSame(secondPolicy, config.RetryPolicy);
        }

        #endregion

        #region WithObserver Tests

        [TestMethod]
        public void WithObserver_CallbackIsStored()
        {
            // Arrange
            Action<IAppConfig, ExecutionResult> observer = (config, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                   .Create(TestConstants.ClientId)
               .WithClientSecret(TestConstants.ClientSecret)
                    .WithObserver(observer)
              .BuildConcrete();

            // Assert
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration)?.ExecutionObserver);
        }

        [TestMethod]
        public void WithObserver_ThrowsOnNullCallback()
        {
            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() =>
                ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithObserver(null)
                    .Build());

            Assert.AreEqual("observer", ex.ParamName);
        }

        [TestMethod]
        public void WithObserver_AllowsMultipleRegistrations_LastOneWins()
        {
            // Arrange
            Action<IAppConfig, ExecutionResult> firstObserver = (config, result) => { };
            Action<IAppConfig, ExecutionResult> secondObserver = (config, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithObserver(firstObserver)
                .WithObserver(secondObserver)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config.ExecutionObserver);
            Assert.AreSame(secondObserver, config.ExecutionObserver);
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
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) => GetTestCertificate();
            Func<IAppConfig, MsalException, bool> retryPolicy = (config, ex) => false;
            Action<IAppConfig, ExecutionResult> observer = (config, result) => { };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithCertificate(certificateProvider)
                .WithRetry(retryPolicy)
                .WithObserver(observer)
                .BuildConcrete();

            // Assert
            var config = app.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config.RetryPolicy);
            Assert.IsNotNull(config.ExecutionObserver);
        }

        [TestMethod]
        public void ExtensibilityPoints_CanBeConfiguredInAnyOrder()
        {
            // Arrange
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) => GetTestCertificate();
            Func<IAppConfig, MsalException, bool> retryPolicy = (config, ex) => false;
            Action<IAppConfig, ExecutionResult> observer = (config, result) => { };

            // Act - Order: Observer, Retry, Certificate
            var app1 = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithObserver(observer)
                .WithRetry(retryPolicy)
                .WithCertificate(certificateProvider)
                .BuildConcrete();

            // Act - Order: Retry, Certificate, Observer
            var app2 = ConfidentialClientApplicationBuilder
            .Create(TestConstants.ClientId)
       .WithRetry(retryPolicy)
            .WithCertificate(certificateProvider)
          .WithObserver(observer)
        .BuildConcrete();

            // Assert
            var config1 = app1.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config1.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config1.RetryPolicy);
            Assert.IsNotNull(config1.ExecutionObserver);

            var config2 = app2.AppConfig as ApplicationConfiguration;
            Assert.IsNotNull(config2.ClientCredentialCertificateProvider);
            Assert.IsNotNull(config2.RetryPolicy);
            Assert.IsNotNull(config2.ExecutionObserver);
        }

        [TestMethod]
        public void WithCertificate_WorksWithOtherConfidentialClientOptions()
        {
            // Arrange
            Func<IAppConfig, X509Certificate2> certificateProvider = (config) =>
              {
                  Assert.AreEqual(TestConstants.ClientId, config.ClientId);
                  Assert.AreEqual(TestConstants.TenantId, config.TenantId);
                  return GetTestCertificate();
              };

            // Act
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .WithRedirectUri("https://localhost")
                .WithClientName("TestApp")
                .WithClientVersion("1.0.0")
                .WithCertificate(certificateProvider)
                .BuildConcrete();

            // Assert
            Assert.IsNotNull(app);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(TestConstants.TenantId, app.AppConfig.TenantId);
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
