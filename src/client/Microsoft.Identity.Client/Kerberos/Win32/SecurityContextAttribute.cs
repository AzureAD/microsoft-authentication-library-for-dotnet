// -----------------------------------------------------------------------
// Licensed to The .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -----------------------------------------------------------------------

namespace Microsoft.Identity.Client.Kerberos.Win32
{
    internal enum SecurityContextAttribute
    {
        SECPKG_ATTR_SIZES = 0,
        SECPKG_ATTR_NAMES = 1,
        SECPKG_ATTR_LIFESPAN = 2,
        SECPKG_ATTR_DCE_INFO = 3,
        SECPKG_ATTR_STREAM_SIZES = 4,
        SECPKG_ATTR_AUTHORITY = 6,
        SECPKG_ATTR_PACKAGE_INFO = 10,
        SECPKG_ATTR_NEGOTIATION_INFO = 12,
        SECPKG_ATTR_UNIQUE_BINDINGS = 25,
        SECPKG_ATTR_ENDPOINT_BINDINGS = 26,
        SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27,
        SECPKG_ATTR_APPLICATION_PROTOCOL = 35
    }
}