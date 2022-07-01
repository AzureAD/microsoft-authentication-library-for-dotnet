// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;
using WinRT;

namespace Microsoft.Identity.Client.Platforms.net5win
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Uses a non-traditional method of async")]
    internal static class WebAuthenticationCoreManagerInterop
    {
        public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr hWnd, WebTokenRequest request)
        {
            IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop =
                WebAuthenticationCoreManager.As<IWebAuthenticationCoreManagerInterop>();
            Guid guid = WinRT.GuidGenerator.CreateIID(typeof(IAsyncOperation<WebTokenRequestResult>));

            var requestPtr = MarshalInspectable<WebTokenRequest>.FromManaged(request);

            webAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(
               hWnd,
               requestPtr,
               ref guid,
               out IntPtr result);

            return MarshalInterface<IAsyncOperation<WebTokenRequestResult>>.FromAbi(result);
        }

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(
            IntPtr hWnd, WebTokenRequest request, WebAccount webAccount)
        {
            IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop =
                WebAuthenticationCoreManager.As<IWebAuthenticationCoreManagerInterop>();
            Guid guid = WinRT.GuidGenerator.CreateIID(typeof(IAsyncOperation<WebTokenRequestResult>));

            var requestPtr = MarshalInspectable<WebTokenRequest>.FromManaged(request);
            var webAccountPtr = MarshalInspectable<WebAccount>.FromManaged(webAccount);
            webAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(
                hWnd,
                requestPtr,
                webAccountPtr,
                ref guid,
                out IntPtr result);

            return MarshalInterface<IAsyncOperation<WebTokenRequestResult>>.FromAbi(result);
        }
    }

    //------------------------IWebAuthenticationCoreManagerInterop----------------------------

    //MIDL_INTERFACE("F4B8E804-811E-4436-B69C-44CB67B72084")
    //IWebAuthenticationCoreManagerInterop : public IInspectable
    //{
    //public:
    //    virtual HRESULT STDMETHODCALLTYPE RequestTokenForWindowAsync( 
    //        /* [in] */ HWND appWindow,
    //        /* [in] */ IInspectable *request,
    //        /* [in] */ REFIID riid,
    //        /* [iid_is][retval][out] */ void **asyncInfo) = 0;
    //    virtual HRESULT STDMETHODCALLTYPE RequestTokenWithWebAccountForWindowAsync( 
    //        /* [in] */ HWND appWindow,
    //        /* [in] */ IInspectable *request,
    //        /* [in] */ IInspectable *webAccount,
    //        /* [in] */ REFIID riid,
    //        /* [iid_is][retval][out] */ void **asyncInfo) = 0;
    //};
    [System.Runtime.InteropServices.Guid("F4B8E804-811E-4436-B69C-44CB67B72084")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    [System.Runtime.InteropServices.ComImport]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Uses a non-traditional method of async")]
    internal interface IWebAuthenticationCoreManagerInterop
    {
        // Note: Invoking methods on ComInterfaceType.InterfaceIsIInspectable interfaces
        // no longer appears supported in the runtime (probably with removal of WinRT support),
        // so simulate with IUnknown.
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out WinRT.TrustLevel trustLevel);

        void RequestTokenForWindowAsync(
            IntPtr appWindow,
            IntPtr request, // WebTokenRequest
            ref Guid riid,
            out IntPtr result); // IAsyncOperation<WebTokenRequestResult>

        void RequestTokenWithWebAccountForWindowAsync(
            IntPtr appWindow,
            IntPtr request, // WebTokenRequest
            IntPtr webAccount, // WebAccount
            ref Guid riid,
            out IntPtr result); // IAsyncOperation<WebTokenRequestResult>
    }

    //------------------------IAccountsSettingsPaneInterop----------------------------
    //MIDL_INTERFACE("D3EE12AD-3865-4362-9746-B75A682DF0E6")
    //IAccountsSettingsPaneInterop : public IInspectable
    //{
    //public:
    //    virtual HRESULT STDMETHODCALLTYPE GetForWindow(
    //        /* [in] */ __RPC__in HWND appWindow,
    //        /* [in] */ __RPC__in REFIID riid,
    //        /* [iid_is][retval][out] */ __RPC__deref_out_opt void** accountsSettingsPane) = 0;
    //    virtual HRESULT STDMETHODCALLTYPE ShowManageAccountsForWindowAsync(
    //        /* [in] */ __RPC__in HWND appWindow,
    //        /* [in] */ __RPC__in REFIID riid,
    //        /* [iid_is][retval][out] */ __RPC__deref_out_opt void** asyncAction) = 0;
    //    virtual HRESULT STDMETHODCALLTYPE ShowAddAccountForWindowAsync(
    //        /* [in] */ __RPC__in HWND appWindow,
    //        /* [in] */ __RPC__in REFIID riid,
    //        /* [iid_is][retval][out] */ __RPC__deref_out_opt void** asyncAction) = 0;
    //};
    [System.Runtime.InteropServices.Guid("D3EE12AD-3865-4362-9746-B75A682DF0E6")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    [System.Runtime.InteropServices.ComImport]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Uses a non-traditional method of async")]
    internal interface IAccountsSettingsPaneInterop
    {
        // Note: Invoking methods on ComInterfaceType.InterfaceIsIInspectable interfaces
        // no longer appears supported in the runtime (probably with removal of WinRT support),
        // so simulate with IUnknown.
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out WinRT.TrustLevel trustLevel);

        [STAThread]
        void GetForWindow(IntPtr appWindow, ref Guid riid, out IntPtr result);

        void ShowAddAccountForWindowAsync(IntPtr appWindow, ref Guid riid, out IntPtr result);
    }

    [Guid("81EA942C-4F09-4406-A538-838D9B14B7E6")]
    internal interface IAccountsSettingPane
    {

    }

    //Helper to initialize AccountsSettingsPane
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal static class AccountsSettingsPaneInterop
    {
        [STAThread]
        public static AccountsSettingsPane GetForWindow(IntPtr hWnd)
        {
            IAccountsSettingsPaneInterop accountsSettingsPaneInterop =
                AccountsSettingsPane.As<IAccountsSettingsPaneInterop>();
            //Guid guid = typeof(AccountsSettingsPane).GUID;
            Guid guid = //WinRT.GuidGenerator.CreateIID(typeof(IAccountsSettingPane));
                Guid.Parse("81EA942C-4F09-4406-A538-838D9B14B7E6");

            //IAccountsSettingsPaneInterop accountsSettingsPaneInterop = 
            //    (IAccountsSettingsPaneInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(AccountsSettingsPane));
            //Guid guid = typeof(AccountsSettingsPane).GetInterface("IAccountsSettingsPane").GUID;

            accountsSettingsPaneInterop.GetForWindow(hWnd, ref guid, out IntPtr result);
            return MarshalInterface<AccountsSettingsPane>.FromAbi(result);
        }

        public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr hWnd)
        {
            IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop =
                WebAuthenticationCoreManager.As<IWebAuthenticationCoreManagerInterop>();

            IAccountsSettingsPaneInterop accountsSettingsPaneInterop =
                AccountsSettingsPane.As<IAccountsSettingsPaneInterop>();
            //Guid guid = typeof(IAsyncAction).GUID;
            Guid guid = WinRT.GuidGenerator.CreateIID(typeof(IAsyncAction));

            //IAccountsSettingsPaneInterop accountsSettingsPaneInterop = 
            //    (IAccountsSettingsPaneInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(AccountsSettingsPane));
            //Guid guid = typeof(IAsyncAction).GUID;

            accountsSettingsPaneInterop.ShowAddAccountForWindowAsync(hWnd, ref guid, out IntPtr result);

            return MarshalInterface<IAsyncAction>.FromAbi(result);
        }
    }
}
