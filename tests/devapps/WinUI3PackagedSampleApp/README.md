# MSAL.NET WinUI3 Packaged Sample App

This is a Windows packaged WinUI3 sample application demonstrating how to use MSAL.NET for authentication in a packaged desktop application.

## Features

- **Interactive Authentication**: Sign in with a Microsoft account using interactive flow
- **Silent Authentication**: Acquire tokens silently when the user is already signed in
- **Sign Out**: Remove all accounts from the token cache
- **Token Information**: Display access token details and user information
- **Packaged App**: Demonstrates MSAL.NET in an MSIX packaged WinUI3 application

## Configuration

The app is configured with:
- **Client ID**: `4b0db8c2-9f26-4417-8bde-3f0e3656f8e0` (MSAL test app)
- **Authority**: `https://login.microsoftonline.com/common`
- **Scopes**: `User.Read`
- **Redirect URI**: `https://login.microsoftonline.com/common/oauth2/nativeclient`

## Building and Running

1. Ensure you have the Windows App SDK installed
2. Build the solution in Visual Studio
3. Deploy the packaged app to test MSIX packaging scenarios
4. Run the application and test authentication flows

## Key Differences from Unpackaged Apps

This packaged sample demonstrates:
- MSIX packaging configuration
- Package.appxmanifest setup for WinUI3
- Proper capabilities for network access
- Packaged app identity and publisher information
- Windows App SDK integration in a packaged context

## MSAL.NET Integration

The app uses:
- `Microsoft.Identity.Client` (core MSAL.NET library)
- `Microsoft.Identity.Client.Desktop.WinUI3` (WinUI3-specific extensions)
- Proper WebView2 integration for authentication flows in packaged apps
