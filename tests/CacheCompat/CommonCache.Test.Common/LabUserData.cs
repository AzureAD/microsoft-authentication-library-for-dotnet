// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CommonCache.Test.Common
{
    public class LabUserData
    {
        public LabUserData()
        {
        }

        public LabUserData(string upn, string password)
        {
            Upn = upn;
            Password = password;
        }

        public string Upn { get; set; }
        public string Password { get; set; }
    }
}
