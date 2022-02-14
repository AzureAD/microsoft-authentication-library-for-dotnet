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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ex = {ex.Message}");
                }

            }
        }
    }
}
