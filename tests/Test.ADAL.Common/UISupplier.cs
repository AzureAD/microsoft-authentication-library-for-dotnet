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

    public class UISupplier
    {
        static string[] errorObjectIDs = new string[] { "cta_error_message_text", "cta_client_error_text", "errorDetails", "login_no_cookie_error_text", "cannot_locate_resource", "service_exception_message", "errorMessage" };

        static string[] expandLinkIDs = new string[] { "switch_user_link" };
        static string[] usernameIDs = new string[] { "cred_userid_inputtext", "txtBoxMobileEmail", "txtBoxEmail", "userNameInput" };
        static string[] passwordIDs = new string[] { "cred_password_inputtext", "txtBoxMobilePassword", "txtBoxPassword", "passwordInput" };
        static string[] signInIDs = new string[] { "cred_sign_in_button", "btnSignInMobile", "btnSignin", "submitButton" };

        public enum Results
        {
            Continue,
            Error
        };

        public Results SupplyUIStep(WebBrowser webBrowser, string login, string password)
        {
            if (webBrowser.Document == null)
            {
                return Results.Continue;
            }

            foreach (string id in errorObjectIDs)
            {
                HtmlElement error = webBrowser.Document.GetElementById(id);
                if (error != null)
                {
                    if (error.IsVisible())
                    {
                        return Results.Error;
                    }
                }
            }

            HtmlElement link = webBrowser.GetFirstOf(UISupplier.expandLinkIDs);
            if (link != null)
            {
                link.MakeClick();
            }

            HtmlElement pwd = webBrowser.GetFirstOf(UISupplier.passwordIDs);
            HtmlElement uid = webBrowser.GetFirstOf(UISupplier.usernameIDs);
            HtmlElement button = webBrowser.GetFirstOf(UISupplier.signInIDs);

            if (pwd != null && uid != null && button != null)
            {
                if(password != null)
                    pwd.SetValue(password);

                if(login != null)
                    uid.SetValue(login);

                button.MakeClick();
            }

            return Results.Continue;
        }
    }
}
