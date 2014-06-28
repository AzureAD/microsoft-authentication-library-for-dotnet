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

using System.Windows.Forms;

namespace Test.ADAL.Common
{
    static class HTMLUIExtension
    {
        public static HtmlElement GetFirstOf( this WebBrowser webBrowser, params string[] elementIDs )
        {
            foreach ( string id in elementIDs )
            {
                HtmlElement element = webBrowser.Document.GetElementById( id );
                if ( element != null )
                {
                    return element;
                }
            }

            return null;
        }

        public static void MakeClick( this HtmlElement element )
        {
            mshtml.IHTMLElement el = (mshtml.IHTMLElement)element.DomElement;
            el.click();
        }

        public static void SetValue( this HtmlElement element, string value )
        {
            mshtml.IHTMLInputElement input = (mshtml.IHTMLInputElement)element.DomElement;
            input.value = value;
        }

        public static bool IsVisible( this HtmlElement element )
        {
            mshtml.IHTMLElement el = (mshtml.IHTMLElement)element.DomElement;
            return el.offsetHeight > 0 || el.offsetWidth > 0;
        }
    }
}
