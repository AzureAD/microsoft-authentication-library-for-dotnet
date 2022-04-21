// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Performance
{
    [SimpleJob(RuntimeMoniker.Net472)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public class Base64EncodeTests
    {
        const string s1 = "The quick brown fox jumps over the lazy dog";
        const string s2 = "The quick brown fox jumps over the lazy dog==";
        const string s3 = "The quick +brown fox jumps //over the lazy dog=";

        static readonly byte[] d1 = Encoding.UTF8.GetBytes(s1);
        static readonly byte[] d2 = Encoding.UTF8.GetBytes(s2);
        static readonly byte[] d3 = Encoding.UTF8.GetBytes(s3);

        static string s4 = Base64UrlHelpers.Encode(s1);
        static string s5 = Base64UrlHelpers.Encode(s2);
        static string s6 = Base64UrlHelpers.Encode(s3);

        static readonly byte[] d4 = Encoding.UTF8.GetBytes(s4);
        static readonly byte[] d5 = Encoding.UTF8.GetBytes(s5);
        static readonly byte[] d6 = Encoding.UTF8.GetBytes(s6);
        #region Encode

        //[Benchmark]
        //public void Encoding_String_Old()
        //{
        //    OldBase64UrlHelpers.Encode(s1);
        //    OldBase64UrlHelpers.Encode(s2);
        //    OldBase64UrlHelpers.Encode(s3);
        //}

        [Benchmark]
        public void Encode_String_New()
        {
            Base64UrlHelpers.Encode(s1);
            Base64UrlHelpers.Encode(s2);
            Base64UrlHelpers.Encode(s3);
        }

        //[Benchmark]
        //public void Encode_Byte_Old()
        //{
        //    OldBase64UrlHelpers.Encode(d1);
        //    OldBase64UrlHelpers.Encode(d2);
        //    OldBase64UrlHelpers.Encode(d3);
        //}

        [Benchmark]
        public void Encode_Byte_New()
        {
            Base64UrlHelpers.Encode(d1);
            Base64UrlHelpers.Encode(d2);
            Base64UrlHelpers.Encode(d3);
        }
        #endregion

        #region Decode
        //[Benchmark]
        //public void Decode_Bytes_Old()
        //{
        //    OldBase64UrlHelpers.DecodeToBytes(s4);
        //    OldBase64UrlHelpers.DecodeToBytes(s5);
        //    OldBase64UrlHelpers.DecodeToBytes(s6);
        //}

        [Benchmark]
        public void Decode_Bytes_New()
        {
            Base64UrlHelpers.DecodeBytes(s4);
            Base64UrlHelpers.DecodeBytes(s5);
            Base64UrlHelpers.DecodeBytes(s6);
        }

        //[Benchmark]
        //public void Decode_String_Old()
        //{
        //    OldBase64UrlHelpers.DecodeToString(s4);
        //    OldBase64UrlHelpers.DecodeToString(s5);
        //    OldBase64UrlHelpers.DecodeToString(s6);
        //}

        [Benchmark]
        public void Decode_String_New()
        {
            Base64UrlHelpers.Decode(s4);
            Base64UrlHelpers.Decode(s5);
            Base64UrlHelpers.Decode(s6);
        }
        #endregion
    }
}
