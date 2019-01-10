// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MacCocoaApp
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        AppKit.NSTextField OutputLabel { get; set; }

        [Action ("ClearCacheClickAsync:")]
        partial void ClearCacheClickAsync (Foundation.NSObject sender);

        [Action ("GetTokenClickAsync:")]
        partial void GetTokenClickAsync (Foundation.NSObject sender);

        [Action ("GetTokenDeviceCodeAsync:")]
        partial void GetTokenDeviceCodeAsync (Foundation.NSObject sender);

        [Action ("OnButtonPushAsync:")]
        partial void OnButtonPushAsync (Foundation.NSObject sender);

        [Action ("ShowCacheStatus:")]
        partial void ShowCacheStatus (Foundation.NSObject sender);

        [Action ("ShowCacheStatusAsync:")]
        partial void ShowCacheStatusAsync (Foundation.NSObject sender);
        
        void ReleaseDesignerOutlets ()
        {
            if (OutputLabel != null) {
                OutputLabel.Dispose ();
                OutputLabel = null;
            }
        }
    }
}
