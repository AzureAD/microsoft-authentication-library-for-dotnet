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

            if (sts.State != StsState.Started)
            {
                sts.Start();
            }

            return sts;
        }
    }
}
