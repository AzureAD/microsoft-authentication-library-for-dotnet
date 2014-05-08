//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

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
