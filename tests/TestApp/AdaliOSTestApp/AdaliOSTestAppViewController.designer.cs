// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;

namespace AdaliOSTestApp
{
	[Foundation.Register("AdaliOSTestAppViewController")]
	partial class AdaliOSTestAppViewController
	{
		[Foundation.Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel ReportLabel { get; set; }

		[Foundation.Action("UIButton11_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void UIButton11_TouchUpInside (UIButton sender);

		[Foundation.Action("UIButton16_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void UIButton16_TouchUpInside (UIButton sender);

		[Foundation.Action("UIButton25_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void UIButton25_TouchUpInside (UIButton sender);

		void ReleaseDesignerOutlets ()
		{
			if (ReportLabel != null) {
				ReportLabel.Dispose ();
				ReportLabel = null;
			}
		}
	}
}
