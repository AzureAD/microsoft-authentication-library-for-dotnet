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
