
//Typeforward 

using System.Runtime.CompilerServices;
#if ANDROID
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PlatformParameters))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationAgentContinuationHelper))]
#endif

#if iOS
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PlatformParameters))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContinuationHelper))]
#endif

#if DESKTOP
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PlatformParameters))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.ClientAssertionCertificate))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.SecureClientSecret))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.WindowsFormsWebAuthenticationDialogBase))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.WebBrowserNavigateErrorEventArgs))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.WindowsFormsWebAuthenticationDialog))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.Native.AsymmetricPaddingMode))]
#endif

#if NETSTANDARD1_3
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions))]
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.ClientAssertionCertificate))]
#endif

#if WINDOWS_APP
[assembly: TypeForwardedTo(typeof(Microsoft.IdentityModel.Clients.ActiveDirectory.PlatformParameters))]
#endif