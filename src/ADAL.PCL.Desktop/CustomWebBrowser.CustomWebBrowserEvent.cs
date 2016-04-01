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

using System.Runtime.InteropServices;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal partial class CustomWebBrowser
    {
        [ClassInterface(ClassInterfaceType.None)]
        private sealed class CustomWebBrowserEvent : StandardOleMarshalObject, NativeWrapper.DWebBrowserEvents2
        {
            // Fields
            private readonly CustomWebBrowser parent;

            // Methods
            public CustomWebBrowserEvent( CustomWebBrowser parent )
            {
                this.parent = parent;
            }

            public void NavigateError(object pDisp, ref object url, ref object frame, ref object statusCode, ref bool cancel)
            {
                string uriString = ( url == null ) ? "" : ( (string)url );
                string frameString = ( frame == null ) ? "" : ( (string)frame );
                int statusCodeInt = ( statusCode == null )? 0 : ( (int)statusCode );

                WebBrowserNavigateErrorEventArgs e = new WebBrowserNavigateErrorEventArgs( uriString, frameString, statusCodeInt, pDisp );
                this.parent.OnNavigateError( e );
                cancel = e.Cancel;
            }

            // This method are empty because we agree with their implementation in base class event handler System.Windows.Forms.WebBrowser+WebBrowserEvent.
            // We disagree with empty implementation of NavigateError.
            // We also could tweak implementation of base class if we disagree by implementing this method.
            // This is COM events handler, defined in COM interface, however this model works as Events in .NET
            // Multiple handlers are possible, so empty method just called and do nothing.
            public void BeforeNavigate2( object pDisp, ref object urlObject, ref object flags, ref object targetFrameName, ref object postData, ref object headers, ref bool cancel )
            {
                // TODO: Navigating event from public class could be called for internal object.
                //       Current implementation of System.Windows.Forms.WebBrowser doesn't allow you to track who issues this event this control or IFrame,
                //       internal IFrame will have different pDisp, so we need filter events from internal IFrames by analyzing this field:
                //       
                //       if ( this.webBrowser.ActiveXInstance != e.WebBrowserActiveXInstance )
                //       {
                //           // this event came from internal frame, ignore this.
                //           return;
                //       }
                //      
                //       See WindowsFormsWebAuthenticationDialogBase.WebBrowserNavigateErrorHandler( object sender, WebBrowserNavigateErrorEventArgs e )
                //       Thus, before making any decision it will be safe to check if Navigating event comes from right object.
                //       This not a P0 bug, as it final URL with auth code could came only in main frame, however it could give issue with more complicated logic.
            }

            public void ClientToHostWindow( ref long cX, ref long cY )
            {
            }

            public void CommandStateChange( long command, bool enable )
            {
            }

            public void DocumentComplete( object pDisp, ref object urlObject )
            {
            }

            public void DownloadBegin()
            {
            }

            public void DownloadComplete()
            {
            }

            public void FileDownload( ref bool cancel )
            {
            }

            public void NavigateComplete2( object pDisp, ref object urlObject )
            {
            }


            public void NewWindow2( ref object ppDisp, ref bool cancel )
            {
            }

            public void OnFullScreen( bool fullScreen )
            {
            }

            public void OnMenuBar( bool menuBar )
            {
            }

            public void OnQuit()
            {
            }

            public void OnStatusBar( bool statusBar )
            {
            }

            public void OnTheaterMode( bool theaterMode )
            {
            }

            public void OnToolBar( bool toolBar )
            {
            }

            public void OnVisible( bool visible )
            {
            }

            public void PrintTemplateInstantiation( object pDisp )
            {
            }

            public void PrintTemplateTeardown( object pDisp )
            {
            }

            public void PrivacyImpactedStateChange( bool bImpacted )
            {
            }

            public void ProgressChange( int progress, int progressMax )
            {
            }

            public void PropertyChange( string szProperty )
            {
            }

            public void SetSecureLockIcon( int secureLockIcon )
            {
            }

            public void StatusTextChange( string text )
            {
            }

            public void TitleChange( string text )
            {
            }

            public void UpdatePageStatus( object pDisp, ref object nPage, ref object fDone )
            {
            }

            public void WindowClosing( bool isChildWindow, ref bool cancel )
            {
            }

            public void WindowSetHeight( int height )
            {
            }

            public void WindowSetLeft( int left )
            {
            }

            public void WindowSetResizable( bool resizable )
            {
            }

            public void WindowSetTop( int top )
            {
            }

            public void WindowSetWidth( int width )
            {
            }
        }
    }
}
