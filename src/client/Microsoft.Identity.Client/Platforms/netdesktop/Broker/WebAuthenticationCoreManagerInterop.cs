using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.ApplicationSettings;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    internal static class WebAuthenticationCoreManagerInterop
    {
        public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr hWnd, WebTokenRequest request)
        {
            IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop = (IWebAuthenticationCoreManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(WebAuthenticationCoreManager));
            //Guid guid = typeof(WebAuthenticationCoreManager).GetInterface("IWebAuthenticationCoreManager").GUID;
            Guid guid = typeof(IAsyncOperation<WebTokenRequestResult>).GUID;

            return webAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(hWnd, request, ref guid);
        }
        public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr hWnd, WebTokenRequest request, WebAccount webAccount)
        {
            IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop = (IWebAuthenticationCoreManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(WebAuthenticationCoreManager));
            Guid guid = typeof(IAsyncOperation<WebTokenRequestResult>).GUID;

            return webAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(hWnd, request, webAccount, ref guid);

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
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
    internal interface IWebAuthenticationCoreManagerInterop
    {
        IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr appWindow, WebTokenRequest request, [System.Runtime.InteropServices.In] ref Guid riid);
        IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr appWindow, WebTokenRequest request, WebAccount webAccount, [System.Runtime.InteropServices.In] ref Guid riid);
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
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIInspectable)]
    internal interface IAccountsSettingsPaneInterop
    {
        [STAThread]
        AccountsSettingsPane GetForWindow(IntPtr appWindow, [System.Runtime.InteropServices.In] ref Guid riid);
        IAsyncAction ShowManagedAccountsForWindowAsync(IntPtr appWindow, [System.Runtime.InteropServices.In] ref Guid riid);
        IAsyncAction ShowAddAccountForWindowAsync(IntPtr appWindow, [System.Runtime.InteropServices.In] ref Guid riid);
    }

    //Helper to initialize AccountsSettingsPane
    internal static class AccountsSettingsPaneInterop
    {
        [STAThread]
        public static AccountsSettingsPane GetForWindow(IntPtr hWnd)
        {
            IAccountsSettingsPaneInterop accountsSettingsPaneInterop = (IAccountsSettingsPaneInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(AccountsSettingsPane));
            Guid guid = typeof(AccountsSettingsPane).GetInterface("IAccountsSettingsPane").GUID;

            var result = accountsSettingsPaneInterop.GetForWindow(hWnd, ref guid);

            return result;
        }
        public static IAsyncAction ShowManagedAccountsForWindowAsync(IntPtr hWnd)
        {
            IAccountsSettingsPaneInterop accountsSettingsPaneInterop = (IAccountsSettingsPaneInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(AccountsSettingsPane));
            Guid guid = typeof(IAsyncAction).GUID;

            return accountsSettingsPaneInterop.ShowManagedAccountsForWindowAsync(hWnd, ref guid);
        }
        public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr hWnd)
        {
            IAccountsSettingsPaneInterop accountsSettingsPaneInterop = (IAccountsSettingsPaneInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(AccountsSettingsPane));
            Guid guid = typeof(IAsyncAction).GUID;

            return accountsSettingsPaneInterop.ShowAddAccountForWindowAsync(hWnd, ref guid);
        }
    }
}
