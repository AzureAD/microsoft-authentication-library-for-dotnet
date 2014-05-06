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

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class PlatformSpecificHelper
    {
        public static string GetProductName()
        {
            return ".NET";
        }

        public static string GetEnvironmentVariable(string variable)
        {
            string value = Environment.GetEnvironmentVariable(variable);
            return !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        public static AuthenticationResult ProcessServiceError(string error, string errorDescription)
        {
            throw new AdalServiceException(error, errorDescription);
        }

        public static string PlatformSpecificToLower(this string input)
        {
            return input.ToLower(CultureInfo.InvariantCulture);
        }

        internal static string CreateSha256Hash(string input)
        {
            SHA256 sha256 = SHA256Managed.Create();
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] inputBytes = encoding.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }
    }
}
