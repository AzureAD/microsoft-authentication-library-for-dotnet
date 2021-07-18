// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi
{
    internal partial class CustomWebBrowser
    {
        [ClassInterface(ClassInterfaceType.None)]
        private sealed class CustomWebBrowserEvent : StandardOleMarshalObject, NativeWrapper.DWebBrowserEvents2
        {
            // Fields
            private readonly CustomWebBrowser parent;
            // Methods
            public CustomWebBrowserEvent(CustomWebBrowser parent)
            {
                this.parent = parent;
            }

            public void NavigateError(object pDisp, ref object url, ref object frame, ref object statusCode,
                ref bool cancel)
            {
                string uriString = (url == null) ? "" : ((string)url);
                string frameString = (frame == null) ? "" : ((string)frame);
                int statusCodeInt = (statusCode == null) ? 0 : ((int)statusCode);

#pragma warning disable 618 // WebBrowserNavigateErrorEventArgs is marked obsolete
                WebBrowserNavigateErrorEventArgs e = new WebBrowserNavigateErrorEventArgs(uriString, frameString,
                    statusCodeInt, pDisp);
#pragma warning restore 618
                parent.OnNavigateError(e);
                cancel = e.Cancel;
            }

            public void BeforeNavigate2(object pDisp, ref object url, ref object flags, ref object targetFrameName,
                ref object postData, ref object headers, ref bool cancel)
            {
                string urlString = (url == null) ? string.Empty : ((string)url);
                int flagsInt = (flags == null) ? 0 : ((int)flags);
                string targetFrameNameString = (targetFrameName == null) ? string.Empty : ((string)targetFrameName);
                byte[] postDataBytes = (byte[])postData;
                string headersString = (headers == null) ? string.Empty : ((string)headers);

                WebBrowserBeforeNavigateEventArgs e = new WebBrowserBeforeNavigateEventArgs(urlString, postDataBytes,
                    headersString, flagsInt, targetFrameNameString, pDisp);
                parent.OnBeforeNavigate(e);
                cancel = e.Cancel;
            }

            public void ClientToHostWindow(ref long cX, ref long cY)
            {
            }

            public void CommandStateChange(long command, bool enable)
            {
            }

            public void DocumentComplete(object pDisp, ref object urlObject)
            {
            }

            public void DownloadBegin()
            {
            }

            public void DownloadComplete()
            {
            }

            public void FileDownload(ref bool cancel)
            {
            }

            public void NavigateComplete2(object pDisp, ref object urlObject)
            {
            }

            public void NewWindow2(ref object ppDisp, ref bool cancel)
            {
            }

            public void OnFullScreen(bool fullScreen)
            {
            }

            public void OnMenuBar(bool menuBar)
            {
            }

            public void OnQuit()
            {
            }

            public void OnStatusBar(bool statusBar)
            {
            }

            public void OnTheaterMode(bool theaterMode)
            {
            }

            public void OnToolBar(bool toolBar)
            {
            }

            public void OnVisible(bool visible)
            {
            }

            public void PrintTemplateInstantiation(object pDisp)
            {
            }

            public void PrintTemplateTeardown(object pDisp)
            {
            }

            public void PrivacyImpactedStateChange(bool bImpacted)
            {
            }

            public void ProgressChange(int progress, int progressMax)
            {
            }

            public void PropertyChange(string szProperty)
            {
            }

            public void SetSecureLockIcon(int secureLockIcon)
            {
            }

            public void StatusTextChange(string text)
            {
            }

            public void TitleChange(string text)
            {
            }

            public void UpdatePageStatus(object pDisp, ref object nPage, ref object fDone)
            {
            }

            public void WindowClosing(bool isChildWindow, ref bool cancel)
            {
            }

            public void WindowSetHeight(int height)
            {
            }

            public void WindowSetLeft(int left)
            {
            }

            public void WindowSetResizable(bool resizable)
            {
            }

            public void WindowSetTop(int top)
            {
            }

            public void WindowSetWidth(int width)
            {
            }
        }
    }
}
