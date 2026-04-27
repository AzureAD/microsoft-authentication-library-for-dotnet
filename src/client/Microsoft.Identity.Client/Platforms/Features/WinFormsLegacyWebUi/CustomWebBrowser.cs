// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal partial class CustomWebBrowser : WebBrowser
    {
#pragma warning disable IDE1006 // Windows API HRESULT constants use conventional S_OK/S_FALSE naming
        private const int S_OK = 0;
        private const int S_FALSE = 1;
#pragma warning restore IDE1006
        private const int WM_CHAR = 0x102;
        private static readonly HashSet<Shortcut> s_shortcutDisallowedList = [];
        private CustomWebBrowserEvent _webBrowserEvent;
        private AxHost.ConnectionPointCookie _webBrowserEventCookie;

        static CustomWebBrowser()
        {
            s_shortcutDisallowedList.Add(Shortcut.AltBksp);
            s_shortcutDisallowedList.Add(Shortcut.AltDownArrow);
            s_shortcutDisallowedList.Add(Shortcut.AltLeftArrow);
            s_shortcutDisallowedList.Add(Shortcut.AltRightArrow);
            s_shortcutDisallowedList.Add(Shortcut.AltUpArrow);
        }

        protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
        {
            return new CustomSite(this);
        }

        protected override void CreateSink()
        {
            base.CreateSink();

            object activeXInstance = ActiveXInstance;
            if (activeXInstance != null)
            {
                _webBrowserEvent = new CustomWebBrowserEvent(this);
                _webBrowserEventCookie = new AxHost.ConnectionPointCookie(activeXInstance, _webBrowserEvent,
                    typeof(NativeWrapper.DWebBrowserEvents2));
            }
        }

        protected override void DetachSink()
        {
            if (_webBrowserEventCookie != null)
            {
                _webBrowserEventCookie.Disconnect();
                _webBrowserEventCookie = null;
            }

            base.DetachSink();
        }

#pragma warning disable 618 // WebBrowserNavigateErrorEventArgs is marked obsolete
        protected virtual void OnNavigateError(WebBrowserNavigateErrorEventArgs e)
        {
            NavigateError?.Invoke(this, e);
        }
#pragma warning restore 618

        protected virtual void OnBeforeNavigate(WebBrowserBeforeNavigateEventArgs e)
        {
            BeforeNavigate?.Invoke(this, e);
        }

        public event WebBrowserNavigateErrorEventHandler NavigateError;

        public event WebBrowserBeforeNavigateEventHandler BeforeNavigate;

        [ComVisible(true), ComDefaultInterface(typeof(NativeWrapper.IDocHostUIHandler))]
        protected class CustomSite(WebBrowser host) : WebBrowserSite(host), NativeWrapper.IDocHostUIHandler, ICustomQueryInterface
        {
            private const int NotImplemented = -2147467263;
            private readonly WebBrowser _host = host;

            #region ICustomQueryInterface Members

            public CustomQueryInterfaceResult GetInterface(ref Guid iid, out IntPtr ppv)
            {
                ppv = IntPtr.Zero;
                if (iid == typeof(NativeWrapper.IDocHostUIHandler).GUID)
                {
                    ppv = Marshal.GetComInterfaceForObject(this, typeof(NativeWrapper.IDocHostUIHandler),
                        CustomQueryInterfaceMode.Ignore);
                    return CustomQueryInterfaceResult.Handled;
                }
                return CustomQueryInterfaceResult.NotHandled;
            }

            #endregion

            public int EnableModeless(bool fEnable)
            {
                return NotImplemented;
            }

            public int FilterDataObject(System.Runtime.InteropServices.ComTypes.IDataObject pDO,
                out System.Runtime.InteropServices.ComTypes.IDataObject ppDORet)
            {
                ppDORet = null;
                return S_FALSE;
            }

            public int GetDropTarget(NativeWrapper.IOleDropTarget pDropTarget,
                out NativeWrapper.IOleDropTarget ppDropTarget)
            {
                ppDropTarget = null;
                return S_OK;
            }

            public int GetExternal(out object ppDispatch)
            {
                ppDispatch = _host.ObjectForScripting;
                return S_OK;
            }

            public int GetHostInfo(NativeWrapper.DOCHOSTUIINFO info)
            {
                const int DOCHOSTUIFLAG_ENABLE_REDIRECT_NOTIFICATION = 0x4000000;
                const int DOCHOSTUIFLAG_NO3DOUTERBORDER = 0x0020000;
                const int DOCHOSTUIFLAG_DISABLE_SCRIPT_INACTIVE = 0x00000010;
                const int DOCHOSTUIFLAG_NOTHEME = 0x00080000;
                const int DOCHOSTUIFLAG_SCROLL_NO = 0x00000008;
                const int DOCHOSTUIFLAG_FLAT_SCROLLBAR = 0x00000080;
                const int DOCHOSTUIFLAG_THEME = 0x00040000;
                const int DOCHOSTUIFLAG_DPI_AWARE = 0x40000000;

                info.dwDoubleClick = 0;
                info.dwFlags = DOCHOSTUIFLAG_NO3DOUTERBORDER | DOCHOSTUIFLAG_DISABLE_SCRIPT_INACTIVE;

                if (WindowsDpiHelper.IsProcessDPIAware())
                {
                    info.dwFlags |= DOCHOSTUIFLAG_DPI_AWARE;
                }

                if (_host.ScrollBarsEnabled)
                {
                    info.dwFlags |= DOCHOSTUIFLAG_FLAT_SCROLLBAR;
                }
                else
                {
                    info.dwFlags |= DOCHOSTUIFLAG_SCROLL_NO;
                }
                if (Application.RenderWithVisualStyles)
                {
                    info.dwFlags |= DOCHOSTUIFLAG_THEME;
                }
                else
                {
                    info.dwFlags |= DOCHOSTUIFLAG_NOTHEME;
                }

                info.dwFlags |= DOCHOSTUIFLAG_ENABLE_REDIRECT_NOTIFICATION;
                return S_OK;
            }

            public int GetOptionKeyPath(string[] pbstrKey, int dw)
            {
                return NotImplemented;
            }

            public int HideUI()
            {
                return NotImplemented;
            }

            public int OnDocWindowActivate(bool fActivate)
            {
                return NotImplemented;
            }

            public int OnFrameWindowActivate(bool fActivate)
            {
                return NotImplemented;
            }

            public int ResizeBorder(NativeWrapper.COMRECT rect, NativeWrapper.IOleInPlaceUIWindow doc, bool fFrameWindow)
            {
                return NotImplemented;
            }

            public int ShowContextMenu(int dwID, NativeWrapper.POINT pt, object pcmdtReserved, object pdispReserved)
            {
                return dwID switch
                {
                    // https://learn.microsoft.com/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa753264(v=vs.85)
                    // this is edit CONTEXT_MENU_CONTROL
                    0x2 or 0x4 or 0x9 or 0x10 => S_FALSE,// allow to show menu; Host did not display its UI. MSHTML will display its UI.
                    _ => S_OK,
                };
            }

            public int ShowUI(int dwID, NativeWrapper.IOleInPlaceActiveObject activeObject,
                NativeWrapper.IOleCommandTarget commandTarget, NativeWrapper.IOleInPlaceFrame frame,
                NativeWrapper.IOleInPlaceUIWindow doc)
            {
                return S_FALSE;
            }

            public int TranslateAccelerator(ref NativeWrapper.MSG msg, ref Guid group, int nCmdID)
            {
                if (msg.message != WM_CHAR)
                {
                    if (ModifierKeys == Keys.Shift || ModifierKeys == Keys.Alt || ModifierKeys == Keys.Control)
                    {
                        int num = ((int)msg.wParam) | (int)ModifierKeys;
                        Shortcut s = (Shortcut)num;
                        if (s_shortcutDisallowedList.Contains(s))
                        {
                            return S_OK;
                        }
                    }
                }

                return S_FALSE;
            }

            public int TranslateUrl(int dwTranslate, string strUrlIn, out string pstrUrlOut)
            {
                pstrUrlOut = null;
                return S_FALSE;
            }

            public int UpdateUI()
            {
                return NotImplemented;
            }
        }
    }

#pragma warning disable 618 // WebBrowserNavigateErrorEventArgs is marked obsolete
    /// <summary>
    /// </summary>
    internal delegate void WebBrowserNavigateErrorEventHandler(object sender, WebBrowserNavigateErrorEventArgs e);
#pragma warning restore 618
    /// <summary>
    /// </summary>
    internal delegate void WebBrowserBeforeNavigateEventHandler(object sender, WebBrowserBeforeNavigateEventArgs e);
}
