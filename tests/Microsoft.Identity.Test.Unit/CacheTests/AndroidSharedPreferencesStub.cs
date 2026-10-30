// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

// Only the Android surface used by the linked accessor is needed here.
// The Android product build checks these calls against the real platform APIs.
namespace Android.Content
{
    public interface ISharedPreferences
    {
        IDictionary<string, object> All { get; }
        string GetString(string key, string defaultValue);
        ISharedPreferencesEditor Edit();
    }

    public interface ISharedPreferencesEditor
    {
        ISharedPreferencesEditor PutString(string key, string value);
        ISharedPreferencesEditor Remove(string key);
        ISharedPreferencesEditor Clear();
        void Apply();
    }

    internal enum FileCreationMode { Private }

    internal sealed class Context
    {
        internal ISharedPreferences GetSharedPreferences(string name, FileCreationMode mode) =>
            throw new NotSupportedException("Unit tests must inject SharedPreferences.");
    }
}

namespace Android.App
{
    internal static class Application
    {
        internal static Content.Context Context =>
            throw new NotSupportedException("Unit tests must inject SharedPreferences.");
    }

}

namespace Microsoft.Identity.Client.Platforms.Android
{
    // This constant is exposed by MSAL only on Android targets.
    internal static class MsalError
    {
        internal const string FailedToCreateSharedPreference = "shared_preference_creation_failed";
    }
}
