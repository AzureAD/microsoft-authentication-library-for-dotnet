// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Test.LabInfrastructure;
using NUnit.Framework;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Xamarin.UITest;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

//NOTICE! Inorder to run UI automation tests for xamarin locally, you may need to upgrade nunit to 3.0 and above for this project and the core ui Automation project.
//It is set to 2.6.4 because that is the maximum version that appcenter can support.
//There is an error in visual studio that can prevent the NUnit test framework from loading the test dll properly.
//Remember to return the version back to 2.6.4 before commiting to prevent appcenter from failing

namespace Test.Microsoft.Identity.UIAutomation
{
    /// <summary>
    /// Configures environment for core/iOS tests to run
    /// </summary>
    [TestFixture(Platform.iOS)]
    public class IOSMsalTests
    {
        private IApp _app;
        private readonly Platform _platform;
        private readonly ITestController _xamarinController = new IOSXamarinUiTestController();
        MobileTestHelper _mobileTestHelper;

        /// <summary>
        /// Initializes Xamarin UI tests
        /// </summary>
        /// <param name="platform">The platform where the tests will be performed</param>
        public IOSMsalTests(Platform platform)
        {
            this._platform = platform;
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
        public async Task RunAllTestsAsync()
        {
            var tests = new List<Func<Task>>()
            {
                AcquireTokenTestAsync,
                //AcquireTokenSilentTest,
                AcquireTokenADFSV3InteractiveFederatedTestAsync,
                AcquireTokenADFSV3InteractiveNonFederatedTestAsync,
                AcquireTokenADFSV4InteractiveFederatedTestAsync,
                AcquireTokenADFSV4InteractiveNonFederatedTestAsync,
                AcquireTokenADFSV2019InteractiveFederatedTestAsync,
                AcquireTokenADFSV2019InteractiveNonFederatedTestAsync,

                //B2CFacebookB2CLoginAuthorityAcquireTokenTest,
                //B2CFacebookMicrosoftAuthorityAcquireTokenTest,
                //B2CGoogleB2CLoginAuthorityAcquireTokenTest,
                //B2CGoogleMicrosoftAuthorityAcquireTokenTest,
                //B2CLocalAccountAcquireTokenTest,
                //B2CFacebookEditPolicyAcquireTokenTest,
            };

            var hasFailed = false;
            var stringBuilderMessage = new StringBuilder();

            foreach (Func<Task> test in tests)
            {
                try
                {
                    LogMessage($"Running test: {test.Method.Name}", stringBuilderMessage);
                    await test().ConfigureAwait(false);
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

        private static void LogMessage(string message, StringBuilder stringBuilderMessage)
        {
            Console.WriteLine(message);
            stringBuilderMessage.AppendLine(message);
        }

        /// <summary>
        /// Runs through the standard acquire token flow, using the default app configured UiBehavior = Login
        /// </summary>
        [Test]
        public async Task AcquireTokenTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token silent flow
        /// </summary>
        [Test]
        public async Task AcquireTokenSilentTestAsync()
        {
            _mobileTestHelper.AcquireTokenSilentTestHelper(_xamarinController, await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Test]
        [Ignore("Current web element search implementation is unable to properly wait for select account elements on login page. Will be addressed in future updates.")]
        public async Task PromptBehaviorConsentSelectAccountAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            _mobileTestHelper.PromptBehaviorTestHelperWithConsent(_xamarinController, labResponse);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public async Task B2CFacebookB2CLoginAuthorityAcquireTokenTestAsync()
        {
            _mobileTestHelper.B2CFacebookAcquireTokenSilentTest(_xamarinController, await LabUserHelper.GetB2CFacebookAccountAsync().ConfigureAwait(false), true);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public async Task B2CFacebookMicrosoftAuthorityAcquireTokenTestAsync()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(_xamarinController, (await LabUserHelper.GetB2CFacebookAccountAsync().ConfigureAwait(false)).User, B2CIdentityProvider.Facebook, false);
            _mobileTestHelper.B2CSilentFlowHelper(_xamarinController);
        }

        /// <summary>
        /// B2C acquire token with Facebook provider
        /// b2clogin.com authority
        /// call to edit profile authority with
        ///  UIBehavior none
        /// </summary>
        [Test]
        [Ignore("Facebook updated to Graph v3 and app center tests are failing. Ignoring for the moment.")]
        public async Task B2CFacebookEditPolicyAcquireTokenTestAsync()
        {
            _mobileTestHelper.PerformB2CSelectProviderOnlyFlow(_xamarinController, (await LabUserHelper.GetB2CFacebookAccountAsync().ConfigureAwait(false)).User, B2CIdentityProvider.Facebook, true);
            _mobileTestHelper.B2CSilentFlowHelper(_xamarinController);
            _mobileTestHelper.B2CFacebookEditPolicyAcquireTokenInteractiveTestHelper(_xamarinController);
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
        public async Task B2CGoogleB2CLoginAuthorityAcquireTokenTestAsync()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(_xamarinController, await LabUserHelper.GetB2CGoogleAccountAsync().ConfigureAwait(false), true);
        }

        /// <summary>
        /// B2C acquire token with Google provider
        /// login.microsoftonline.com authority
        /// with subsequent silent call
        /// </summary>
        [Test]
        [Ignore("UI is different in AppCenter compared w/local.")]
        public async Task B2CGoogleMicrosoftAuthorityAcquireTokenTestAsync()
        {
            _mobileTestHelper.B2CGoogleAcquireTokenSilentTest(_xamarinController, await LabUserHelper.GetB2CGoogleAccountAsync().ConfigureAwait(false), false);
        }

        /// <summary>
        /// B2C acquire token with local account
        /// b2clogin.com authority
        /// and subsequent silent call
        /// </summary>
        [Test]
        [Ignore("Fails to find B2C elements on the app during setup.")]
        public async Task B2CLocalAccountAcquireTokenTestAsync()
        {
            _mobileTestHelper.B2CLocalAccountAcquireTokenSilentTest(_xamarinController, await LabUserHelper.GetB2CLocalAccountAsync().ConfigureAwait(false), true);
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV4InteractiveFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(
                _xamarinController,
                await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4).ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV2019InteractiveFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(
                _xamarinController,
                await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019).ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV3InteractiveFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV3).ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV4 Non-Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV4InteractiveNonFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV2019 Non-Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV2019InteractiveNonFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, false).ConfigureAwait(false));
        }

        /// <summary>
        /// Runs through the standard acquire token ADFSV3 Non-Federated flow
        /// </summary>
        [Test]
        public async Task AcquireTokenADFSV3InteractiveNonFederatedTestAsync()
        {
            _mobileTestHelper.AcquireTokenInteractiveTestHelper(_xamarinController, await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, false).ConfigureAwait(false));
        }
    }
}
