// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;
using XForms;
using XForms.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Security;

[assembly: ExportRenderer(typeof(AcquirePage), typeof(AcquirePageRenderer))]

namespace XForms.iOS
{
    internal class AcquirePageRenderer : PageRenderer
    {
        AcquirePage page;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            page = e.NewElement as AcquirePage;

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
