
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
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.BrokerHelper")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.WebUI.#authorizationResult")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.CryptographyHelper")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.DeviceAuthHelper")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.Logger")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformInformation")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformParameters")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.TokenCachePlugin")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.WebUI")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.WebUIFactory")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.AuthenticationAgentUIViewController+ReturnCodeCallback.#Invoke(Microsoft.Identity.Client.Internal.AuthorizationResult)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.Logger.#.ctor()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#.ctor()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.AuthenticationAgentUIViewController.#ViewDidLoad()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.AuthenticationAgentUIViewController.#CancelAuthentication(System.Object,System.EventArgs)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.BrokerHelper.#AcquireTokenUsingBroker(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.Exception,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Verbose(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Information(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Warning(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.MsalInitializer.#Initialize()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetAssemblyFileVersionAttribute()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#BeforeAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#AfterAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.WebUI.#.ctor(Microsoft.Identity.Client.IPlatformParameters)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#SetAuthorizationResult(Microsoft.Identity.Client.Internal.AuthorizationResult)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#Authenticate(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#CallbackMethod(Microsoft.Identity.Client.Internal.AuthorizationResult)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUIFactory.#CreateAuthenticationDialog(Microsoft.Identity.Client.IPlatformParameters)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#PlatformParameters")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.BrokerHelper.#CanInvokeBroker")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.BrokerHelper.#AcquireTokenUsingBroker(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.CryptographyHelper.#CreateBase64UrlEncodedSha256Hash(System.String)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.DeviceAuthHelper.#CanHandleDeviceAuthChallenge")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.DeviceAuthHelper.#CreateDeviceAuthChallengeResponse(System.Collections.Generic.IDictionary`2<System.String,System.String>)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.Exception,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Verbose(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Information(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Warning(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetProductName()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetEnvironmentVariable(System.String)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetUserPrincipalNameAsync()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetProcessorArchitecture()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetOperatingSystem()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetDeviceModel()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target = "Microsoft.Identity.Client.PlatformInformation.#GetAssemblyFileVersionAttribute()")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#BeforeAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.TokenCachePlugin.#AfterAccess(Microsoft.Identity.Client.TokenCacheNotificationArgs)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,System.Collections.Generic.IDictionary`2<System.String,System.String>,Microsoft.Identity.Client.Internal.RequestContext)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.WebUIFactory.#CreateAuthenticationDialog(Microsoft.Identity.Client.IPlatformParameters)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2132:DefaultConstructorsMustHaveConsistentTransparencyFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.Logger")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2132:DefaultConstructorsMustHaveConsistentTransparencyFxCopRule", Scope = "type",
        Target = "Microsoft.Identity.Client.PlatformInformation")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
        "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member",
        Target =
            "Microsoft.Identity.Client.Logger.#Error(Microsoft.Identity.Client.Internal.RequestContext,System.String,System.String)"
        )]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.CryptographyHelper.#GenerateCodeVerifier()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.CryptographyHelper.#GenerateCodeVerifier()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#AcquireAuthorizationAsync(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#Authenticate(System.Uri,System.Uri,Microsoft.Identity.Client.Internal.RequestContext)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.WebUI.#DidFinish(SafariServices.SFSafariViewController)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.AuthenticationContinuationHelper.#SetAuthenticationContinuationEventArgs(Foundation.NSUrl,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.TokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(Microsoft.Identity.Client.Internal.Cache.RefreshTokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#AllRefreshTokens()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#GetAllAccessTokensForClient()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.TokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(Microsoft.Identity.Client.Internal.Cache.RefreshTokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheKey)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(Microsoft.Identity.Client.Internal.Cache.AccessTokenCacheItem)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveAccessToken(System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#SaveRefreshToken(System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteAccessToken(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule", Scope = "member", Target = "Microsoft.Identity.Client.TokenCachePlugin.#DeleteRefreshToken(System.String)")]