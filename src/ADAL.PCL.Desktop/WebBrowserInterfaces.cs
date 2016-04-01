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
using System.Drawing;
using System.Runtime.InteropServices;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal class NativeWrapper
    {
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class OLECMD
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cmdID;
            [MarshalAs(UnmanagedType.U4)]
            public int cmdf;
        }

        [ComImport, ComVisible(true), Guid("B722BCCB-4E68-101B-A2BC-00AA00404770"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleCommandTarget
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int QueryStatus(ref Guid pguidCmdGroup, int cCmds, [In, Out] OLECMD prgCmds, [In, Out] IntPtr pCmdText);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int Exec(IntPtr guid, int nCmdID, int nCmdexecopt,
                     [In, MarshalAs(UnmanagedType.LPArray)] object[] pvaIn, IntPtr pvaOut);
        }

        [StructLayout(LayoutKind.Sequential), ComVisible(true)]
        public class DOCHOSTUIINFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize = Marshal.SizeOf(typeof(DOCHOSTUIINFO));
            [MarshalAs(UnmanagedType.I4)]
            public int dwFlags;
            [MarshalAs(UnmanagedType.I4)]
            public int dwDoubleClick;
            [MarshalAs(UnmanagedType.I4)]
            public int dwReserved1;
            [MarshalAs(UnmanagedType.I4)]
            public int dwReserved2;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            public int pt_x;
            public int pt_y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class COMRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public override string ToString()
            {
                return
                    string.Concat(new object[] { "Left = ", this.left, " Top ", this.top, " Right = ", this.right, " Bottom = ", this.bottom });
            }

            public COMRECT()
            {
            }

            public COMRECT(Rectangle r)
            {
                this.left = r.X;
                this.top = r.Y;
                this.right = r.Right;
                this.bottom = r.Bottom;
            }

            public COMRECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public static COMRECT FromXYWH(int x, int y, int width, int height)
            {
                return new COMRECT(x, y, x + width, y + height);
            }
        }

        [ComImport, Guid("00000115-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleInPlaceUIWindow
        {
            IntPtr GetWindow();

            [PreserveSig]
            int ContextSensitiveHelp(int fEnterMode);

            [PreserveSig]
            int GetBorder([Out] COMRECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace([In] COMRECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace([In] COMRECT pborderwidths);

            void SetActiveObject([In, MarshalAs(UnmanagedType.Interface)] IOleInPlaceActiveObject pActiveObject,
                                 [In, MarshalAs(UnmanagedType.LPWStr)] string pszObjName);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000117-0000-0000-C000-000000000046")]
        public interface IOleInPlaceActiveObject
        {
            [PreserveSig]
            int GetWindow(out IntPtr hwnd);

            void ContextSensitiveHelp(int fEnterMode);

            [PreserveSig]
            int TranslateAccelerator([In] ref MSG lpmsg);

            void OnFrameWindowActivate(bool fActivate);
            void OnDocWindowActivate(int fActivate);
            void ResizeBorder([In] COMRECT prcBorder, [In] IOleInPlaceUIWindow pUIWindow, bool fFrameWindow);
            void EnableModeless(int fEnable);
        }

        [StructLayout(LayoutKind.Sequential)]
        public sealed class tagOleMenuGroupWidths
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public int[] widths = new int[6];
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000116-0000-0000-C000-000000000046")]
        public interface IOleInPlaceFrame
        {
            IntPtr GetWindow();

            [PreserveSig]
            int ContextSensitiveHelp(int fEnterMode);

            [PreserveSig]
            int GetBorder([Out] COMRECT lprectBorder);

            [PreserveSig]
            int RequestBorderSpace([In] COMRECT pborderwidths);

            [PreserveSig]
            int SetBorderSpace([In] COMRECT pborderwidths);

            [PreserveSig]
            int SetActiveObject([In, MarshalAs(UnmanagedType.Interface)] IOleInPlaceActiveObject pActiveObject,
                                [In, MarshalAs(UnmanagedType.LPWStr)] string pszObjName);

            [PreserveSig]
            int InsertMenus([In] IntPtr hmenuShared, [In, Out] tagOleMenuGroupWidths lpMenuWidths);

            [PreserveSig]
            int SetMenu([In] IntPtr hmenuShared, [In] IntPtr holemenu, [In] IntPtr hwndActiveObject);

            [PreserveSig]
            int RemoveMenus([In] IntPtr hmenuShared);

            [PreserveSig]
            int SetStatusText([In, MarshalAs(UnmanagedType.LPWStr)] string pszStatusText);

            [PreserveSig]
            int EnableModeless(bool fEnable);

            [PreserveSig]
            int TranslateAccelerator([In] ref MSG lpmsg, [In, MarshalAs(UnmanagedType.U2)] short wID);
        }

        [ComImport, Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleDropTarget
        {
            [PreserveSig]
            int OleDragEnter([In, MarshalAs(UnmanagedType.Interface)] object pDataObj,
                             [In, MarshalAs(UnmanagedType.U4)] int grfKeyState, [In] POINT pt,
                             [In, Out] ref int pdwEffect);

            [PreserveSig]
            int OleDragOver([In, MarshalAs(UnmanagedType.U4)] int grfKeyState, [In] POINT pt,
                            [In, Out] ref int pdwEffect);

            [PreserveSig]
            int OleDragLeave();

            [PreserveSig]
            int OleDrop([In, MarshalAs(UnmanagedType.Interface)] object pDataObj,
                        [In, MarshalAs(UnmanagedType.U4)] int grfKeyState, [In] POINT pt, [In, Out] ref int pdwEffect);
        }

        [ComImport, ComVisible(true), InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         Guid("BD3F23C0-D43E-11CF-893B-00AA00BDCE1A")]
        public interface IDocHostUIHandler
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ShowContextMenu([In, MarshalAs(UnmanagedType.U4)] int dwID, [In] POINT pt,
                                [In, MarshalAs(UnmanagedType.Interface)] object pcmdtReserved,
                                [In, MarshalAs(UnmanagedType.Interface)] object pdispReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetHostInfo([In, Out] DOCHOSTUIINFO info);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ShowUI([In, MarshalAs(UnmanagedType.I4)] int dwID, [In] IOleInPlaceActiveObject activeObject,
                       [In] IOleCommandTarget commandTarget, [In] IOleInPlaceFrame frame, [In] IOleInPlaceUIWindow doc);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int HideUI();

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int UpdateUI();

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int EnableModeless([In, MarshalAs(UnmanagedType.Bool)] bool fEnable);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int OnDocWindowActivate([In, MarshalAs(UnmanagedType.Bool)] bool fActivate);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int OnFrameWindowActivate([In, MarshalAs(UnmanagedType.Bool)] bool fActivate);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ResizeBorder([In] COMRECT rect, [In] IOleInPlaceUIWindow doc, bool fFrameWindow);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int TranslateAccelerator([In] ref MSG msg, [In] ref Guid group, [In, MarshalAs(UnmanagedType.I4)] int nCmdID);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetOptionKeyPath([Out, MarshalAs(UnmanagedType.LPArray)] string[] pbstrKey,
                                 [In, MarshalAs(UnmanagedType.U4)] int dw);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetDropTarget([In, MarshalAs(UnmanagedType.Interface)] IOleDropTarget pDropTarget,
                              [MarshalAs(UnmanagedType.Interface)] out IOleDropTarget ppDropTarget);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetExternal([MarshalAs(UnmanagedType.Interface)] out object ppDispatch);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int TranslateUrl([In, MarshalAs(UnmanagedType.U4)] int dwTranslate,
                             [In, MarshalAs(UnmanagedType.LPWStr)] string strURLIn,
                             [MarshalAs(UnmanagedType.LPWStr)] out string pstrURLOut);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            // Please, don't use System.Windows.Forms.IDataObject it is wrong one.
            int FilterDataObject(System.Runtime.InteropServices.ComTypes.IDataObject pDO, out System.Runtime.InteropServices.ComTypes.IDataObject ppDORet);
        }

        [ComImport]
        [Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IWebBrowser2
        {
            [DispId(0xcb)]
            object Document { [return: MarshalAs(UnmanagedType.IDispatch)] [DispId(0xcb)] get; }

            [DispId(0x227)]
            bool Silent { [param: MarshalAs(UnmanagedType.Bool)] [DispId(0x227)]set; }
        }

        [ComImport, Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D"), TypeLibType(TypeLibTypeFlags.FHidden), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface DWebBrowserEvents2
        {
            [DispId(0x66)]
            void StatusTextChange([In] string text);
            [DispId(0x6c)]
            void ProgressChange([In] int progress, [In] int progressMax);
            [DispId(0x69)]
            void CommandStateChange([In] long command, [In] bool enable);
            [DispId(0x6a)]
            void DownloadBegin();
            [DispId(0x68)]
            void DownloadComplete();
            [DispId(0x71)]
            void TitleChange([In] string text);
            [DispId(0x70)]
            void PropertyChange([In] string szProperty);
            [DispId(250)]
            void BeforeNavigate2([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL, [In] ref object flags, [In] ref object targetFrameName, [In] ref object postData, [In] ref object headers, [In, Out] ref bool cancel);
            [DispId(0xfb)]
            void NewWindow2([In, Out, MarshalAs(UnmanagedType.IDispatch)] ref object pDisp, [In, Out] ref bool cancel);
            [DispId(0xfc)]
            void NavigateComplete2([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL);
            [DispId(0x103)]
            void DocumentComplete([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL);
            [DispId(0xfd)]
            void OnQuit();
            [DispId(0xfe)]
            void OnVisible([In] bool visible);
            [DispId(0xff)]
            void OnToolBar([In] bool toolBar);
            [DispId(0x100)]
            void OnMenuBar([In] bool menuBar);
            [DispId(0x101)]
            void OnStatusBar([In] bool statusBar);
            [DispId(0x102)]
            void OnFullScreen([In] bool fullScreen);
            [DispId(260)]
            void OnTheaterMode([In] bool theaterMode);
            [DispId(0x106)]
            void WindowSetResizable([In] bool resizable);
            [DispId(0x108)]
            void WindowSetLeft([In] int left);
            [DispId(0x109)]
            void WindowSetTop([In] int top);
            [DispId(0x10a)]
            void WindowSetWidth([In] int width);
            [DispId(0x10b)]
            void WindowSetHeight([In] int height);
            [DispId(0x107)]
            void WindowClosing([In] bool isChildWindow, [In, Out] ref bool cancel);
            [DispId(0x10c)]
            void ClientToHostWindow([In, Out] ref long cx, [In, Out] ref long cy);
            [DispId(0x10d)]
            void SetSecureLockIcon([In] int secureLockIcon);
            [DispId(270)]
            void FileDownload([In, Out] ref bool cancel);
            [DispId(0x10f)]
            void NavigateError([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL, [In] ref object frame, [In] ref object statusCode, [In, Out] ref bool cancel);
            [DispId(0xe1)]
            void PrintTemplateInstantiation([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);
            [DispId(0xe2)]
            void PrintTemplateTeardown([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp);
            [DispId(0xe3)]
            void UpdatePageStatus([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object nPage, [In] ref object fDone);
            [DispId(0x110)]
            void PrivacyImpactedStateChange([In] bool bImpacted);
        }

        internal static class NativeMethods
        {
            [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
            internal static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
            internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("Gdi32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
            internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

            [DllImport("User32.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
            internal static extern bool IsProcessDPIAware();
        }
    }
}
