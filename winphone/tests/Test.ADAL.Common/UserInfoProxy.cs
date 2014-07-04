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

namespace Test.ADAL.Common
{
    using System.Runtime.Serialization;

    [DataContract]
    class UserInfoProxy
    {
        [DataMember]
        public string UserId { get; internal set; }

        [DataMember]
        public bool IsUserIdDisplayable { get; internal set; }

        [DataMember]
        public string GivenName { get; internal set; }

        [DataMember]
        public string FamilyName { get; internal set; }

        [DataMember]
        public string IdentityProvider { get; internal set; }
    }
}
