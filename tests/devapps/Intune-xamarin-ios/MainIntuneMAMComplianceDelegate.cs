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
            if (status == IntuneMAMComplianceStatus.Compliant)
            {
                try
                {
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
