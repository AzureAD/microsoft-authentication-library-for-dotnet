// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace AdalMacTestApp
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSTextView textView { get; set; }

		[Action ("AcquireInteractiveClicked:")]
		partial void AcquireInteractiveClicked (Foundation.NSObject sender);

		[Action ("AcquireSilentClicked:")]
		partial void AcquireSilentClicked (Foundation.NSObject sender);

		[Action ("ClearCacheClicked:")]
		partial void ClearCacheClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (textView != null) {
				textView.Dispose ();
				textView = null;
			}
		}
	}
}
