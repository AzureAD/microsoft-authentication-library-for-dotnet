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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT
{
    /// <summary>
    /// To indicate what API was called in the test app
    /// Acronyms used:
    /// A: Authority
    /// V: ValidateAuthority
    /// T: Token Cache
    /// R: Resource
    /// C: ClientId
    /// Re: RedirectUri
    /// P: PromptBehavior
    /// U: UserId/UserName
    /// Pa: Password
    /// X: ExtraQueryParameters
    /// </summary>
    enum CommandType
    {
        ClearDefaultTokenCache,
        SetEnvironmentVariable,
        SetCorrelationId,
        ClearUseCorporateNetwork,
        SetUseCorporateNetwork,
        CreateContextA,
        CreateContextAV,
        CreateContextAVT,
        AquireTokenAsyncRC,
        AquireTokenAsyncRCP,
        AquireTokenAsyncRCUPa,
        AquireTokenAsyncRCRe,
        AquireTokenAsyncRCReP,
        AquireTokenAsyncRCRePU,
        AcquireTokenByRefreshTokenAsyncRC,
        AcquireTokenByRefreshTokenAsyncRCRe,
        AquireTokenAsyncRCRePUX,
        CreateFromResourceUrlAsync,
        CreateFromResponseAuthenticateHeader,
    }

    [DataContract]
    class CommandArguments
    {
        [DataMember]
        public string EnvironmentVariable { get; set; }

        [DataMember]
        public string EnvironmentVariableValue { get; set; }

        [DataMember]
        public string Authority { get; set; }

        [DataMember]
        public bool ValidateAuthority { get; set; }

        [DataMember]
        public string Resource { get; set; }

        [DataMember]
        public string ClientId { get; set; }

        [DataMember]
        public Uri RedirectUri { get; set; }

        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string RefreshToken { get; set; }

        [DataMember]
        public string AuthorizationCode { get; set; }

        [DataMember]
        public string ClientSecret { get; set; }

        [DataMember]
        public TokenCacheType TokenCacheType { get; set; }

        [DataMember]
        public PromptBehaviorProxy PromptBehavior { get; set; }

        [DataMember]
        public Guid CorrelationId { get; set; }

        [DataMember]
        public string Extra { get; set; }
    }

    [DataContract]
    class AuthenticationContextCommand
    {
        [DataMember]
        public CommandType CommandType { get; set; }

        [DataMember]
        public CommandArguments Arguments { get; set; }

        public AuthenticationContextCommand(CommandType commandType, CommandArguments arguments)
        {
            this.CommandType = commandType;
            this.Arguments = arguments;
        }
    }

    class CommandProxy
    {
        public CommandProxy()
        {
            this.Commands = new List<AuthenticationContextCommand>();
        }

        public List<AuthenticationContextCommand> Commands { get; set; }

        public void AddCommand(AuthenticationContextCommand command)
        {
            Commands.Add(command);
        }

        internal string Serialize()
        {
            string output = string.Empty;
            var serializer = new DataContractJsonSerializer(typeof(List<AuthenticationContextCommand>));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this.Commands);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    output = reader.ReadToEnd();                    
                }
            }

            return output;
        }

        internal static CommandProxy Deserialize(string obj)
        {
            var output = new CommandProxy();
            var serializer = new DataContractJsonSerializer(typeof(List<AuthenticationContextCommand>));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(obj);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                output.Commands = (List<AuthenticationContextCommand>)serializer.ReadObject(stream);
            }

            return output;
        }
    }
}
