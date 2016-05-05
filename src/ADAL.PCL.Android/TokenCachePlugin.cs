//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Android.App;
using Android.Content;
using System;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        private const string SharedPreferencesName = "ActiveDirectoryAuthenticationLibrary";
        private const string SharedPreferencesKey = "cache";

        public IntPtr Handle 
        {
            get { return IntPtr.Zero; }
        }

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.Count > 0)
            {
                // We assume that the cache has not changed since last write
                return;
            }

            try
            {
                ISharedPreferences preferences = Application.Context.GetSharedPreferences(SharedPreferencesName, FileCreationMode.Private);
                string stateString = preferences.GetString(SharedPreferencesKey, null);
                if (stateString != null)
                {
                    byte[] state = Convert.FromBase64String(stateString);
                    args.TokenCache.Deserialize(state);
                }
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Warning(null, "Failed to load cache: " + ex);
                // Ignore as the cache seems to be corrupt
            }
        }
        
        public void AfterAccess(TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                try
                {
                    ISharedPreferences preferences = Application.Context.GetSharedPreferences(SharedPreferencesName, FileCreationMode.Private);
                    ISharedPreferencesEditor editor = preferences.Edit();
                    editor.Remove(SharedPreferencesKey);

                    if (args.TokenCache.Count > 0)
                    {
                        byte[] state = args.TokenCache.Serialize();
                        string stateString = Convert.ToBase64String(state);
                        editor.PutString(SharedPreferencesKey, stateString);
                    }

                    editor.Apply();
                    args.TokenCache.HasStateChanged = false;
                }
                catch (Exception ex)
                {
                    PlatformPlugin.Logger.Warning(null, "Failed to save cache: " + ex);
                }
            }
        }
    }
}
