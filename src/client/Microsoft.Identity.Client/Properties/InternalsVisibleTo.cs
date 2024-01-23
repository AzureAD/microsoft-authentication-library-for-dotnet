// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

[assembly: InternalsVisibleTo("Microsoft.Identity.Client.Desktop" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Client.Broker" + KeyTokens.MSAL)]

[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Unit" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Common" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.SideBySide" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Integration.NetCore" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Integration.NetFx" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Integration.NetStandard" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Performance" + KeyTokens.MSAL)]

[assembly: InternalsVisibleTo("CommonCache.Test.Common" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("CommonCache.Test.Unit" + KeyTokens.MSAL)]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

[assembly: InternalsVisibleTo("Microsoft.Identity.Client.Extensions.Msal" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Test.Unit" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Client.Extensions.Web" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("Microsoft.Identity.Client.Extensions.Web.UnitTests" + KeyTokens.MSAL)]

[assembly: InternalsVisibleTo("XForms" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("WebApi" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("NetFxConsoleApp" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("NetCoreWinFormsWAM" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("DesktopTestApp" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("UWP standalone" + KeyTokens.MSAL)]
[assembly: InternalsVisibleTo("XamarinDev" + KeyTokens.MSAL)]
