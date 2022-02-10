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
            this.btnMSAL.SetTitle("MSAL", UIControlState.Normal);
            
            this.btnMSAL.TouchUpInside += BtnMSAL_TouchUpInside;

            View.AddSubview(btnMSAL);
        }

        private async void BtnMSAL_TouchUpInside(object sender, EventArgs e)
        {
            bool useLab4 = false;
            bool useLab20 = !useLab4;

            string clientId = "6d50af5d-2529-4ff4-912f-c1d6ad06953e"; // my app in the lab
            string redirectURI = $"msauth.com.xamarin.microsoftintunemamsample://auth";
            string tenantID = "7257a09f-53cc-4a91-aca8-0cb6713642a5";
            string[] Scopes = { "https://xamarintruemamenterpriseapp-msidlab20.msappproxy.net//Hello.World" };
            string[] clientCapabilities = { "ProtApp" };

            if (useLab4)
            {
                // for IDLABTRUEMAMCA@msidlab4.onmicrosoft.com
                // com.xamarin.microsoftintunemamsample
                // Redirect URI = msauth.com.xamarin.microsoftintunemamsample://auth
                // MSAL config
                // let kClientID = "cc2ef30a-30ff-404e-b0c8-8022dc941b51"
                // let kRedirectUri = "msauth.com.xamarin.microsoftintunemamsample://auth"
                // let kAuthority = "https://login.microsoftonline.com/common"
                // let kGraphEndpoint = "https://graph.microsoft.com/"

                // string clientId = "cc2ef30a-30ff-404e-b0c8-8022dc941b51"; // my app in the lab
                // string[] Scopes = { "User.Read" };
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
                    var interParamBuilder = PCA.AcquireTokenInteractive(Scopes)
                                                .WithParentActivityOrWindow(this)
                                                .WithUseEmbeddedWebView(true);

                    var authResult = await interParamBuilder.ExecuteAsync().ConfigureAwait(false);
                    ShowAlert("Success Interactive 1", authResult.AccessToken);
                }
            }
            catch (IntuneAppProtectionPolicyRequiredException ex)
            {
                _manualReset.Reset();

                IntuneMAMComplianceManager.Instance.RemediateComplianceForIdentity(ex.Upn, false);
                _manualReset.WaitOne();
                System.Diagnostics.Debug.WriteLine("Complied");
                var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
                var acct = accts.FirstOrDefault();
                if (acct != null)
                {
                    try
                    {
                        var silentParamBuilder = PCA.AcquireTokenSilent(Scopes, acct);
                        var authResult = await silentParamBuilder.ExecuteAsync().ConfigureAwait(false);
                        ShowAlert("Success Silent 1", authResult.AccessToken);
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine(ex2.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert($"Error {((MsalException)ex).ErrorCode}", ex.Message + "\r\n" + ex.StackTrace);
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
