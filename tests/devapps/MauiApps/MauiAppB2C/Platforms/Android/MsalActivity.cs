// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Identity.Client;

namespace MauiAppBasic.Platforms.Android.Resources
{
    [Activity(Exported =true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
        DataHost = "auth",
        DataScheme = "msale5737214-6372-472c-a85a-68e8fbe6cf3c")]
    public class MsalActivity : BrowserTabActivity
    {
    }
}
