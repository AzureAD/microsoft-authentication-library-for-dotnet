// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

[assembly: InternalsVisibleTo("Microsoft.Identity.Lab.Api" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Unit" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Unit" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Integration.Netfx" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Integration.NetCore" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Performance" + KeyTokens.MSAL)]
