
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

//XForms
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~M:XForms.AcquirePage.OnAcquireClicked(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~M:XForms.AcquirePage.OnAcquireSilentlyClicked(System.Object,System.EventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.Droid.AcquirePageRenderer.#OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs`1<Xamarin.Forms.Page>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.Droid.MainActivity.#OnCreate(Android.OS.Bundle)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.iOS.AppDelegate.#FinishedLaunching(UIKit.UIApplication,Foundation.NSDictionary)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.iOS.AcquirePageRenderer.#OnElementChanged(Xamarin.Forms.Platform.iOS.VisualElementChangedEventArgs)")]

//Desktop Test App
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~T:DesktopTestApp.MainForm(System.Object,System.EventArgs)")]

//Sample App
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~T:SampleApp.MainForm(System.Object,System.EventArgs)")]

//Microsoft.Identity.Client
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~M:Microsoft.Identity.Client.DispatcherTaskExtensions.RunTaskAsync``1(Windows.UI.Core.CoreDispatcher,System.Func{System.Threading.Tasks.Task{``0}},Windows.UI.Core.CoreDispatcherPriority)~System.Threading.Tasks.Task{``0}")]

//Automation
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Reviewed", Scope = "member", Target = "~M:AutomationApp.AutomationUI.GoBtn_Click(System.Object,System.EventArgs)")]

//IOS Suppressions
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
using System.Diagnostics.CodeAnalysis;

//XForms
[assembly: SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "<Pending>", Scope = "member", Target = "~M:XForms.AcquirePage.OnAcquireClicked(System.Object,System.EventArgs)")]
[assembly: SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "<Pending>", Scope = "member", Target = "~M:XForms.AcquirePage.OnAcquireSilentlyClicked(System.Object,System.EventArgs)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.Droid.AcquirePageRenderer.#OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs`1<Xamarin.Forms.Page>)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.Droid.MainActivity.#OnCreate(Android.OS.Bundle)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.iOS.AppDelegate.#FinishedLaunching(UIKit.UIApplication,Foundation.NSDictionary)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "XForms.iOS.AcquirePageRenderer.#OnElementChanged(Xamarin.Forms.Platform.iOS.VisualElementChangedEventArgs)")]

//IOS Suppressions
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.BrokerHelper")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.WebUI.#authorizationResult")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.CryptographyHelper")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.DeviceAuthHelper")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.Logger")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformInformation")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformParameters")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.TokenCachePlugin")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.WebUI")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.WebUIFactory")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.AuthenticationAgentUIViewController+ReturnCodeCallback.#Invoke(Microsoft.Identity.Client.Internal.AuthorizationResult)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.Logger.#.ctor()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#.ctor()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.AuthenticationAgentUIViewController.#ViewDidLoad()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.AuthenticationAgentUIViewController.#CancelAuthentication(System.Object,System.EventArgs)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.BrokerHelper.#AcquireTokenUsingBroker(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.Exception,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Verbose(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Information(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Warning(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.MsalInitializer.#Initialize()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetAssemblyFileVersionAttribute()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#BeforeAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#AfterAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.WebUI.#.ctor(Microsoft.Identity.Client.IPlatformParameters)")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#SetAuthorizationResult(Microsoft.Identity.Client.Internal.AuthorizationResult)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#Authenticate(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#CallbackMethod(Microsoft.Identity.Client.Internal.AuthorizationResult)")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUIFactory.#CreateAuthenticationDialog(Microsoft.Identity.Client.IPlatformParameters)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#CanInvokeBroker")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.BrokerHelper.#AcquireTokenUsingBroker(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.CryptographyHelper.#CreateBase64UrlEncodedSha256Hash(System.String)")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.DeviceAuthHelper.#CanHandleDeviceAuthChallenge")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.DeviceAuthHelper.#CreateDeviceAuthChallengeResponse(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.Exception,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Verbose(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Information(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Warning(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetProductName()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetEnvironmentVariable(System.String)")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetUserPrincipalNameAsync()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetProcessorArchitecture()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetOperatingSystem()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetDeviceModel()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetAssemblyFileVersionAttribute()")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#BeforeAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#AfterAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUIFactory.#CreateAuthenticationDialog(Microsoft.Identity.Client.IPlatformParameters)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2132:DefaultConstructorsMustHaveConsistentTransparencyFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.Logger")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2132:DefaultConstructorsMustHaveConsistentTransparencyFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformInformation")]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
	SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.CryptographyHelper.#GenerateCodeVerifier()")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.CryptographyHelper.#GenerateCodeVerifier()")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#Authenticate(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#DidFinish(SafariServices.SFSafariViewController)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.AuthenticationContinuationHelper.#SetAuthenticationContinuationEventArgs(Foundation.NSUrl,System.String)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.TokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(Microsoft.Identity.Client.Internal.Cache.RefreshTokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#AllRefreshTokens()")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#GetAllAccessTokensForClient()")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.TokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(Microsoft.Identity.Client.Internal.Cache.RefreshTokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheItem)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(System.String)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(System.String)")]

// Async
[assembly: SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Top level event handlers should use void", Scope = "member", Target = "~M:SampleApp.MainForm.TabControl1_SelectedIndexChanged(System.Object,System.EventArgs)")]
[assembly: SuppressMessage("AsyncUsage.CSharp.Reliability", "AvoidAsyncVoid:Avoid async void", Justification = "Top level event handlers should use void", Scope = "member", Target = "~M:SampleApp.MainForm.pictureBox1_Click(System.Object,System.EventArgs)")]