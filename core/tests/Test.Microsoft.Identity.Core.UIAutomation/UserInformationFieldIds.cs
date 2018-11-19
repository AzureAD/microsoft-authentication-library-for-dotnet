//------------------------------------------------------------------------------
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

using Test.Microsoft.Identity.LabInfrastructure;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public class UserInformationFieldIds
    {
        public string PasswordInputId { get; set; }
        public string SignInButtonId { get;  set; }

        public void DetermineFieldIds(LabUser user)
        {
            if (user.IsFederated)
            {
                switch (user.FederationProvider)
                {
                    case FederationProvider.AdfsV3:
                    case FederationProvider.AdfsV4:
                        PasswordInputId = CoreUiTestConstants.AdfsV4WebPasswordID;
                        SignInButtonId = CoreUiTestConstants.AdfsV4WebSubmitID;
                        break;
                    default:
                        PasswordInputId = CoreUiTestConstants.WebPasswordID;
                        SignInButtonId = CoreUiTestConstants.WebSubmitID;
                        break;
                }
            }

            if(user.UserType == UserType.B2C)
            {
                DetermineB2CFieldIds(user);
            }
            
            else
            {
                PasswordInputId = CoreUiTestConstants.WebPasswordID;
                SignInButtonId = CoreUiTestConstants.WebSubmitID;
            }
        }

        private void DetermineB2CFieldIds(LabUser user)
        {
            if (user.B2CIdentityProvider == B2CIdentityProvider.Local)
            {
                PasswordInputId = CoreUiTestConstants.B2CWebPasswordID;
                SignInButtonId = CoreUiTestConstants.B2CWebSubmitID;
            }
            if (user.B2CIdentityProvider == B2CIdentityProvider.Facebook)
            {
                PasswordInputId = CoreUiTestConstants.B2CWebPasswordFacebookID;
                SignInButtonId = CoreUiTestConstants.B2CFacebookSubmitID;
            }
            if (user.B2CIdentityProvider == B2CIdentityProvider.Google)
            {
                PasswordInputId = CoreUiTestConstants.B2CWebPasswordGoogleID;
                SignInButtonId = CoreUiTestConstants.B2CGoogleSignInID;
            }
        }
    }
}
