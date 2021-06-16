using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Microsoft.Identity.Client;


namespace App1
{
    /// <summary>
    /// This is PoC common wrapper interface for all the platforms
    /// it can be further extended on each platform to support other not commom scanarios
    /// </summary>
    public interface INativeWrapper
    {
        /// <summary>
        /// This is called on each platform to perfrom initialization required for the pltform
        /// </summary>
        /// <param name="mpca"></param>
        void Init(IMultipleAccountPublicClientApplication mpca);

        /// <summary>
        /// Method to get accounts
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IAccount>> GetAccountsAsync();

        /// <summary>
        /// Acquires token silently for an account
        /// </summary>
        /// <param name="scopes"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        Task<IAuthenticationResult> AcquireTokenSilentAsync(string[] scopes, IAccount account);

        /// <summary>
        /// Acquires token interactively based on teh scopes.
        /// This must be called on the UI thread.
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        Task<IAuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes);
    }
}
