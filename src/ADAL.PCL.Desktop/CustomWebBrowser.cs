//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal partial class CustomWebBrowser : WebBrowser
    {
        private const int S_OK = 0;
        private const int S_FALSE = 1;
        private const int WM_CHAR = 0x102;

        private AxHost.ConnectionPointCookie webBrowserEventCookie;
        private CustomWebBrowserEvent webBrowserEvent;
        private static readonly HashSet<Shortcut> shortcutBlacklist = new HashSet<Shortcut>();

        static CustomWebBrowser()
        {
            shortcutBlacklist.Add(Shortcut.AltBksp);
            shortcutBlacklist.Add(Shortcut.AltDownArrow);
            shortcutBlacklist.Add(Shortcut.AltLeftArrow);
            shortcutBlacklist.Add(Shortcut.AltRightArrow);
            shortcutBlacklist.Add(Shortcut.AltUpArrow);
        }

        [ComVisible(true), ComDefaultInterface(typeof(NativeWrapper.IDocHostUIHandler))]
        protected class CustomSite : WebBrowserSite, NativeWrapper.IDocHostUIHandler, ICustomQueryInterface
        {
            private const int NotImplemented = -2147467263;

            private readonly WebBrowser host;

            public CustomSite(WebBrowser host)
                : base(host)
            {
                this.host = host;
            }


            public int EnableModeless(bool fEnable)
            {
                return NotImplemented;
            }

            public int FilterDataObject(System.Runtime.InteropServices.ComTypes.IDataObject pDO, out System.Runtime.InteropServices.ComTypes.IDataObject ppDORet)
            {
                ppDORet = null;
                return S_FALSE;
            }

            public int GetDropTarget(NativeWrapper.IOleDropTarget pDropTarget, out NativeWrapper.IOleDropTarget ppDropTarget)
            {
                ppDropTarget = null;
                return S_OK;
            }

            public int GetExternal(out object ppDispatch)
            {
                ppDispatch = this.host.ObjectForScripting;
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

                if (NativeWrapper.NativeMethods.IsProcessDPIAware())
                {
                    info.dwFlags |= DOCHOSTUIFLAG_DPI_AWARE;
                }

                if (this.host.ScrollBarsEnabled)
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
                switch (dwID)
                {
                    // http://msdn.microsoft.com/en-us/library/aa753264(v=vs.85).aspx
                    case 0x2: // this is edit CONTEXT_MENU_CONTROL
                    case 0x4: // selected text CONTEXT_MENU_TEXTSELECT
                    case 0x9: // CONTEXT_MENU_VSCROLL
                    case 0x10: //CONTEXT_MENU_HSCROLL
                         return S_FALSE; // allow to show menu; Host did not display its UI. MSHTML will display its UI.
                        
                }
                return S_OK;
            }

            public int ShowUI(int dwID, NativeWrapper.IOleInPlaceActiveObject activeObject, NativeWrapper.IOleCommandTarget commandTarget, NativeWrapper.IOleInPlaceFrame frame, NativeWrapper.IOleInPlaceUIWindow doc)
            {
                return S_FALSE;
            }

            public int TranslateAccelerator(ref NativeWrapper.MSG msg, ref Guid group, int nCmdID)
            {
                if (msg.message != WM_CHAR)
                {
                    if (ModifierKeys == Keys.Shift || ModifierKeys == Keys.Alt || ModifierKeys == Keys.Control)
                    {
                        int num = ((int) msg.wParam) | (int) ModifierKeys;
                        Shortcut s = (Shortcut) num;
                        if (shortcutBlacklist.Contains(s))
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

            #region ICustomQueryInterface Members

            public CustomQueryInterfaceResult GetInterface(ref Guid iid, out IntPtr ppv)
            {
                ppv = IntPtr.Zero;
                if (iid == typeof(NativeWrapper.IDocHostUIHandler).GUID)
                {
                    ppv = Marshal.GetComInterfaceForObject(this, typeof(NativeWrapper.IDocHostUIHandler), CustomQueryInterfaceMode.Ignore);
                    return CustomQueryInterfaceResult.Handled;
                }
                return CustomQueryInterfaceResult.NotHandled;
            }

            #endregion
        }

        protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
        {
            return new CustomSite(this);
        }

        protected override void CreateSink()
        {
            base.CreateSink();

            object activeXInstance = this.ActiveXInstance;
            if (activeXInstance != null)
            {
                this.webBrowserEvent = new CustomWebBrowserEvent(this);
                this.webBrowserEventCookie = new AxHost.ConnectionPointCookie(activeXInstance, this.webBrowserEvent, typeof(NativeWrapper.DWebBrowserEvents2));
            }

        }

        protected override void DetachSink()
        {
            if (this.webBrowserEventCookie != null)
            {
                this.webBrowserEventCookie.Disconnect();
                this.webBrowserEventCookie = null;
            }

            base.DetachSink();
        }

        protected virtual void OnNavigateError(WebBrowserNavigateErrorEventArgs e)
        {
            if (NavigateError != null)
            {
                this.NavigateError(this, e);
            }
        }
        

        public event WebBrowserNavigateErrorEventHandler NavigateError;
    }

    /// <summary>
    /// Delegate to handle navifation errors in the browser control
    /// </summary>
    /// <param name="sender">object type</param>
    /// <param name="e">WebBrowserNavigateErrorEventArgs type</param>
    public delegate void WebBrowserNavigateErrorEventHandler(object sender, WebBrowserNavigateErrorEventArgs e);
}
