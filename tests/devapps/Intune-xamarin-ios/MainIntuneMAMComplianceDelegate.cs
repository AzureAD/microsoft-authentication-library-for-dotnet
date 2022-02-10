using System;
using System.Linq;
using System.Threading;
using Foundation;
using Microsoft.Intune.MAM;

namespace IntuneMAMSampleiOS
{
    public class MainIntuneMAMComplianceDelegate : IntuneMAMComplianceDelegate
    {
        ManualResetEvent _manualReset;
        public MainIntuneMAMComplianceDelegate(ManualResetEvent manualReset)
        {
            this._manualReset = manualReset;
        }

        public async override void IdentityHasComplianceStatus(string identity, IntuneMAMComplianceStatus status, string errorMessage, string errorTitle)
        {
            // base.IdentityHasComplianceStatus(identity, status, errorMessage, errorTitle);
            System.Diagnostics.Debug.WriteLine($"Status = {status} Id = {identity}");
            if (status == IntuneMAMComplianceStatus.Compliant)
            {
                try
                {
                    var plist = NSUserDefaults.StandardUserDefaults;
                    var abcd = plist.StringForKey("intune_app_protection_enrollment_id_V1");
                    System.Diagnostics.Debug.WriteLine(abcd);
                    _manualReset.Set();
                    //try
                    //{
                    //    string[] Scopes = { "api://09aec9b9-0b0f-488a-81d6-72fd13a3a1c1/Hello.World" };
                    //    var accts = await MainViewController.PCA.GetAccountsAsync().ConfigureAwait(false);
                    //    var acct = accts.FirstOrDefault();
                    //    if (acct != null)
                    //    {
                    //        var silentParamBuilder = MainViewController.PCA.AcquireTokenSilent(Scopes, acct);
                    //        var authResult = await silentParamBuilder.ExecuteAsync().ConfigureAwait(false);
                    //        System.Diagnostics.Debug.WriteLine(authResult.AccessToken);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    System.Diagnostics.Debug.WriteLine(ex.Message);
                    //}
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ex = {ex.Message}");
                }

            }
        }
    }
}
