//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.WindowsForms
{
    public partial class Form1 : Form
    {
        public delegate void TestMethod(Sts sts);
        public delegate Task TestMethodAsync(Sts sts);

        public Form1()
        {
            InitializeComponent();
            AdalTests.TestType = TestType.DotNet;
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            TestButton.Enabled = false;
            this.StatusTextBox.Text = string.Empty;

            await this.RunTestAsync(AdalTests.AcquireTokenPositiveTestAsync);
            /*await this.RunTestAsync(AdalTests.AcquireTokenPositiveWithoutRedirectUriOrUserIdTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenPositiveByRefreshTokenTestAsync);
            await this.RunTestAsync(AdalTests.AuthenticationContextAuthorityValidationTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithRedirectUriTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithInvalidAuthorityTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithInvalidResourceTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithInvalidClientIdTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithIncorrectUserCredentialTestAsync);
            await this.RunTestAsync(AdalTests.ExtraQueryParametersTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithAuthenticationCanceledTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenPositiveWithDefaultCacheTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenPositiveWithInMemoryCacheTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenPositiveWithNullCacheTestAsync);
            await this.RunTestAsync(AdalTests.UserInfoTestAsync);
            await this.RunTestAsync(AdalTests.MultiResourceRefreshTokenTestAsync);
            await this.RunTestAsync(AdalTests.TenantlessTestAsync);*/
            await this.RunTestAsync(AdalTests.InstanceDiscoveryTestAsync);
            /*await this.RunTestAsync(AdalTests.ForcePromptTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenNonInteractivePositiveTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenNonInteractivePositiveTestAsync, StsType.AADFederatedWithADFS3);
            await this.RunTestAsync(AdalTests.AcquireTokenPositiveWithFederatedTenantTest);
            await this.RunTestAsync(AdalTests.CorrelationIdTestAsync);
            await this.RunTestAsync(AdalTests.AuthenticationParametersDiscoveryTestAsync);
            await this.RunTestAsync(AdalTests.WebExceptionAccessTestAsync);
            await this.RunTestAsync(AdalTests.ConfidentialClientWithX509TestAsync);
            await this.RunTestAsync(AdalTests.ClientCredentialTestAsync);
            await this.RunTestAsync(AdalTests.ClientAssertionWithX509TestAsync);
            await this.RunTestAsync(AdalTests.ConfidentialClientWithJwtTestAsync);
            await this.RunTestAsync(AdalTests.ClientAssertionWithSelfSignedJwtTestAsync);
            await this.RunTestAsync(AdalTests.ConfidentialClientTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenWithPromptBehaviorNeverTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenOnBehalfAndClientCredentialTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenOnBehalfAndClientCertificateTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenOnBehalfAndClientAssertionTestAsync);
            await this.RunTestAsync(AdalTests.AcquireTokenFromCacheTestAsync);
            await this.RunTestAsync(AdalTests.MultiUserCacheTestAsync);
            await this.RunTestAsync(AdalTests.SwitchUserTestAsync);
            await this.RunTestAsync(AdalTests.CacheExpirationMarginTestAsync);*/

            TestButton.Enabled = true;
        }

        private void RunTest(TestMethod testMethod, StsType stsType = StsType.AAD)
        {
            this.RunTestSyncOrAsync(testMethod, NullTestMethodAsync, stsType).Wait();
        }

        private async Task RunTestAsync(TestMethodAsync testMethodAsync, StsType stsType = StsType.AAD)
        {
            await this.RunTestSyncOrAsync(NullTestMethod, testMethodAsync, stsType);
        }

        private async Task RunTestSyncOrAsync(TestMethod testMethod, TestMethodAsync testMethodAsync, StsType stsType)
        {
            this.AppendText(string.Format(CultureInfo.CurrentCulture, " {0}: ", (testMethod != NullTestMethod) ? testMethod.Method.Name : testMethodAsync.Method.Name));
            try
            {
                Sts sts = StsFactory.CreateSts(stsType);
                AdalTests.InitializeTest();
                AdalTests.EndBrowserDialogSession();
                if (testMethod != NullTestMethod)
                {
                    testMethod(sts);
                }

                if (testMethodAsync != NullTestMethodAsync)
                {
                    await testMethodAsync(sts);
                }

                this.AppendText("PASSED.\n", Color.Green);
            }
            catch (AssertFailedException)
            {
                this.AppendText("FAILED!\n", Color.Red);
            }
            catch (Exception ex)
            {
                this.AppendText(string.Format(CultureInfo.CurrentCulture, " FAILED with exception '{0}'!\n", ex), Color.Red);
            }
        }

        private void AppendText(string message)
        {
            this.AppendText(message, Color.Black);
        }

        private void AppendText(string message, Color color)
        {
            int begin = this.StatusTextBox.TextLength;
            this.StatusTextBox.AppendText(message);
            int end = this.StatusTextBox.TextLength;

            this.StatusTextBox.Select(begin, end - begin);
            {
                this.StatusTextBox.SelectionColor = color;
            }
            this.StatusTextBox.SelectionLength = 0;
        }

        public static void NullTestMethod(Sts sts)
        {
            
        }

        public static Task NullTestMethodAsync(Sts sts)
        {
            return null;
        }
    }
}
