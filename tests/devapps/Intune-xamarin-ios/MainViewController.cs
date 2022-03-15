//-----------------------------------------------------------------------
// <copyright file="MainViewController.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using CoreGraphics;
using Foundation;
using System;
using System.IO;
using System.Linq;
using UIKit;
using Microsoft.Intune.MAM;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IntuneMAMSampleiOS
{
    public partial class MainViewController: UIViewController
	{
        UIButton btnMSAL;
        UIButton btnSignOut;

        internal static IPublicClientApplication PCA { get; set; }
        MainIntuneMAMComplianceDelegate _mamComplianceDelegate;
        ManualResetEvent _manualReset;

        public MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            _manualReset = new ManualResetEvent(false);
            _mamComplianceDelegate = new MainIntuneMAMComplianceDelegate(_manualReset);
            IntuneMAMComplianceManager.Instance.Delegate = _mamComplianceDelegate;

            this.btnMSAL = new UIButton(UIButtonType.System);
            this.btnMSAL.Frame = new CGRect(0, 400, 300, 30);
            this.btnMSAL.SetTitle("Acquire token", UIControlState.Normal);
            
            this.btnMSAL.TouchUpInside += BtnMSAL_TouchUpInside;

            View.AddSubview(btnMSAL);

            this.btnSignOut = new UIButton(UIButtonType.System);
            this.btnSignOut.Frame = new CGRect(0, 500, 300, 30);
            this.btnSignOut.SetTitle("Sign out", UIControlState.Normal);

            this.btnSignOut.TouchUpInside += BtnSignOut_TouchUpInside;

            View.AddSubview(btnSignOut);
        }

        /// <summary>
        /// This method shows calling pattern to access resource protected by Conditional Access with App Protection Policy
        /// </summary>
        /// <param name="sender">Sender button</param>
        /// <param name="e">arguments</param>
        private async void BtnMSAL_TouchUpInside(object sender, EventArgs e)
        {
            // The following parameters are for sample app in lab4. Please configure them as per your app registration.
            // And also update corresponding entries in info.plist -> IntuneMAMSettings -> ADALClientID and ADALRedirectUri
            string clientId = "bd9933c9-a825-4f9a-82a0-bbf23c9049fd";
            string redirectURI = $"msauth.com.xamarin.microsoftintunemamsample://auth";
            string tenantID = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            string[] Scopes = { "api://a8bf4bd3-c92d-44d0-8307-9753d975c21e/Hello.World" }; // needs admin consent
            string[] clientCapabilities = { "ProtApp" }; // Important: This must be passed to the PCABuilder

            try
            {
                // Create PCA once. Make sure that all the config parameters below are passed
                // ClientCapabilities - must have ProtApp
                if (PCA == null)
                {
                    string authority = $"https://login.microsoftonline.com/{tenantID}/";
                    var pcaBuilder = PublicClientApplicationBuilder.Create(clientId)
                                                                        .WithRedirectUri(redirectURI)
                                                                        .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
                                                                        .WithLogging(MSALLogCallback, LogLevel.Verbose)
                                                                        .WithAuthority(authority)
                                                                        .WithClientCapabilities(clientCapabilities)
                                                                        .WithHttpClientFactory(new HttpSnifferClientFactory())
                                                                        .WithBroker(true);
                    PCA = pcaBuilder.Build();
                }

                // attempt silent login.
                // If this is very first time and the device is not enrolled, it will throw MsalUiRequiredException
                // If the device is enrolled, this will succeed.
                var authResult = await DoSilentAsync(Scopes).ConfigureAwait(false);
                ShowAlert("Success Silent 1", authResult.AccessToken);
            }
            catch (MsalUiRequiredException _)
            {
                // This executes UI interaction
                try
                {
                    var interParamBuilder = PCA.AcquireTokenInteractive(Scopes)
                                                .WithParentActivityOrWindow(this)
                                                .WithUseEmbeddedWebView(true);

                    var authResult = await interParamBuilder.ExecuteAsync().ConfigureAwait(false);
                    ShowAlert("Success Interactive", authResult.AccessToken);
                }
                catch (IntuneAppProtectionPolicyRequiredException ex)
                {
                    // if the scope requires App Protection Policy,  IntuneAppProtectionPolicyRequiredException is thrown.
                    // To ensure that the policy is applied before the next call, reset the semaphore
                    _manualReset.Reset();
                    // Using IntuneMAMComplianceManager, ensure that the device is compliant.
                    // This will raise UI for compliance. After user satisfies the compliance requirements, MainIntuneMAMComplianceDelegate method will be called.
                    // the delegate will set the semaphore
                    IntuneMAMComplianceManager.Instance.RemediateComplianceForIdentity(ex.Upn, false);
                    // wait for the delegate to set it. 
                    _manualReset.WaitOne();
                    // now the device is compliant
                    System.Diagnostics.Debug.WriteLine("Complied");
                    // Attempt silent acquisition again.
                    // this should succeed
                    var authResult = await DoSilentAsync(Scopes).ConfigureAwait(false);
                    ShowAlert("Success Silent 2", authResult.AccessToken);
                }
            }
            catch (Exception ex)
            {
                ShowAlert($"{ex}", "Error");
            }
        }

        /// <summary>
        /// This method performs signout
        /// </summary>
        /// <param name="sender">Sender button</param>
        /// <param name="e">arguments</param>
        private async void BtnSignOut_TouchUpInside(object sender, EventArgs e)
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);

            while (accounts.Any())
            {
                await PCA.RemoveAsync(accounts.FirstOrDefault()).ConfigureAwait(false);
                accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            }
        }

        private async Task<AuthenticationResult> DoSilentAsync(string[] Scopes)
        {
            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            var acct = accts.FirstOrDefault();
            if (acct != null)
            {
                var silentParamBuilder = PCA.AcquireTokenSilent(Scopes, acct);
                var authResult = await silentParamBuilder.ExecuteAsync().ConfigureAwait(false);
                return authResult;
            }
            else
            {
                throw new MsalUiRequiredException("ErrCode", "ErrMessage");
            }
        }

        private void MSALLogCallback(LogLevel level, string message, bool containsPii)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void ShowAlert(string title, string message)
        {
            BeginInvokeOnMainThread(() => {
                UIAlertController alertController = new UIAlertController
                {
                    Title = title,
                    Message = message
                };

                UIAlertAction alertAction = UIAlertAction.Create("OK", UIAlertActionStyle.Default, null);
                alertController.AddAction(alertAction);

                UIPopoverPresentationController popoverPresenter = alertController.PopoverPresentationController;
                if (null != popoverPresenter)
                {
                    CGRect frame = UIScreen.MainScreen.Bounds;
                    frame.Height /= 2;
                    popoverPresenter.SourceView = this.View;
                    popoverPresenter.SourceRect = frame;
                }

                this.PresentViewController(alertController, false, null);
            });
        }
    }
}
