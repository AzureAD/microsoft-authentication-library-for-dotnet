//------------------------------------------------------------------------------
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
using System.Collections.Generic;

namespace Test.ADAL.Common
{
    public abstract class AdalTestsBase
    {
        static AdalTestsBase()
        {
            StsDictionary = new Dictionary<StsType, Sts>();
        }

        public static Dictionary<StsType, Sts> StsDictionary { get; private set; }

        protected Sts Sts { get; set; }

#if !TEST_ADAL_WINRT_UNIT
        public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }

        protected StsType GetStsTypeFromContext()
        {
            return GetStsType((string)TestContext.DataRow["StsType"]);
        }
#endif

        protected static StsType GetStsType(string stsType)
        {
            return (StsType)Enum.Parse(typeof(StsType), stsType);
        }

        protected static Sts SetupStsService(StsType stsType)
        {
            Sts sts;

            if (!StsDictionary.TryGetValue(stsType, out sts))
            {
                sts = StsFactory.CreateSts(stsType);
                StsDictionary.Add(stsType, sts);
            }

            return sts;
        }
    }
}
