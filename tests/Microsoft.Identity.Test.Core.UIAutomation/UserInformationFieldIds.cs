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

using Microsoft.Identity.Test.LabInfrastructure;
using System;

namespace Microsoft.Identity.Test.Core.UIAutomation
{
    public class UserInformationFieldIds
    {
        private readonly LabUser _user;
        private string _passwordInputId;
        private string _signInButtonId;

        public UserInformationFieldIds(LabUser user)
        {
            _user = user;
        }

        public string PasswordInputId
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_passwordInputId))
                {
                    DetermineFieldIds();
                }
                return _passwordInputId;
            }
        }

        public string SignInButtonId
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_signInButtonId))
                {
                    DetermineFieldIds();
                }
                return _signInButtonId;
            }
        }

        private void DetermineFieldIds()
        {
            if (_user.IsFederated)
            {
                // We use the same IDs for ADFSv3 and ADFSv4
                _passwordInputId = CoreUiTestConstants.AdfsV4WebPasswordID;
                _singInButtonId = CoreUiTestConstants.AdfsV4WebSubmitID;
                return;
            }

            if (_user.UserType == UserType.B2C)
            {
                DetermineB2CFieldIds();
                return;
            }

            _passwordInputId = CoreUiTestConstants.WebPasswordID;
            _singInButtonId = CoreUiTestConstants.WebSubmitID;
        }

        private void DetermineB2CFieldIds()
        {
            if (_user.B2CIdentityProvider == B2CIdentityProvider.Local)
            {
                _passwordInputId = CoreUiTestConstants.B2CWebPasswordID;
                _singInButtonId = CoreUiTestConstants.B2CWebSubmitID;
            }

            if (_user.B2CIdentityProvider == B2CIdentityProvider.Facebook)
            {
                _passwordInputId = CoreUiTestConstants.B2CWebPasswordFacebookID;
                _passwordInputId = CoreUiTestConstants.B2CFacebookSubmitID;
            }

            if (_user.B2CIdentityProvider == B2CIdentityProvider.Google)
            {
                _passwordInputId = CoreUiTestConstants.B2CWebPasswordGoogleID;
                _singInButtonId = CoreUiTestConstants.B2CGoogleSignInID;
            }
        }
    }
}
