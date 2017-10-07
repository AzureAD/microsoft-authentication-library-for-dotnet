//----------------------------------------------------------------------
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

using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class TokenCacheKeyTests
    {
        private const string Authority = "the authority";
        private const string Resource = "my awesome resource";
        private const string ClientId = "09ca2c8f-3e6a-42d0-8299-1438422f4d31";
        private const TokenSubjectType SubjectType = TokenSubjectType.User;
        private const string UniqueId = "27f324f9-4164-4a1e-b778-e13e06606127";
        private const string DisplayableId = "A displayable ID";

        #region Tests

        [TestMethod]
        [Description("Test TokenCacheKey.Equals with different instances but same internal values")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheKey_Equals_SameValuesDifferenceInstancesAreEqual()
        {
            // Setup
            var testSubject1 = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, DisplayableId);
            var testSubject2 = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, DisplayableId);

            // Act
            var result1 = testSubject1.Equals(testSubject2);
            var result2 = testSubject2.Equals(testSubject1);

            // Verify
            Assert.IsTrue(result1, "Expected different instance with same values to be considered equal");
            Assert.IsTrue(result2, "Expected different instance with same values to be considered equal");
        }

        [TestMethod]
        [Description("Test TokenCacheKey.Equals with same instances")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheKey_Equals_SameInstancesAreEqual()
        {
            // Setup
            var testSubject = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, DisplayableId);

            // Act
            var result = testSubject.Equals(testSubject);

            // Verify
            Assert.IsTrue(result, "Expected same instance to be considered equal");
        }

        [TestMethod]
        [Description("Test TokenCacheKey.Equals with different internal values are NOT considered equal")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheKey_Equals_DifferentValuesAreNotEqual()
        {
            // Setup
            var referenceSubject = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, DisplayableId);

            foreach (var other in ChangeEachProperty(referenceSubject, "a different value", TokenSubjectType.Client))
            {
                // Act
                var result1 = referenceSubject.Equals(other);
                var result2 = other.Equals(referenceSubject);

                // Verify
                Assert.IsFalse(result1, "Expected NOT to be equal." + Environment.NewLine +
                                       AsString(referenceSubject) + Environment.NewLine +
                                       "should not equal" + Environment.NewLine +
                                       AsString(other));
                Assert.IsFalse(result2, "Expected NOT to be equal." + Environment.NewLine +
                                       AsString(referenceSubject) + Environment.NewLine +
                                       "should not equal" + Environment.NewLine +
                                       AsString(other));
            }
        }

        [TestMethod]
        [Description("Test TokenCacheKey.Equals with different internal values are NOT considered equal")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheKey_GetHashCode_DifferentValuesAreNotEqual()
        {
            // Setup
            var referenceSubject = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, DisplayableId);

            var baseCode = referenceSubject.GetHashCode();

            foreach (var other in ChangeEachProperty(referenceSubject, "a different value", TokenSubjectType.Client))
            {
                // Act
                var otherCode = other.GetHashCode();

                // Verify
                Assert.AreNotEqual(baseCode, other, "Expected hash codes NOT to be equal.");
            }
        }


        [TestMethod]
        [Description("Test TokenCacheKey.GetHashCode is not culture dependent")]
        [TestCategory("AdalDotNetUnit")]
        public void TokenCacheKey_GetHashCode_IsNotCurrentCultureDependent()
        {
            var initialCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // Setup
                var testSubject = new TokenCacheKey(Authority, Resource, ClientId, SubjectType, UniqueId, "a displayable ID with four types of letter iIİı");

                // Act: en-US as current culture
                Thread.CurrentThread.CurrentCulture = GetEnglishUsCulture();
                var enUsHashCode = testSubject.GetHashCode();

                // Act: tr-TK as current culture
                Thread.CurrentThread.CurrentCulture = GetTurkishCulture();
                var trTkHashCode = testSubject.GetHashCode();

                // Verify
                Assert.AreEqual(enUsHashCode, trTkHashCode, "Expected hash codes to be the same regardless of current thread culture");
            }
            catch (CultureNotFoundException ex)
            {
                Assert.Inconclusive($"The culture {ex.InvalidCultureId} is not available.");
            }
            finally
            {
                // To ensure isolation with other test cases in this assembly,
                // ensure the test exits with the same culture it started with.
                Thread.CurrentThread.CurrentCulture = initialCulture;
            }
        }

        #endregion

        #region Helpers

        private static CultureInfo GetEnglishUsCulture()
        {
            try
            {
                // Try "English (United States)" first
                return new CultureInfo("en-US");
            }
            catch (CultureNotFoundException)
            {
                // If not found, try the more general "English"
                return new CultureInfo("en");
            }
        }

        private static CultureInfo GetTurkishCulture()
        {
            try
            {
                // Try "Turkish (Turkey)" first
                return new CultureInfo("tr-TK");
            }
            catch (CultureNotFoundException)
            {
                // If not found, try the more general "Turkish"
                return new CultureInfo("tr");
            }
        }

        private static string AsString(TokenCacheKey key)
        {
            return $"({key.Authority}, {key.Resource}, {key.ClientId}, {key.TokenSubjectType}, {key.UniqueId}, {key.DisplayableId})";
        }

        /// <summary>
        /// Generate a series of <see cref="TokenCacheKey"/> instances based on the given <paramref name="reference"/>
        /// instance, but change one property in each instance.
        /// </summary>
        private static IEnumerable<TokenCacheKey> ChangeEachProperty(TokenCacheKey reference, string differentString, TokenSubjectType differentSubjectType)
        {
            // Different authority
            yield return new TokenCacheKey(differentString, reference.Resource, reference.ClientId, reference.TokenSubjectType, reference.UniqueId, reference.DisplayableId);

            // Different resource
            yield return new TokenCacheKey(reference.Authority, differentString, reference.ClientId, reference.TokenSubjectType, reference.UniqueId, reference.DisplayableId);

            // Different client ID
            yield return new TokenCacheKey(reference.Authority, reference.Resource, differentString, reference.TokenSubjectType, reference.UniqueId, reference.DisplayableId);

            // Different token subject type
            yield return new TokenCacheKey(reference.Authority, reference.Resource, reference.ClientId, differentSubjectType, reference.UniqueId, reference.DisplayableId);

            // Different unique ID
            yield return new TokenCacheKey(reference.Authority, reference.Resource, reference.ClientId, reference.TokenSubjectType, differentString, reference.DisplayableId);

            // Different displayable ID
            yield return new TokenCacheKey(reference.Authority, reference.Resource, reference.ClientId, reference.TokenSubjectType, reference.UniqueId, differentString);
        }

        #endregion
    }
}
