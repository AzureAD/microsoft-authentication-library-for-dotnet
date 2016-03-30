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
