// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.UIAutomation;
using NUnit.Framework;
using Xamarin.UITest;
using UITestConstants = Microsoft.Identity.Test.UIAutomation.UITestConstants;

namespace Microsoft.Identity.Test.UIAutomationTests
{
    /// <summary>
    /// Configures environment for mobile tests to run
    /// </summary>
    public class XamarinTests
    {
        private IApp _app;
        private readonly Platform _platform;
        private readonly ITestController _xamarinController = new AndroidTestController();
        MobileTestHelper _mobileTestHelper;

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public XamarinTests(Platform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// Initializes app and test controller before each test
        /// </summary>
        [SetUp]
        public void InitializeBeforeTest()
        {
            if (_platform == Platform.iOS)
            {
                _app = AppFactory.StartApp(_platform, "Xamarin.AutomationApp.iOS");
            }
            else
            {
                _app = AppFactory.StartApp(_platform, "com.Microsoft.XamarinAutomationApp");
            }
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
               
                // Ignored tests
                //B2CGoogleB2CLoginAuthorityAcquireTokenTest,
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
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult(),
                UITestConstants.AcquireTokenInteractive);
        }

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Test]
        public void PromptBehaviorConsentSelectAccount()
        {
            TestCommon.ResetInternalStaticCaches();

            LabResponse labResponse = LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult();

            _mobileTestHelper.AcquireTokenInteractiveWithConsentTest(
                _xamarinController, 
                labResponse,
                UITestConstants.AcquireTokenInteractiveConsentWithSelectAccount);
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        [Test]
        public void AcquireTokenSilentTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult(),
                UITestConstants.AcquireTokenSilent);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Facebook does not allow automated test accounts. " +
            "Tracking here: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookB2CLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult(),
                UITestConstants.B2CFacebookb2clogin);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Facebook does not allow automated test accounts. " +
            "Tracking here: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookMicrosoftLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(
                _xamarinController,
                LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult().User,
                B2CIdentityProvider.Facebook,
                UITestConstants.B2CFacebookMicrosoftLogin);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// call to edit profile authority with
        ///  UIBehavior none
        /// </summary>
        [Test]
        [Ignore("Facebook does not allow automated test accounts. " +
            "Tracking here: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1026")]
        public void B2CFacebookEditPolicyAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.PerformB2CSignInEditProfileFlow(
                _xamarinController,
                LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult().User,
                B2CIdentityProvider.Facebook,
                UITestConstants.B2CFacebookb2cloginEditProfile);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Google Auth does not support embedded webview from b2clogin.com authority. " +
            "App Center cannot run system browser tests yet, so this test can only be run in " +
            "system browser locally.")]
        public void B2CGoogleB2CLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CGoogleAccountAsync().GetAwaiter().GetResult(),
                UITestConstants.B2CGoogleb2clogin);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("UI is different in AppCenter compared w/local.")]
        public void B2CGoogleMicrosoftLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CGoogleAccountAsync().GetAwaiter().GetResult(),
                UITestConstants.B2CGoogleMicrosoftLogin);
        }

        /// <summary>
        /// B2C acquire token with local account
        /// b2clogin.com authority
        /// and subsequent silent call
        /// </summary>
        [Test]
        public void B2CLocalAccountAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CLocalAccountAsync().GetAwaiter().GetResult(),
                UITestConstants.B2CLocalb2clogin);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Federated flow
        /// </summary
        [Test]
        public void AcquireTokenADFSV4InteractiveFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4).GetAwaiter().GetResult(),
                UITestConstants.ADFSv4Federated);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Federated flow
        /// </summary
        [Test]
        public void AcquireTokenADFSV2019InteractiveFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).GetAwaiter().GetResult(),
                UITestConstants.ADFSv2019Federated);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV3InteractiveFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV3).GetAwaiter().GetResult(),
                UITestConstants.ADFSv3Federated);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV4InteractiveNonFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).GetAwaiter().GetResult(),
                UITestConstants.ADFSv4NonFederated);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV2019InteractiveNonFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, false).GetAwaiter().GetResult(),
                UITestConstants.ADFSv2019NonFederated);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Non-Federated flow
        /// </summary>
        [Test]
        public void AcquireTokenADFSV3InteractiveNonFederatedTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).GetAwaiter().GetResult(),
                UITestConstants.ADFSv3NonFederated);
        }

        private static void LogMessage(string message, StringBuilder stringBuilderMessage)
        {
            Console.WriteLine(message);
            stringBuilderMessage.AppendLine(message);
        }
    }
}
