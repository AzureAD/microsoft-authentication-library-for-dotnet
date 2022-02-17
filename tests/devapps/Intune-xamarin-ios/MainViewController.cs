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
        UISwitch dirSwitch;

        internal static IPublicClientApplication PCA { get; set; }
        MainIntuneMAMComplianceDelegate _mamComplianceDelegate;
        ManualResetEvent _manualReset;

        public MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            _manualReset = new ManualResetEvent(false);
            _manualReset.Reset();
            _mamComplianceDelegate = new MainIntuneMAMComplianceDelegate(_manualReset);
            IntuneMAMComplianceManager.Instance.Delegate = _mamComplianceDelegate;

            this.btnMSAL = new UIButton(UIButtonType.System);
            this.btnMSAL.Frame = new CGRect(0, 400, 300, 30);
            this.btnMSAL.SetTitle("Acquire token", UIControlState.Normal);
            
            this.btnMSAL.TouchUpInside += BtnMSAL_TouchUpInside;

            View.AddSubview(btnMSAL);
        }

        /// <summary>
        /// This method shows calling pattern to access resource protected by Conditional Access with App Protection Policy
        /// </summary>
        /// <param name="sender">Sender button</param>
        /// <param name="e">arguments</param>
        private async void BtnMSAL_TouchUpInside(object sender, EventArgs e)
        {
            bool useLab4 = false;
            bool useLab20 = !useLab4;

            // Configure the following parameters
            string clientId = "6d50af5d-2529-4ff4-912f-c1d6ad06953e"; // your app id
            string redirectURI = $"msauth.com.xamarin.microsoftintunemamsample://auth"; // redirect URI for the app as registred in the AD
            string tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5"; // your tenantID
            string[] Scopes = { "https://xamarintruemamenterpriseapp-msidlab20.msappproxy.net//Hello.World" }; // desired scope(s)
            string[] clientCapabilities = { "ProtApp" }; // Important: This must be passed to the PCABuilder

            // This is for now. It will go away when lab is set.
            if (useLab4)
            {
                // for xammamtrust@msidlab4.onmicrosoft.com
                clientId = "39c14f70-8284-4671-b54b-bc51aa1a1b18"; // my app in the lab
                redirectURI = $"msauth.com.xamarin.microsoftintunemamsample://auth";
                tenantID = "30a4dfae-ad95-4192-b5c0-1b8498b83ad3";
                Scopes[0] = "api://34806c4d-ae1c-4836-9aa1-6d7a8ffa6831/Hello.World"; // needs admin consent
            }

            // IDLAB20TrueMAMCA@msidlab20.onmicrosoft.com
            if (useLab20)
            {
                clientId = "6d50af5d-2529-4ff4-912f-c1d6ad06953e"; // my app in the lab
                redirectURI = $"msauth.com.xamarin.microsoftintunemamsample://auth";
                tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5";
                Scopes[0] = "api://09aec9b9-0b0f-488a-81d6-72fd13a3a1c1/Hello.World"; // needs admin consent
            }

            try
            {
                // Create PCA once. Make sure that all the config parameters below are passed
                // ClientCapabilities - must have ProtApp
                if (PCA == null)
                {
                    var pcaBuilder = PublicClientApplicationBuilder.Create(clientId)
                                                                        .WithRedirectUri(redirectURI)
                                                                        .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
                                                                        .WithLogging(MSALLogCallback, LogLevel.Verbose)
                                                                        .WithTenantId(tenantID)
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
                ShowAlert($"Error {((MsalException)ex).ErrorCode}", ex.Message + "\r\n" + ex.StackTrace);
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

        /*
                partial void buttonUrl_TouchUpInside (UIButton sender)
                {
                    string url = textUrl.Text;

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        url = "http://www.microsoft.com/en-us/server-cloud/products/microsoft-intune/";
                    }

                    UIApplication.SharedApplication.OpenUrl (NSUrl.FromString (url));
                }

                partial void buttonShare_TouchUpInside (UIButton sender)
                {
                    NSString text = new NSString ("Test Content Sharing from Intune Sample App");
                    UIActivityViewController avc = new UIActivityViewController (new NSObject[] { text }, null);

                    if (avc.PopoverPresentationController != null)
                    {
                        UIViewController topController = UIApplication.SharedApplication.KeyWindow.RootViewController;
                        CGRect frame = UIScreen.MainScreen.Bounds;
                        frame.Height /= 2;
                        avc.PopoverPresentationController.SourceView = topController.View;
                        avc.PopoverPresentationController.SourceRect = frame;
                        avc.PopoverPresentationController.PermittedArrowDirections = UIPopoverArrowDirection.Unknown;
                    }

                    this.PresentViewController(avc, true, null);
                }

                partial void ButtonSave_TouchUpInside(UIButton sender)
                {
                    // Apps are responsible for enforcing Save-As policy
                    if (!IntuneMAMPolicyManager.Instance.Policy.IsSaveToAllowedForLocation(IntuneMAMSaveLocation.LocalDrive, IntuneMAMEnrollmentManager.Instance.EnrolledAccount))
                    {
                        this.ShowAlert("Blocked", "Blocked from writing to local location");
                        return;
                    }

                    string fileName = "intune-test.txt";
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);

                    try
                    {
                        File.WriteAllText(path, "Test Save to Personal");
                        this.ShowAlert("Success", "Wrote to " + fileName);
                    }
                    catch (Exception)
                    {
                        this.ShowAlert("Failed", "Failed to write to " + fileName);
                    }
                }

                partial void ButtonLogIn_TouchUpInside(UIButton sender)
                {
                    IntuneMAMEnrollmentManager.Instance.LoginAndEnrollAccount(this.textEmail.Text);
                }

                partial void ButtonLogOut_TouchUpInside(UIButton sender)
                {
                    IntuneMAMEnrollmentManager.Instance.DeRegisterAndUnenrollAccount(IntuneMAMEnrollmentManager.Instance.EnrolledAccount, true);
                }

                bool DismissKeyboard (UITextField textField)
                {
                    textField.ResignFirstResponder ();
                    return true;
                }

                public void HideLogInButton()
                {
                    this.buttonLogIn.Hidden = true;
                    this.textEmail.Hidden = true;
                    this.labelEmail.Hidden = true;
                    this.buttonLogOut.Hidden = false;
                }

                public void HideLogOutButton()
                {
                    this.buttonLogOut.Hidden = true;
                    this.textEmail.Hidden = false;
                    this.labelEmail.Hidden = false;
                    this.buttonLogIn.Hidden = false;
                }
        */
    }
}
