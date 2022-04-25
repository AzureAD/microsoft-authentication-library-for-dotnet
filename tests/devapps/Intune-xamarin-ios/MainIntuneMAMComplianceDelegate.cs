using System;
using System.Linq;
using System.Threading;
using Foundation;
using Microsoft.Intune.MAM;

namespace IntuneMAMSampleiOS
{
    /// <summary>
    /// When device becomes Intune MAM compliant, IdentityHasComplianceStatus method in this class will be called.
    /// It will set the event that will let the calling app know that the device is now compliant.
    /// And app can take the further actions such as calling silent token acquisition.
    /// </summary>
    public class MainIntuneMAMComplianceDelegate : IntuneMAMComplianceDelegate
    {
        private readonly ManualResetEvent _manualReset;
        public MainIntuneMAMComplianceDelegate(ManualResetEvent manualReset)
        {
            _manualReset = manualReset;
            _manualReset.Reset();
        }

        public async override void IdentityHasComplianceStatus(string identity, IntuneMAMComplianceStatus status, string errorMessage, string errorTitle)
        {
            if (status == IntuneMAMComplianceStatus.Compliant)
            {
                try
                {
                    // Now the app is compliant, set the event. It will notify the App to take the next steps.
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
