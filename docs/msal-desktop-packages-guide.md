# MSAL.NET Desktop Packages Guide

This guide explains how to choose the correct MSAL.NET Desktop package for your application.

## Package Overview

MSAL.NET now provides two separate Desktop packages to better support different Windows application types:

### 1. Microsoft.Identity.Client.Desktop (Traditional)
- **Target Frameworks**: .NET Framework 4.6.2, .NET Core 3.1
- **UI Technologies**: Windows Forms, WPF
- **WebView Implementation**: Windows Forms WebView2
- **Use Case**: Traditional Windows desktop applications

### 2. Microsoft.Identity.Client.Desktop.WinUI3 (Modern)
- **Target Frameworks**: .NET 8.0 Windows (net8.0-windows10.0.17763.0)
- **UI Technologies**: WinUI3, Windows App SDK
- **WebView Implementation**: WinUI3 WebView2
- **Use Case**: Modern Windows applications built with WinUI3

## How to Choose the Right Package

### For Windows Forms Applications
```xml
<PackageReference Include="Microsoft.Identity.Client.Desktop" Version="x.x.x" />
```

**Example project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Identity.Client.Desktop" Version="x.x.x" />
  </ItemGroup>
</Project>
```

### For WPF Applications
```xml
<PackageReference Include="Microsoft.Identity.Client.Desktop" Version="x.x.x" />
```

**Example project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Identity.Client.Desktop" Version="x.x.x" />
  </ItemGroup>
</Project>
```

### For WinUI3 Applications
```xml
<PackageReference Include="Microsoft.Identity.Client.Desktop.WinUI3" Version="x.x.x" />
```

**Example project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Identity.Client.Desktop.WinUI3" Version="x.x.x" />
    <PackageReference Include="Microsoft.WindowsAppSDK" />
  </ItemGroup>
</Project>
```

## Framework Selection Logic

### NuGet Package Selection
NuGet automatically selects the best compatible framework based on your application's target framework:

| Your App's Target Framework | Selected Package | Implementation Used |
|------------------------------|------------------|-------------------|
| `net462`, `net472`, etc. | Desktop (net462) | Windows Forms WebView2 |
| `netcoreapp3.1` | Desktop (netcoreapp3.1) | Windows Forms WebView2 |
| `net5.0`, `net6.0`, `net7.0` | Desktop (netcoreapp3.1) | Windows Forms WebView2 |
| `net8.0`, `net8.0-windows` | Desktop (netcoreapp3.1) | Windows Forms WebView2 |
| `net8.0-windows10.0.17763.0` | WinUI3 (net8.0-windows10.0.17763.0) | WinUI3 WebView2 |

### Key Benefits

1. **No Framework Conflicts**: Each package targets the appropriate frameworks for its use case
2. **Clear Intent**: Developers explicitly choose the package that matches their application type
3. **Better Performance**: Each package is optimized for its specific UI technology
4. **Easier Troubleshooting**: Clear separation reduces compatibility issues

## Migration Guide

### If you're getting COM registration errors:

1. **Check your target framework**: If you're targeting `net8.0-windows10.0.17763.0` but building a Windows Forms/WPF app, change to `net8.0-windows`
2. **Use the correct package**: Windows Forms/WPF apps should use `Microsoft.Identity.Client.Desktop`
3. **WinUI3 apps**: Use `Microsoft.Identity.Client.Desktop.WinUI3` and ensure your app is properly configured for WinUI3

### Breaking Changes
- Applications targeting `net8.0-windows10.0.17763.0` must now explicitly reference `Microsoft.Identity.Client.Desktop.WinUI3`
- The traditional Desktop package no longer includes the modern TFM target

## Example Usage

### Windows Forms Application
```csharp
var pca = PublicClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/common")
    .WithRedirectUri("http://localhost")
    .Build();

var result = await pca.AcquireTokenInteractive(scopes)
    .WithParentActivityOrWindow(this) // 'this' is your Windows Form
    .ExecuteAsync();
```

### WinUI3 Application
```csharp
var pca = PublicClientApplicationBuilder
    .Create("your-client-id")
    .WithAuthority("https://login.microsoftonline.com/common")
    .WithRedirectUri("http://localhost")
    .Build();

var result = await pca.AcquireTokenInteractive(scopes)
    .WithParentActivityOrWindow(this) // 'this' is your WinUI3 Window
    .ExecuteAsync();
```

The key difference is that the WinUI3 package will properly handle the WinUI3 Window type and use the appropriate WebView2 implementation.
