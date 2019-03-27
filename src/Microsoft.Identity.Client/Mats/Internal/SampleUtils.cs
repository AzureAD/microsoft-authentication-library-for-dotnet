// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class SampleUtils
    {
        public static bool ShouldEnableDevice(string uuid)
        {
            if (GetFirstCharValue(uuid, out short firstChar))
            {
                // DPTI is the result of a hash. A hash function by definition should have uniformity, that is, every hash value in the output range should be generated with roughly the same probability.
                // Because of this, we can assume that the probability of occurrence of any specific character should be uniformly distributed, and if this is the case, then within each character, every
                // hexadecimal digit has a 1/16 (6.25%) chance of showing.
                // So, in theory, if we take the first character of a DPTI, it has a 6.25% probability of being a 1, 6.25% probability of being a 2 and so on.
                // If we want to enable 25% of the devices, we can just take that first character, and if its one of 4 out of 16 possible values (from 0 to F), then we would end up with 25% of the devices only.
                // What we'll do, is check if this digit is in the range of 0-3 (4 different possible values) and if it is indeed, we'll upload it, if not we won't.
                return firstChar < 4;
            }
            else
            {
                return true;
            }
        }

        private static bool GetFirstCharValue(string uuid, out short outValue)
        {
            outValue = 0;

            if (string.IsNullOrEmpty(uuid))
            {
                // DPTI is empty
                // MatsShared::MatsPrivateImpl::ReportError("DPTI is empty", MatsShared::ErrorType::OTHER, MatsShared::ErrorSeverity::LIBRARY_ERROR);
                return false;
            }

            char firstChar = uuid[0];

            if (char.IsLetterOrDigit(firstChar))
            {
                outValue = Convert.ToInt16(char.IsDigit(firstChar) ? firstChar - '0' : 10 + (firstChar - 'a'));
                return true;
            }

            // Character is not a hex value
            //MatsShared::MatsPrivateImpl::ReportError("Character is not a hex value", MatsShared::ErrorType::OTHER, MatsShared::ErrorSeverity::LIBRARY_ERROR);
            return false;
        }
    }
}
