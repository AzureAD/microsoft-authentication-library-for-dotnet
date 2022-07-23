// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AuthScheme.PoP
{
    /// <summary>
    /// Names for Json Web Key Values
    /// </summary>
    internal static class JsonWebKeyParameterNames
    {
        public const string Alg = "alg";
        public const string Crv = "crv";
        public const string D = "d";
        public const string DP = "dp";
        public const string DQ = "dq";
        public const string E = "e";
        public const string K = "k";
        public const string KeyOps = "key_ops";
        public const string Keys = "keys";
        public const string Kid = "kid";
        public const string Kty = "kty";
        public const string N = "n";
        public const string Oth = "oth";
        public const string P = "p";
        public const string Q = "q";
        public const string R = "r";
        public const string T = "t";
        public const string QI = "qi";
        public const string Use = "use";
        public const string X5c = "x5c";
        public const string X5t = "x5t";
        public const string X5tS256 = "x5t#S256";
        public const string X5u = "x5u";
        public const string X = "x";
        public const string Y = "y";

        public const string EC = "EC";
    }

    /// <summary>
    /// Constants for JsonWebAlgorithms  "kty" Key Type (sec 6.1)
    /// http://tools.ietf.org/html/rfc7518#section-6.1
    /// </summary>
    internal static class JsonWebAlgorithmsKeyTypes
    {
#pragma warning disable 1591
        public const string EllipticCurve = "EC";
        public const string RSA = "RSA";
        public const string Octet = "oct";
#pragma warning restore 1591
    }
}
