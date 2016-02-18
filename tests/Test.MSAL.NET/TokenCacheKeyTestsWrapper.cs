using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Unit;

namespace Test.ADAL.NET
{
    [TestClass]
    public class TokenCacheKeyTestsWrapper
    {
       // [TestMethod]
        public void ConstructorInitCombinations()
        {
            TokenCacheKeyTests tests = new TokenCacheKeyTests();
            tests.ConstructorInitCombinations();
        }

       // [TestMethod]
        public void TestEquals()
        {
            TokenCacheKeyTests tests = new TokenCacheKeyTests();
            tests.TestEquals();
        }
    }
}