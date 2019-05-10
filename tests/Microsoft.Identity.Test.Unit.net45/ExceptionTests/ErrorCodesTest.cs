// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Identity.Client;
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
        [Description("MsalError are internal. Msal and Adal should expose equivalent public constants.")]
        public void CheckErrorCodesArePublicMsalConstants()
        {
            // Act
            List<string> msalErrorCodes = msalTypesWithConstants.SelectMany(GetConstants).ToList();
            IEnumerable<string> MsalError = GetConstants(typeof(MsalError));

            // Assert
            bool missingErrorCode = false;
            StringBuilder errorsFound = new StringBuilder();
            foreach (string coreErrorCode in MsalError)
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
