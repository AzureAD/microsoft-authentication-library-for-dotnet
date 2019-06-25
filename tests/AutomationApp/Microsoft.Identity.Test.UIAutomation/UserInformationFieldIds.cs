// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Test.LabInfrastructure;

namespace Microsoft.Identity.Test.UIAutomation
{
    public class UserInformationFieldIds
    {
        private readonly LabUser _user;
        private string _passwordInputId;
        private string _passwordSignInButtonId;

        public UserInformationFieldIds(LabUser user)
        {
            _user = user;
        }

        public string GetPasswordInputId(bool isB2CTest = false)
        {

            if (string.IsNullOrWhiteSpace(_passwordInputId))
            {
                DetermineFieldIds(isB2CTest);
            }
            return _passwordInputId;

        }

        public string GetPasswordSignInButtonId(bool isB2CTest = false)
        {

            if (string.IsNullOrWhiteSpace(_passwordSignInButtonId))
            {
                DetermineFieldIds(isB2CTest);
            }
            return _passwordSignInButtonId;

        }

        /// <summary>
        /// When starting auth, the firt screen, which collects the username, is from AAD.
        /// Subsequent screens can be from ADFS.
        /// </summary>
        public string AADSignInButtonId
        {
            get
            {
                return UITestConstants.WebSubmitId;
            }
        }

        /// <summary>
        /// When starting auth, the firt screen, which collects the username, is from AAD.
        /// Subsequent screens can be from ADFS.
        /// </summary>
        public string AADUsernameInputId
        {
            get
            {
                return UITestConstants.WebUPNInputId;
            }
        }

        private void DetermineFieldIds(bool isB2CTest)
        {
            if (_user.IsFederated)
            {
                if (_user.FederationProvider == FederationProvider.AdfsV2)
                {
                    _passwordInputId = UITestConstants.AdfsV2WebPasswordInputId;
                    _passwordSignInButtonId = UITestConstants.AdfsV2WebSubmitButtonId;
                    return;
                }

                // We use the same IDs for ADFSv3 and ADFSv4
                _passwordInputId = UITestConstants.AdfsV4WebPasswordId;
                _passwordSignInButtonId = UITestConstants.AdfsV4WebSubmitId;
                return;
            }

            if (_user.UserType == UserType.B2C && isB2CTest)
            {
                DetermineB2CFieldIds();
                return;
            }

            _passwordInputId = UITestConstants.WebPasswordId;
            _passwordSignInButtonId = UITestConstants.WebSubmitId;
        }

        private void DetermineB2CFieldIds()
        {
            switch (_user.B2CIdentityProvider)
            {
                case B2CIdentityProvider.Local:
                    _passwordInputId = UITestConstants.B2CWebPasswordId;
                    _passwordSignInButtonId = UITestConstants.B2CWebSubmitId;
                    break;
                case B2CIdentityProvider.Facebook:
                    _passwordInputId = UITestConstants.B2CWebPasswordFacebookId;
                    _passwordSignInButtonId = UITestConstants.B2CFacebookSubmitId;
                    break;
                case B2CIdentityProvider.Google:
                    _passwordInputId = UITestConstants.B2CWebPasswordGoogleId;
                    _passwordSignInButtonId = UITestConstants.B2CGoogleSignInId;
                    break;
            }
        }
    }
}
