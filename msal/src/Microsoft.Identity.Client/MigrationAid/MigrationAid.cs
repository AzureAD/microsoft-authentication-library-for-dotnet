using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// In MSAL.NET 1.x, was representing a User. From MSAL 2.x use <see cref="IAccount"/> which represents an account
    /// (a user has several accounts). See https://aka.ms/msal-net-2-released for more details.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use IAccount instead (See https://aka.ms/msal-net-2-released)")]
    public interface IUser
    {
        /// <summary>
        /// In MSAL.NET 1.x was the displayable ID of a user. From MSAL 2.x use the <see cref="IAccount.Username"/> of an account.
        /// See https://aka.ms/msal-net-2-released for more details
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Username instead (See https://aka.ms/msal-net-2-released)", true)]
        string DisplayableId { get; }

        /// <summary>
        /// In MSAL.NET 1.x was the name of the user (which was not very useful as the concatenation of 
        /// some claims). From MSAL 2.x rather use <see cref="IAccount.Username"/>. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Username instead (See https://aka.ms/msal-net-2-released)", true)]
        string Name { get; }

        /// <summary>
        /// In MSAL.NET 1.x was the URL of the identity provider (e.g. https://login.microsoftonline.com/tenantId).
        /// From MSAL.NET 2.x use <see cref="IAccount.Environment"/> which retrieves the host only (e.g. login.microsoftonline.com).
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.Environment instead to get the Identity Provider host (See https://aka.ms/msal-net-2-released)", true)]
        string IdentityProvider { get; }

        /// <summary>
        /// In MSAL.NET 1.x was an identifier for the user in the guest tenant.
        /// From MSAL.NET 2.x, use <see cref="IAccount.HomeAccountId"/><see cref="AccountId.Identifier"/> to get
        /// the user identifier (globally unique accross tenants). See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use IAccount.HomeAccountId.Identifier instead to get the user identifier (See https://aka.ms/msal-net-2-released)", true)]
        string Identifier { get; }
    }

    /// <Summary>
    /// Interface defining common API methods and properties. Both <see cref="T:PublicClientApplication"/> and <see cref="T:ConfidentialClientApplication"/> 
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public partial interface IClientApplicationBase
    {
        /// <summary>
        /// In MSAL 1.x returned an enumeration of <see cref="IUser"/>. From MSAL 2.x, use <see cref="GetAccountsAsync"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use GetAccountsAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        IEnumerable<IUser> Users { get; }

        /// <summary>
        /// In MSAL 1.x, return a user from its identifier. From MSAL 2.x, use <see cref="GetAccountsAsync"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="identifier">Identifier of the user to retrieve</param>
        /// <returns>the user in the cache with the identifier passed as an argument</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAccountAsync instead and pass IAccount.HomeAccountId.Identifier (See https://aka.ms/msal-net-2-released)", true)]
        IUser GetUser(string identifier);

        /// <summary>
        /// In MSAL 1.x removed a user from the cache. From MSAL 2.x, use <see cref="RemoveAsync(IAccount)"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="user">User to remove from the cache</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use RemoveAccountAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        void Remove(IUser user);
    }

    /// <Summary>
    /// Abstract class containing common API methods and properties. Both <see cref="T:PublicClientApplication"/> and <see cref="T:ConfidentialClientApplication"/> 
    /// extend this class. For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public partial class ClientApplicationBase
    {
        /// <summary>
        /// In MSAL 1.x returned an enumeration of <see cref="IUser"/>. From MSAL 2.x, use <see cref="GetAccountsAsync"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use GetAccountsAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        public IEnumerable<IUser> Users { get { throw new NotImplementedException(); } }

        /// <summary>
        /// In MSAL 1.x, return a user from its identifier. From MSAL 2.x, use <see cref="GetAccountsAsync"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="identifier">Identifier of the user to retrieve</param>
        /// <returns>the user in the cache with the identifier passed as an argument</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetAccountAsync instead and pass IAccount.HomeAccountId.Identifier (See https://aka.ms/msal-net-2-released)", true)]
        public IUser GetUser(string identifier) { throw new NotImplementedException(); }

        /// <summary>
        /// In MSAL 1.x removed a user from the cache. From MSAL 2.x, use <see cref="RemoveAsync(IAccount)"/> instead.
        /// See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        /// <param name="user">User to remove from the cache</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use RemoveAccountAsync instead (See https://aka.ms/msal-net-2-released)", true)]
        public void Remove(IUser user) { throw new NotImplementedException(); }
    }

    public partial class AuthenticationResult
    {
        /// <summary>
        /// In MSAL.NET 1.x, returned the user who signed in to get the authentication result. From MSAL 2.x
        /// rather use <see cref="Account"/> instead. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use Account instead (See https://aka.ms/msal-net-2-released)", true)]
        public IUser User { get { throw new NotImplementedException(); } }
    }

    public partial class TokenCacheNotificationArgs
    {
        /// <summary>
        /// In MSAL.NET 1.x, returned the user who signed in to get the authentication result. From MSAL 2.x
        /// rather use <see cref="Account"/> instead. See https://aka.ms/msal-net-2-released for more details.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Obsolete("Use Account instead (See https://aka.ms/msal-net-2-released)", true)]
        public IUser User { get { throw new NotImplementedException(); } }
    }

    /// <Summary>
    /// Abstract class containing common API methods and properties. 
    /// For details see https://aka.ms/msal-net-client-applications
    /// </Summary>
    public partial class PublicClientApplication
    {
        #pragma warning disable 1998
        /// <summary>
        /// In ADAL.NET, acquires security token from the authority, using the username/password authentication, 
        /// with the password sent in clear. 
        /// In MSAL 2.x, only the method that accepts a SecureString parameter is supported.
        /// 
        /// See https://aka.ms/msal-net-up for more details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.</param>
        /// <param name="password">User password.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use overload with SecureString instead (See https://aka.ms/msal-net-up)", true)]
        public async Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(IEnumerable<string> scopes, string username, string password)
        {
            { throw new NotImplementedException(); }
        }
        #pragma warning restore 1998
    }
}
