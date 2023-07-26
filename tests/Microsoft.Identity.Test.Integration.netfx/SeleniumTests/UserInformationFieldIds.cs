// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;

namespace Microsoft.Identity.Test.Integration.NetFx.SeleniumTests
{
    internal class UserInformationFieldIds
    {
        private readonly LabUser _user;

        private string _passwordInputId;

        private string _passwordSignInButtonId;

        public string AADSignInButtonId => "idSIButton9";

        public string AADUsernameInputId => "i0116";

        public UserInformationFieldIds(LabUser user)
        {
            _user = user;
        }

        public string GetPasswordInputId()
        {
            if (string.IsNullOrWhiteSpace(_passwordInputId))
            {
                DetermineFieldIds();
            }

            return _passwordInputId;
        }

        public string GetPasswordSignInButtonId()
        {
            if (string.IsNullOrWhiteSpace(_passwordSignInButtonId))
            {
                DetermineFieldIds();
            }

            return _passwordSignInButtonId;
        }

        private void DetermineFieldIds()
        {
            if (_user.UserType == UserType.Federated)
            {
                if (_user.FederationProvider == FederationProvider.AdfsV2)
                {
                    _passwordInputId = "ContentPlaceHolder1_PasswordTextBox";
                    _passwordSignInButtonId = "ContentPlaceHolder1_SubmitButton";
                }
                else
                {
                    _passwordInputId = "passwordInput";
                    _passwordSignInButtonId = "submitButton";
                }
            }
            else if (_user.UserType == UserType.B2C)
            {
                DetermineB2CFieldIds();
            }
            else
            {
                _passwordInputId = "i0118";
                _passwordSignInButtonId = "idSIButton9";
            }
        }

        private void DetermineB2CFieldIds()
        {
            switch (_user.B2cProvider)
            {
                case B2CIdentityProvider.Local:
                    _passwordInputId = "password";
                    _passwordSignInButtonId = "next";
                    break;
                case B2CIdentityProvider.Facebook:
                    _passwordInputId = "m_login_password";
                    _passwordSignInButtonId = "u_0_5";
                    break;
                case B2CIdentityProvider.Google:
                    _passwordInputId = "Passwd";
                    _passwordSignInButtonId = "signIn";
                    break;
            }
        }
    }
}
