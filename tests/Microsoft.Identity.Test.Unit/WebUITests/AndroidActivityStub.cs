// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

// Model only the lifecycle and intent surface used by the linked browser activities.
// Compilation against the real Android APIs is checked with the Android product build.
namespace Android.App
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ActivityAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Exported { get; set; }
    }

    public enum Result { }

    public class Activity : Content.Context
    {
        public Content.Intent Intent { get; set; }
        public Content.Context ApplicationContext => this;
        public bool IsFinishing { get; private set; }
        public List<Content.Intent> StartedActivities { get; } = new List<Content.Intent>();
        public Result? ResultCode { get; private set; }
        public Content.Intent ResultData { get; private set; }

        protected virtual void OnCreate(OS.Bundle bundle) { }
        protected virtual void OnResume() { }
        protected virtual void OnNewIntent(Content.Intent intent) { }
        protected virtual void OnSaveInstanceState(OS.Bundle bundle) { }

        public void StartActivity(Content.Intent intent) => StartedActivities.Add(intent);
        public void Finish() => IsFinishing = true;
        public void SetResult(Result code, Content.Intent data)
        {
            ResultCode = code;
            ResultData = data;
        }
    }
}

namespace Android.Content
{
    public partial class Context
    {
        public PM.PackageManager PackageManager { get; set; } = new PM.PackageManager();
    }

    [Flags]
    public enum ActivityFlags { ClearTop = 1, SingleTop = 2 }

    public class ActivityNotFoundException : Exception { }

    public sealed class Intent
    {
        public const string ActionView = "android.intent.action.VIEW";
        public const string CategoryBrowsable = "android.intent.category.BROWSABLE";
        private readonly Dictionary<string, object> _extras = new Dictionary<string, object>();

        public Intent() { }
        public Intent(string action, Net.Uri uri = null)
        {
            Action = action;
            DataString = uri?.Value;
        }
        public Intent(Context context, Type activityType) => ActivityType = activityType;

        public string Action { get; }
        public string DataString { get; set; }
        public string Package { get; private set; }
        public Type ActivityType { get; }
        public ActivityFlags Flags { get; private set; }
        public List<string> Categories { get; } = new List<string>();
        public string GetStringExtra(string key) => _extras.TryGetValue(key, out object value) ? value as string : null;
        public int GetIntExtra(string key, int defaultValue) => _extras.TryGetValue(key, out object value) ? (int)value : defaultValue;
        public void PutExtra(string key, string value) => _extras[key] = value;
        public void PutExtra(string key, int value) => _extras[key] = value;
        public void AddCategory(string category) => Categories.Add(category);
        public void SetFlags(ActivityFlags flags) => Flags = flags;
        public void SetPackage(string package) => Package = package;
    }
}

namespace Android.Content.PM
{
    public enum PackageInfoFlags { MatchAll }
    public sealed class ServiceInfo
    {
        public string PackageName { get; set; }
    }
    public sealed class ResolveInfo
    {
        public ServiceInfo ServiceInfo { get; set; }
    }
    public sealed class PackageManager
    {
        public List<ResolveInfo> Services { get; } = new List<ResolveInfo>();
        public IEnumerable<ResolveInfo> QueryIntentServices(Content.Intent intent, PackageInfoFlags flags) => Services;
    }
}

namespace Android.OS
{
    public sealed class Bundle
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
        public void PutString(string key, string value) => Values[key] = value;
    }
}

namespace Android.Net
{
    public sealed class Uri
    {
        private Uri(string value) => Value = value;
        public string Value { get; }
        public static Uri Parse(string value) => new Uri(value);
    }
}

namespace Android.Util
{
    internal static class Log
    {
        internal static List<string> Warnings { get; } = new List<string>();
        internal static int Warn(string tag, string message)
        {
            Warnings.Add(message);
            return 0;
        }
    }
}

namespace Android.Support.CustomTabs
{
    public sealed class CustomTabsIntent
    {
        public Content.Intent Intent { get; } = new Content.Intent();
        public void LaunchUrl(App.Activity activity, Net.Uri uri)
        {
            Intent.DataString = uri.Value;
            activity.StartActivity(Intent);
        }

        public sealed class Builder
        {
            public CustomTabsIntent Build() => new CustomTabsIntent();
        }
    }
}
