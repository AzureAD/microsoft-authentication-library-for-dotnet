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
        /// <summary>
        /// MAM expects that this method performs silent authentication for the give resourceID
        /// Note. This resource is not the same one from the App. The resource is api for MAM service.
        /// </summary>
        /// <param name="upn">UPN of the user</param>
        /// <param name="aadId">Active directory ID</param>
        /// <param name="resourceId">ID of the resource.</param>
        /// <returns>Access token</returns>
        public string AcquireToken(string upn, string aadId, string resourceId)
        {
            string ret = null;
            try
            {
                // append with /.default
                string[] scopes = new string[] { resourceId + "/.default" };

                // do the silent authentication for the resource
                var authresult = PCAWrapper.Instance.DoSilentAsync(scopes).GetAwaiter().GetResult();
                ret = authresult?.AccessToken;
            }
            catch (Exception ex)
            {
                // write the exception and return null
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return ret;
        }
    }
}
