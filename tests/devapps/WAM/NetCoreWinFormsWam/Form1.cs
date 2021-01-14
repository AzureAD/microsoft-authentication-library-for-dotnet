using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;

namespace NetCoreWinFormsWAM
{

    public partial class Form1 : Form
    {
        private readonly SynchronizationContext _syncContext;


        private static List<ClientEntry> s_clients = new List<ClientEntry>()
        {
            new ClientEntry() { Id = "1d18b3b0-251b-4714-a02a-9956cec86c2d", Name = "1d18b3b0-251b-4714-a02a-9956cec86c2d (App in 49f)"},
            new ClientEntry() { Id = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1", Name = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1 (VS)"},
            new ClientEntry() { Id = "655015be-5021-4afc-a683-a4223eb5d0e5", Name = "655015be-5021-4afc-a683-a4223eb5d0e5"}
        };

        private BindingList<AccountModel> s_accounts = new BindingList<AccountModel>();
        private static IAccount s_nullAccount = new NullAccount();
        private static AccountModel s_nullAccountModel = new AccountModel(s_nullAccount, "");
        private static AccountModel s_osAccountModel = new AccountModel(PublicClientApplication.OperatingSystemAccount, "Default OS Account");

        public Form1()
        {
            InitializeComponent();
            var clientIdBindingSource = new BindingSource();
            clientIdBindingSource.DataSource = s_clients;

            clientIdCbx.DataSource = clientIdBindingSource.DataSource;

            clientIdCbx.DisplayMember = "Name";
            clientIdCbx.ValueMember = "Id";

            //var accountBidingSource = new BindingSource();
            //accountBidingSource.DataSource = s_accounts;

            s_accounts.Add(s_nullAccountModel);
            s_accounts.Add(s_osAccountModel);

            cbxAccount.DataSource = s_accounts;

            cbxAccount.DisplayMember = "DisplayValue";
            cbxAccount.SelectedItem = null;

            _syncContext = SynchronizationContext.Current;
        }

        public static readonly string UserCacheFile =
            System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";


        private IPublicClientApplication CreatePca()
        {
            string clientId = GetClientId();
            bool msaPt = IsMsaPassthroughConfigured();
            string extraQp = null;
            if (msaPt)
            {
                // TODO: better config option, e.g. WithMsaPt(true), 
                extraQp = "MSAL_MSA_PT=1"; // not an actual QP, MSAL will simply use this to provide good experience for MSA-PT
            }

            var pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(this.authorityCbx.Text)
                .WithExperimentalFeatures(true)
                //.WithBroker(this.useBrokerChk.Checked)
                .WithWindowsBroker(this.useBrokerChk.Checked)
                // there is no need to construct the PCA with this redirect URI, 
                // but WAM uses it. We could enforce it.
                .WithRedirectUri($"ms-appx-web://microsoft.aad.brokerplugin/{clientId}")
                .WithParentActivityOrWindow(() => this.Handle)
                .WithExtraQueryParameters(extraQp)
                .WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true)
                .Build();

            BindCache(pca.UserTokenCache, UserCacheFile);
            return pca;
        }


        private static void BindCache(ITokenCache tokenCache, string file)
        {
            tokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(file)
                    ? File.ReadAllBytes(UserCacheFile)
                    : null);
            });

            tokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(file, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }

        private async void atsBtn_Click(object sender, EventArgs e)
        {

            try
            {
                var pca = CreatePca();
                AuthenticationResult result = await RunAtsAsync(pca).ConfigureAwait(false);

                await LogResultAndRefreshAccountsAsync(result).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }

        }

        private async Task<AuthenticationResult> RunAtsAsync(IPublicClientApplication pca)
        {
            string reqAuthority = pca.Authority;


            string loginHint = GetLoginHint();
            if (!string.IsNullOrEmpty(loginHint) && cbxAccount.SelectedIndex > 0)
            {
                throw new InvalidOperationException("[TEST APP FAILURE] Please use either the login hint or the account");
            }

            if (!string.IsNullOrEmpty(loginHint))
            {
                if (IsMsaPassthroughConfigured())
                {
                    throw new InvalidAsynchronousStateException(
                        "[TEST APP FAILURE] Do not use login hint on AcquireTokenSilent for MSA-Passthrough. Use the IAccount overload.");
                }

                Log($"ATS with login hint: " + loginHint);
                return await pca.AcquireTokenSilent(GetScopes(), loginHint)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
            }

            if (cbxAccount.SelectedItem != null &&
                cbxAccount.SelectedItem != s_nullAccount)
            {
                var acc = (cbxAccount.SelectedItem as AccountModel).Account;

                // Today, apps using MSA-PT must manually target the correct tenant 
                if (IsMsaPassthroughConfigured() && acc.HomeAccountId.TenantId == "9188040d-6c67-4c5b-b112-36a304b66dad")
                {
                    reqAuthority = "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a";
                }

                Log($"ATS with IAccount for {acc?.Username ?? acc.HomeAccountId.ToString() ?? "null"}");
                return await pca.AcquireTokenSilent(GetScopes(), acc)
                    .WithAuthority(reqAuthority)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }

            Log($"ATS with no account or login hint ... will fail with UiRequiredEx");
            return await pca.AcquireTokenSilent(GetScopes(), (IAccount)null)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        private string[] GetScopes()
        {
            string[] result = null;
            cbxScopes.Invoke((MethodInvoker)delegate
            {
                result = cbxScopes.Text.Split(' ');
            });

            return result;
        }

        private string GetClientId()
        {
            string clientId = null;

            clientIdCbx.Invoke((MethodInvoker)delegate
            {
                clientId = (this.clientIdCbx.SelectedItem as ClientEntry)?.Id ?? this.clientIdCbx.Text;
            });

            return clientId;
        }


        private async Task LogResultAndRefreshAccountsAsync(AuthenticationResult ar)
        {
            string message =

                $"Account.Username {ar.Account.Username}" + Environment.NewLine +
                $"Account.HomeAccountId {ar.Account.HomeAccountId}" + Environment.NewLine +
                $"Account.Environment {ar.Account.Environment}" + Environment.NewLine +
                $"TenantId {ar.TenantId}" + Environment.NewLine +
                $"Expires {ar.ExpiresOn.ToLocalTime()} local time" + Environment.NewLine +
                $"Source {ar.AuthenticationResultMetadata.TokenSource}" + Environment.NewLine +
                $"Scopes {String.Join(" ", ar.Scopes)}" + Environment.NewLine +
                $"AccessToken: {ar.AccessToken} " + Environment.NewLine +
                $"IdToken {ar.IdToken}" + Environment.NewLine;

            Log(message);

            await _syncContext;

            Log("Refreshing accounts");
            await RefreshAccountsAsync().ConfigureAwait(true);
        }

        private void Log(string message)
        {
            resultTbx.Invoke((MethodInvoker)delegate
            {
                resultTbx.AppendText(message + Environment.NewLine);
            });
        }

        private async void atiBtn_Click(object sender, EventArgs e)
        {
            try
            {
                var pca = CreatePca();
                AuthenticationResult result = await RunAtiAsync(pca).ConfigureAwait(false);

                await LogResultAndRefreshAccountsAsync(result).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        private async Task<AuthenticationResult> RunAtiAsync(IPublicClientApplication pca)
        {
            string loginHint = GetLoginHint();
            if (!string.IsNullOrEmpty(loginHint) && cbxAccount.SelectedIndex > 0)
            {
                throw new InvalidOperationException("[TEST APP FAILURE] Please use either the login hint or the account, but not both");
            }

            AuthenticationResult result = null;

            var builder = pca.AcquireTokenInteractive(GetScopes());


            Prompt? prompt = GetPrompt();
            if (prompt.HasValue)
            {
                builder = builder.WithPrompt(prompt.Value);
            }

            if (!string.IsNullOrEmpty(loginHint))
            {
                Log($"ATI WithLoginHint  {loginHint}");
                builder = builder.WithLoginHint(loginHint);
            }
            else if (cbxAccount.SelectedIndex > 0)
            {
                var acc = (cbxAccount.SelectedItem as AccountModel).Account;
                Log($"ATI WithAccount for account {acc?.Username ?? "null" }");
                builder = builder.WithAccount(acc);
            }
            else
            {
                Log($"ATI without login_hint or account. It should display the account picker");
            }

            result = await builder.ExecuteAsync().ConfigureAwait(false);


            return result;
        }

        private string GetLoginHint()
        {
            string loginHint = null;
            loginHintTxt.Invoke((MethodInvoker)delegate
            {
                loginHint = loginHintTxt.Text;
            });

            return loginHint;
        }

        /// <summary>
        /// It should be possible to omit this if the Account Picker is never invoked, e.g. Office, 
        /// by using a special auth flow based on a transfer token 
        /// </summary>
        /// <returns></returns>
        private bool IsMsaPassthroughConfigured()
        {
            bool msa = false;
            cbxMsaPt.Invoke((MethodInvoker)delegate
            {
                msa = cbxMsaPt.Checked;
            });

            return msa;
        }

        private Prompt? GetPrompt()
        {

            string prompt = null;
            promptCbx.Invoke((MethodInvoker)delegate
            {
                prompt = promptCbx.Text;
            });

            if (string.IsNullOrEmpty(prompt))
                return null;

            switch (prompt)
            {

                case "select_account":
                    return Prompt.SelectAccount;
                case "force_login":
                    return Prompt.ForceLogin;
                case "no_prompt":
                    return Prompt.NoPrompt;
                case "consent":
                    return Prompt.Consent;

                default:
                    throw new NotImplementedException();
            }
        }

        private async void getAccountsBtn_Click(object sender, EventArgs e)
        {
            await RefreshAccountsAsync().ConfigureAwait(false);
        }

        private async Task RefreshAccountsAsync()
        {
            var pca = CreatePca();
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(true);

            s_accounts.Clear();
            s_accounts.Add(s_nullAccountModel);
            s_accounts.Add(s_osAccountModel);

            foreach (var acc in accounts)
            {
                s_accounts.Add(new AccountModel(acc));
            }

            string msg = "Accounts " + Environment.NewLine +
                string.Join(
                     Environment.NewLine,
                    accounts.Select(acc => $"{acc.Username} {acc.Environment} {acc.HomeAccountId.TenantId}"));
            Log(msg);
        }

        private async void atsAtiBtn_Click(object sender, EventArgs e)
        {

            var pca = CreatePca();

            try
            {
                var result = await RunAtsAsync(pca).ConfigureAwait(false);

                await LogResultAndRefreshAccountsAsync(result).ConfigureAwait(false);

            }
            catch (MsalUiRequiredException ex)
            {
                await _syncContext;

                Log("UI required Exception! " + ex.ErrorCode + " " + ex.Message);
                try
                {
                    var result = await RunAtiAsync(pca).ConfigureAwait(false);
                    await LogResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                }
                catch (Exception ex3)
                {
                    Log("Exception: " + ex3);
                }

            }
            catch (Exception ex2)
            {
                Log("Exception: " + ex2);
            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            resultTbx.Text = "";
        }

        private async void btnClearCache_Click(object sender, EventArgs e)
        {
            var pca = CreatePca();
            foreach (var acc in (await pca.GetAccountsAsync().ConfigureAwait(false)))
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }
        }

        private void clientIdCbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientEntry clientEntry = (ClientEntry)clientIdCbx.SelectedItem;

            if (clientEntry.Id == "872cd9fa-d31f-45e0-9eab-6e460a02d1f1") // VS
            {
                cbxScopes.SelectedItem = "https://management.core.windows.net//.default";
                authorityCbx.SelectedItem = "https://login.windows-ppe.net/organizations";
            }


        }

        private async void btnExpire_Click(object sender, EventArgs e)
        {
            var pca = CreatePca();

            // do smth that loads the cache first
            await pca.GetAccountsAsync().ConfigureAwait(false);

            string expiredValue = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)
                    .ToString(CultureInfo.InvariantCulture);

            var accessor = pca.UserTokenCache.GetType()
                .GetRuntimeProperties()
                .Single(p => p.Name == "Microsoft.Identity.Client.ITokenCacheInternal.Accessor")
                .GetValue(pca.UserTokenCache);

            var internalAccessTokens = accessor.GetType().GetMethod("GetAllAccessTokens").Invoke(accessor, null) as IEnumerable<object>;

            foreach (var internalAt in internalAccessTokens)
            {
                internalAt.GetType().GetRuntimeMethods().Single(m => m.Name == "set_ExpiresOnUnixTimestamp").Invoke(internalAt, new[] { expiredValue });
                accessor.GetType().GetMethod("SaveAccessToken").Invoke(accessor, new[] { internalAt });
            }

            var ctor = typeof(TokenCacheNotificationArgs).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

            var argz = ctor.Invoke(new object[] { pca.UserTokenCache, GetClientId(), null, true, false, true, null });
            var task = pca.UserTokenCache.GetType().GetRuntimeMethods()
                .Single(m => m.Name == "Microsoft.Identity.Client.ITokenCacheInternal.OnAfterAccessAsync")
                .Invoke(pca.UserTokenCache, new[] { argz });

            await (task as Task).ConfigureAwait(false);
        }

        private async void remoteAcc_click(object sender, EventArgs e)
        {
            if (cbxAccount.SelectedIndex == 0)
            {
                throw new InvalidOperationException("[TEST APP FAILURE] Please select an account");
            }

            var pca = CreatePca();
            var acc = (cbxAccount.SelectedItem as AccountModel).Account;

            await pca.RemoveAsync(acc).ConfigureAwait(false);

            Log("Removed account " + acc.Username);
        }
    }

    public class ClientEntry
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    public class AccountModel
    {
        public IAccount Account { get; }

        public string DisplayValue { get; }
        //public string IdValue => $"{_account.HomeAccountId.Identifier}";

        public AccountModel(IAccount account, string displayValue = null)
        {
            Account = account;
            string env = string.IsNullOrEmpty(Account?.Environment) || Account.Environment == "login.microsoftonline.com" ?
                "" :
                $"({Account.Environment})";

            DisplayValue = displayValue ?? $"{Account.Username} {env}";
        }


    }


    public class NullAccount : IAccount
    {
        public string Username => "";

        public string Environment => "";

        public AccountId HomeAccountId => null;
    }

}

