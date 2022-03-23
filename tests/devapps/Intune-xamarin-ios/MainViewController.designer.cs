// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace IntuneMAMSampleiOS
{
    [Register ("MainViewController")]
    partial class MainViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton buttonLogIn { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton buttonLogOut { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton buttonSave { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton buttonShare { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton buttonUrl { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel labelEmail { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField textCopy { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField textEmail { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField textUrl { get; set; }

        [Action ("ButtonLogIn_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ButtonLogIn_TouchUpInside (UIKit.UIButton sender);

        [Action ("ButtonLogOut_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ButtonLogOut_TouchUpInside (UIKit.UIButton sender);

        [Action ("ButtonSave_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void ButtonSave_TouchUpInside (UIKit.UIButton sender);

        [Action ("buttonShare_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void buttonShare_TouchUpInside (UIKit.UIButton sender);

        [Action ("buttonUrl_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void buttonUrl_TouchUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (buttonLogIn != null) {
                buttonLogIn.Dispose ();
                buttonLogIn = null;
            }

            if (buttonLogOut != null) {
                buttonLogOut.Dispose ();
                buttonLogOut = null;
            }

            if (buttonSave != null) {
                buttonSave.Dispose ();
                buttonSave = null;
            }

            if (buttonShare != null) {
                buttonShare.Dispose ();
                buttonShare = null;
            }

            if (buttonUrl != null) {
                buttonUrl.Dispose ();
                buttonUrl = null;
            }

            if (labelEmail != null) {
                labelEmail.Dispose ();
                labelEmail = null;
            }

            if (textCopy != null) {
                textCopy.Dispose ();
                textCopy = null;
            }

            if (textEmail != null) {
                textEmail.Dispose ();
                textEmail = null;
            }

            if (textUrl != null) {
                textUrl.Dispose ();
                textUrl = null;
            }
        }
    }
}