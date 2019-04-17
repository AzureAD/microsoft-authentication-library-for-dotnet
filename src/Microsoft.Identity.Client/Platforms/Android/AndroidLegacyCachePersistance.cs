// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidLegacyCachePersistence : ILegacyCachePersistence
    {
        private const string SharedPreferencesName = "ActiveDirectoryAuthenticationLibrary";
        private const string SharedPreferencesKey = "cache";

        private readonly ICoreLogger _logger;

        public AndroidLegacyCachePersistence(ICoreLogger logger)
        {
            _logger = logger;
        }

        byte[] ILegacyCachePersistence.LoadCache()
        {
            try
            {
                ISharedPreferences preferences = Application.Context.GetSharedPreferences(SharedPreferencesName, FileCreationMode.Private);
                string stateString = preferences.GetString(SharedPreferencesKey, null);
                if (stateString != null)
                {
                    return Convert.FromBase64String(stateString);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "An error occurred while reading the adal cache: ");
                // Ignore as the cache seems to be corrupt
            }

            return null;
        }

        void ILegacyCachePersistence.WriteCache(byte[] serializedCache)
        {
            try
            {
                ISharedPreferences preferences = Application.Context.GetSharedPreferences(SharedPreferencesName, FileCreationMode.Private);
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.Remove(SharedPreferencesKey);
                string stateString = Convert.ToBase64String(serializedCache);
                editor.PutString(SharedPreferencesKey, stateString);
                editor.Apply();
            }
            catch (Exception ex)
            {
                _logger.ErrorPiiWithPrefix(ex, "Failed to save adal cache: ");
            }
        }
    }
}
