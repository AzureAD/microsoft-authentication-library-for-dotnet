using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class ErrorCodesTest
    {
        // Arrange
        Type[] msalTypesWithConstants = new[]
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
            var msalErrorCodes = msalTypesWithConstants.SelectMany(GetConstants).ToList();
            var coreErrorCodes = GetConstants(typeof(CoreErrorCodes));

            // Assert
            foreach (string coreErrorCode in coreErrorCodes)
            {
                Assert.IsTrue(
                    msalErrorCodes.Contains(coreErrorCode, StringComparer.InvariantCulture), 
                    "Could not find a core error code in msal error codes: " + coreErrorCode);
            }
        }

        [TestMethod]
        public void ErrorCodeClassesArePublic()
        {
            foreach (Type t in msalTypesWithConstants)
            {
                Assert.IsTrue(t.IsPublic);
            }
        }

        private IEnumerable<string> GetConstants(Type type)
        {
            FieldInfo[] fieldInfos = type.GetFields(
                BindingFlags.Public |
                BindingFlags.Static | 
                BindingFlags.DeclaredOnly |
                BindingFlags.FlattenHierarchy);

           return fieldInfos
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
                .Select(t => type.GetField(t.Name).GetValue(null).ToString())
                .ToList();
        }
    }

}
