// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Intune.Mam.Policy;

namespace Intune_xamarin_Android
{
    /// <summary>
    /// Required by the MAM SDK. A token may be needed very early in the app lifecycle so the ideal
    /// place to register the callback is in the OnMAMCreate() method of the app's implementation
    /// of IMAMApplication.
    /// See https://docs.microsoft.com/en-us/intune/app-sdk-android#account-authentication
    /// </summary>
    class MAMWEAuthCallback : Java.Lang.Object, IMAMServiceAuthenticationCallback
    {
        public string AcquireToken(string upn, string aadId, string resourceId)
        {
            System.Diagnostics.Debug.WriteLine($"Providing token via the callback for aadID: {aadId} and resource ID: {resourceId}");
            string ret = null;
            try
            {
                string[] scopes = new string[] { resourceId + "/.default" };
                var authresult = MainActivity.DoSilentAsync(scopes).GetAwaiter().GetResult();
                ret = authresult?.AccessToken;
                System.Diagnostics.Debug.WriteLine(authresult);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            IntuneSampleApp.MAMRegsiteredEvent.Set();

            return ret;
        }
    }
}
