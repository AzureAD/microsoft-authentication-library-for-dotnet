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

namespace Microsoft.Identity.Test.Core.UIAutomation
{
    public class UserInformationFieldIds
    {
        public string PasswordInputId { get; set; }
        public string SignInButtonId { get; set; }

        public void DetermineFieldIds(LabUser user)
        {
            if (user.IsFederated)
            {
                // We use the same IDs for ADFSv3 and ADFSv4
                PasswordInputId = CoreUiTestConstants.AdfsV4WebPasswordId;
                SignInButtonId = CoreUiTestConstants.AdfsV4WebSubmitId;
                return;
            }

            if (user.UserType == UserType.B2C)
            {
                DetermineB2CFieldIds(user);
                return;
            }

            PasswordInputId = CoreUiTestConstants.WebPasswordId;
            SignInButtonId = CoreUiTestConstants.WebSubmitId;
        }

        private void DetermineB2CFieldIds(LabUser user)
        {
            switch (user.B2CIdentityProvider)
            {
                case B2CIdentityProvider.Local:
                    PasswordInputId = CoreUiTestConstants.B2CWebPasswordId;
                    SignInButtonId = CoreUiTestConstants.B2CWebSubmitId;
                    break;
                case B2CIdentityProvider.Facebook:
                    PasswordInputId = CoreUiTestConstants.B2CWebPasswordFacebookId;
                    SignInButtonId = CoreUiTestConstants.B2CFacebookSubmitId;
                    break;
                case B2CIdentityProvider.Google:
                    PasswordInputId = CoreUiTestConstants.B2CWebPasswordGoogleId;
                    SignInButtonId = CoreUiTestConstants.B2CGoogleSignInId;
                    break;
            }
        }
    }
}
