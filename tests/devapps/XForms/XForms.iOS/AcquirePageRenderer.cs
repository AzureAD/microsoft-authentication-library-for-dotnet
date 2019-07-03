// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Forms.Platform.iOS;

namespace XForms.iOS
{
    internal class AcquirePageRenderer : PageRenderer
    {

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

#if BUILDENV == APPCENTER
            Xamarin.Calabash.Start();
#endif
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
        }
    }
}
