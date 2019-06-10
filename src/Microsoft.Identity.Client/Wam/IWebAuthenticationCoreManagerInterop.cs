// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;

namespace Microsoft.Identity.Client.Wam
{
    [ComImport, Guid("F4B8E804-811E-4436-B69C-44CB67B72084"), InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    interface IWebAuthenticationCoreManagerInterop
    {
        IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(
            IntPtr appWindow,
            [MarshalAs(UnmanagedType.IInspectable)] object request, // Windows.Security.Authentication.Web.Core.WebTokenRequest as IInspectable
            [In]ref Guid riid); // __uuidof(IAsyncOperation<WebTokenRequestResult*>)

        IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(
            IntPtr appWindow,
            [MarshalAs(UnmanagedType.IInspectable)] object request, // Windows.Security.Authentication.Web.Core.WebTokenRequest as IInspectable
            [MarshalAs(UnmanagedType.IInspectable)] object webAccount, // Windows.Security.Authentication.Web.Core.WebTokenRequest as IInspectable
            [In]ref Guid riid); // __uuidof(IAsyncOperation<WebTokenRequestResult*>)
    }
}
