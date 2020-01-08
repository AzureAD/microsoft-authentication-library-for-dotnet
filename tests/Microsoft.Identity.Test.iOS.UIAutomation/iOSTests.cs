// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Test.LabInfrastructure;
using NUnit.Framework;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Xamarin.UITest;
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Identity.Test.Common;

//NOTICE! Inorder to run UI automation tests for xamarin locally, you may need to upgrade nunit to 3.0 and above for this project and the core ui Automation project.
//It is set to 2.6.4 because that is the maximum version that appcenter can support.
//There is an error in visual studio that can prevent the NUnit test framework from loading the test dll properly.
//Remember to return the version back to 2.6.4 before commiting to prevent appcenter from failing

namespace Test.Microsoft.Identity.UIAutomation
{
    /// <summary>
    /// Configures environment for iOS tests to run
    /// </summary>
    [TestFixture(Platform.iOS)]
    public class IOSMsalTests
    {
        private IApp _app;
        private readonly Platform _platform;
        private readonly ITestController _xamarinController = new iOSTestController();
        MobileTestHelper _mobileTestHelper;

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public IOSMsalTests(Platform platform)
        {
            _platform = platform;
        }

        /// <summary>
        /// Initializes app and test controller before each test
        /// </summary>
        [SetUp]
        public void InitializeBeforeTest()
        {
            _app = AppFactory.StartApp(_platform, "XForms.iOS");
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
                AcquireTokenInteractiveAndSilentTest,

                AcquireTokenADFSV3InteractiveFederatedTest,
                AcquireTokenADFSV4InteractiveFederatedTest,
                AcquireTokenADFSV2019InteractiveFederatedTest,

                //B2CLocalAccountAcquireTokenTest,
                //B2CGoogleB2CLoginAuthorityAcquireTokenTest,
                //B2CGoogleMicrosoftAuthorityAcquireTokenTest,
                //B2CFacebookMicrosoftLoginAcquireTokenTest,
                //B2CFacebookB2CLoginAcquireTokenTest,
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
        /// Runs through the standard acquire token 
        /// interactive and then silent flow
        /// </summary>
        [Test]
        public void AcquireTokenInteractiveAndSilentTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.AcquireTokenTestHelper(
                _xamarinController,
                LabUserHelper.GetDefaultUserAsync().GetAwaiter().GetResult(),
                CoreUiTestConstants.AcquireTokenSilent);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("css id not getting picked up on iOS")]
        public void B2CFacebookB2CLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult(),
                CoreUiTestConstants.B2CFacebookb2clogin);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Facebook tests are unstable")] //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1351

        public void B2CFacebookMicrosoftLoginAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(
              _xamarinController,
              LabUserHelper.GetB2CFacebookAccountAsync().GetAwaiter().GetResult(),
              CoreUiTestConstants.B2CFacebookMicrosoftLogin);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// call to edit profile authority with
        ///  UIBehavior none
        /// </summary>
        [Test]
        [Ignore("css id for continue different from Android device")]
        public void B2CFacebookEditPolicyAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.PerformB2CSignInEditProfileFlow(
                _xamarinController);
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
                CoreUiTestConstants.B2CGoogleb2clogin);
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
                CoreUiTestConstants.B2CGoogleMicrosoftLogin);
        }

        /// <summary>
        /// B2C acquire token with local account
        /// b2clogin.com authority
        /// and subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Bug in Xamarin.UITest in iOS with username & password being put in the same box.")]
        public void B2CLocalAccountAcquireTokenTest()
        {
            TestCommon.ResetInternalStaticCaches();

            _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(
                _xamarinController,
                LabUserHelper.GetB2CLocalAccountAsync().GetAwaiter().GetResult(),
                CoreUiTestConstants.B2CLocalb2clogin);
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
                CoreUiTestConstants.ADFSv4Federated);
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
                CoreUiTestConstants.ADFSv2019Federated);
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
                CoreUiTestConstants.ADFSv3Federated);
        }

        private static void LogMessage(string message, StringBuilder stringBuilderMessage)
        {
            Console.WriteLine(message);
            stringBuilderMessage.AppendLine(message);
        }
    }
}
