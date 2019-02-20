// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class ErrorCodesTest
    {
        // Arrange
        private readonly Type[] msalTypesWithConstants = new[]
        {
            typeof(MsalException),
            typeof(MsalClientException),
            typeof(MsalServiceException),
            typeof(MsalUiRequiredException),
            typeof(MsalError)
        };

        [TestMethod]
        [Description("CoreErrorCodes are internal. Msal and Adal should expose equivalent public constants.")]
        public void CheckErrorCodesArePublicMsalConstants()
        {
            // Act
            List<string> msalErrorCodes = msalTypesWithConstants.SelectMany(GetConstants).ToList();
            IEnumerable<string> coreErrorCodes = GetConstants(typeof(CoreErrorCodes));

            // Assert
            bool missingErrorCode = false;
            StringBuilder errorsFound = new StringBuilder();
            foreach (string coreErrorCode in coreErrorCodes)
            {
                var isFound = msalErrorCodes.Contains(coreErrorCode, StringComparer.InvariantCulture);
                if (!isFound)
                {
                    missingErrorCode = true;
                    errorsFound.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "Could not find a core error code in type MsalError: {0}", coreErrorCode));
                }
            }

            Assert.IsFalse(missingErrorCode, errorsFound.ToString());
        }

        [TestMethod]
        public void ErrorCodeClassesArePublic()
        {
            foreach (var t in msalTypesWithConstants)
            {
                Assert.IsTrue(t.IsPublic);
            }
        }

        private IEnumerable<string> GetConstants(Type type)
        {
            FieldInfo[] fieldInfos = type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy);

            return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                             .Select(t => type.GetField(t.Name).GetValue(null).ToString()).ToList();
        }
    }
}