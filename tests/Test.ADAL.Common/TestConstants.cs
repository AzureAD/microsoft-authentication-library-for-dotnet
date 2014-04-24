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
    public static class StringValue
    {
        public const string NotProvided = "NotProvided";
        public const string NotReady = "Not Ready";
        public const string Null = "NULL";
    }

    public static class UserType
    {
        public const string NonFederated = "NonFederated";
        public const string Federated = "Federated";
    }

    public static class CacheType
    {
        public const string Default = "Default";
        public const string Null = "Null";
        public const string Constant = "Constant";
        public const string ShortLived = "ShortLived";
        public const string InMemory = "InMemory";
    }

    public enum ValidateAuthorityIndex
    {
        NotProvided = 0,
        Yes = 1,
        No = 2
    }

    public enum CacheTypeIndex
    {
        NotProvided = 0,
        Default = 1,
        Null = 2,
        ShortLived = 3,
        InMemory = 4
    }
}