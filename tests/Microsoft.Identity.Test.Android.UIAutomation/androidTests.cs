// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xamarin.UITest;

//NOTICE! Inorder to run UI automation tests for xamarin locally, you may need to upgrade nunit to 3.0 and above for this project and the core ui Automation project.
//It is set to 2.6.4 because that is the maximum version that appcenter can support.
//There is an error in visual studio that can prevent the NUnit test framework from loading the test dll properly.
//Remember to return the version back to 2.6.4 before commiting to prevent appcenter from failing

namespace Microsoft.Identity.Test.UIAutomation
{
    /// <summary>
    /// Configures environment for core/Android tests to run
    /// </summary>
    [TestFixture(Platform.Android)]
    public class AndroidTests
    {
        private IApp _app;
        private readonly Platform _platform;
        private readonly ITestController _xamarinController = new AndroidXamarinUiTestController();
        MobileTestHelper _mobileTestHelper;

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public AndroidTests(Platform platform)
        {
            this._platform = platform;
        }

        /// <summary>
        /// Initializes app and test controller before each test
        /// </summary>
        [SetUp]
        public void InitializeBeforeTest()
        {
            _app = AppFactory.StartApp(_platform, "com.Microsoft.XFormsDroid.MSAL");
            _xamarinController.Application = _app;
            _mobileTestHelper = new MobileTestHelper(_platform);
        }

        /// <summary>
        /// Test runner to run all tests, as test initialization is expensive.
        /// </summary>
        [Test]
        [Category("FastRun")]
        public void RunAllTests()
        {
            var tests = new List<Action>()
            {
                AcquireTokenTest,
                AcquireTokenSilentTest,

                PromptBehaviorConsentSelectAccount,

                AcquireTokenADFSV3InteractiveFederatedTest,
                AcquireTokenADFSV3InteractiveNonFederatedTest,
                AcquireTokenADFSV4InteractiveFederatedTest,
                AcquireTokenADFSV4InteractiveNonFederatedTest,
                AcquireTokenADFSV2019InteractiveFederatedTest,
                AcquireTokenADFSV2019InteractiveNonFederatedTest,

                B2CLocalAccountAcquireTokenTest,
                B2CROPCLocalAccountAcquireTokenTest
                // Google Auth does not support embedded webview from b2clogin.com authority.
                // App Center cannot run system browser tests yet, so this test can only be run in system browser locally.
                //B2CGoogleB2CLoginAuthorityAcquireTokenTest,

                // Ignored tests
                //B2CGoogleMicrosoftAuthorityAcquireTokenTest,
                //B2CFacebookMicrosoftAuthorityAcquireTokenTest,
                //B2CFacebookB2CLoginAuthorityAcquireTokenTest,
                //B2CFacebookEditPolicyAcquireTokenTest
            };

            var hasFailed = false;
            var stringBuilderMessage = new StringBuilder();

            foreach (Action test in tests)
            {
                try
                {
                    LogMessage($"Running test: {test.Method.Name}", stringBuilderMessage);
                    test();
                }
                catch (Exception ex)
                {
                    LogMessage($"Fail: {test.Method.Name}, Error: {ex.Message}", stringBuilderMessage);
                    hasFailed = true;
                }
                finally
                {
                    LogMessage($"Complete test: {test.Method.Name}", stringBuilderMessage);
                }
            }

            Assert.IsFalse(hasFailed, $"Test Failed. {stringBuilderMessage}");
        }


        /// <summary>
        /// Runs through the standard acquire token flow, using the default app configured UiBehavior = Login
        /// </summary>
        [Test]
        public void AcquireTokenTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Test]
        public void PromptBehaviorConsentSelectAccount()
        {
            var labResponse = LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult();

            _mobileTestHelper.PromptBehaviorTestHelperWithConsent(_xamarinController, labResponse);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        [Test]
        public void AcquireTokenSilentTest()
        {
            _mobileTestHelper.AcquireTokenSilentTestHelper(_xamarinController, LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult());
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("issue: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookB2CLoginAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(_xamarinController, LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult(), true);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("issue: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookMicrosoftAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(_xamarinController, LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult().User, B2CIdentityProvider.Facebook, false);
            _mobileTestHelper.B2CSilentFlowHelper(_xamarinController);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// call to edit profile authority with
        ///  UIBehavior none
        /// </summary>
        [Test]
        [Ignore("issue: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookEditPolicyAcquireTokenTest()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(_xamarinController, LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult().User, B2CIdentityProvider.Facebook, true);
            _mobileTestHelper.B2CSilentFlowHelper(_xamarinController);
            _mobileTestHelper.B2CFacebookEditPolicyAcquireTokenInteractiveTestHelper(_xamarinController);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("Google Auth does not support embedded webview from b2clogin.com authority. " +
            "App Center cannot run system browser tests yet, so this test can only be run in " +
            "system browser locally.")]
        [Test]
        public void B2CGoogleB2CLoginAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(_xamarinController, LabUserHelper.GetB2CGoogleAccountAsync().GetAwaiter().GetResult(), true);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Ignore("UI is different in AppCenter compared w/local.")]
        [Test]
        public void B2CGoogleMicrosoftAuthorityAcquireTokenTest()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(_xamarinController, LabUserHelper.GetB2CGoogleAccountAsync().GetAwaiter().GetResult(), false);
        }

        /// <summary>
        /// B2C acquire token with local account
        /// b2clogin.com authority
        /// and subsequent silent call
        /// </summary>
        [Test]
        public void B2CLocalAccountAcquireTokenTest()
        {
            _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(_xamarinController, LabUserHelper.GetB2CLocalAccountAsync().GetAwaiter().GetResult(), true);
        }

        /// <summary>
        /// B2C ROPC acquire token with local account
        /// b2clogin.com authority
        /// </summary>
        [Test]
        public void B2CROPCLocalAccountAcquireTokenTest()
        {
            _mobileTestHelper.B2CAcquireTokenROPCTest(_xamarinController, LabUserHelper.GetB2CLocalAccountAsync().GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Federated flow
        /// </summary
        [Test]
        public void AcquireTokenADFSV4InteractiveFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Federated flow
        /// </summary
        [Test]
        public void AcquireTokenADFSV2019InteractiveFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV3InteractiveFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV3).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV4InteractiveNonFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV2019InteractiveNonFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, false).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV3InteractiveNonFederatedTest()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).GetAwaiter().GetResult());
        }

        private static void LogMessage(string message, StringBuilder stringBuilderMessage)
        {
            Console.WriteLine(message);
            stringBuilderMessage.AppendLine(message);
        }
    }
}
