// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace AdaliOSSample
{
	[Register ("AdaliOSSampleViewController")]
	partial class AdaliOSSampleViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel ReportLabel { get; set; }

		[Action ("UIButton11_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void UIButton11_TouchUpInside (UIButton sender);

		[Action ("UIButton16_TouchUpInside:")]
		[GeneratedCode ("iOS Designer", "1.0")]
		partial void UIButton16_TouchUpInside (UIButton sender);

		void ReleaseDesignerOutlets ()
		{
			if (ReportLabel != null) {
				ReportLabel.Dispose ();
				ReportLabel = null;
			}
		}
	}
}
