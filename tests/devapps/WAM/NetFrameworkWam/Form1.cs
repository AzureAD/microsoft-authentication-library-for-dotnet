using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;

namespace NetDesktopWinForms
{
    public partial class Form1 : Form
    {
        private readonly SynchronizationContext _syncContext;

        private static List<ClientEntry> s_clients = new List<ClientEntry>()
        {
            new ClientEntry() { Id = "04f0c124-f2bc-4f59-8241-bf6df9866bbd", Name = "04f0c124-f2bc-4f59-8241-bf6df9866bbd (new VS)"},
            new ClientEntry() { Id = "d735b71b-9eee-4a4f-ad23-421660877ba6", Name = "d735b71b-9eee-4a4f-ad23-421660877ba6 (new GCM)"},
            new ClientEntry() { Id = "1d18b3b0-251b-4714-a02a-9956cec86c2d", Name = "1d18b3b0-251b-4714-a02a-9956cec86c2d (App in 49f)"},
            new ClientEntry() { Id = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1", Name = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1 (VS)"},
            new ClientEntry() { Id = "655015be-5021-4afc-a683-a4223eb5d0e5", Name = "655015be-5021-4afc-a683-a4223eb5d0e5"},
            new ClientEntry() { Id = "c0186a6c-0bfc-4d83-9543-c2295b676f3b", Name = "MSA-PT (lab user and tenanted only)"},
            new ClientEntry() { Id = "95de633a-083e-42f5-b444-a4295d8e9314", Name = "Whiteboard App"}
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

            var pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(this.authorityCbx.Text)
                //.WithDesktopFeatures()
                .WithWindowsBroker()
                .WithBroker(this.useBrokerChk.Checked)
                // there is no need to construct the PCA with this redirect URI, 
                // but WAM uses it. We could enforce it.
                .WithRedirectUri($"ms-appx-web://microsoft.aad.brokerplugin/{clientId}")
                //.WithRedirectUri("ms-appx-web://microsoft.aad.brokerplugin/95de633a-083e-42f5-b444-a4295d8e9314")
                .WithWindowsBrokerOptions(new WindowsBrokerOptions()
                {
                    ListWindowsWorkAndSchoolAccounts = cbxListOsAccounts.Checked,
                    MsaPassthrough = cbxMsaPt.Checked, 
                    HeaderText = "MSAL Dev App .NET FX"
                })
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
                    // TODO: bogavril - move this exception in WAM
                    throw new InvalidOperationException(
                        "[TEST APP FAILURE] Do not use login hint on AcquireTokenSilent for MSA-Passthrough. Use the IAccount overload.");
                }

                Log($"ATS with login hint: " + loginHint);
                return await pca.AcquireTokenSilent(GetScopes(), loginHint)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
            }

            if (cbxAccount.SelectedItem != null &&
                (cbxAccount.SelectedItem as AccountModel).Account != s_nullAccount)
            {
                var acc = (cbxAccount.SelectedItem as AccountModel).Account;

                var builder = pca.AcquireTokenSilent(GetScopes(), acc);
                if (IsMsaPassthroughConfigured() && !useBrokerChk.Checked)
                {
                    // this is the same in all clouds
                    const string PersonalTenantIdV2AAD = "9188040d-6c67-4c5b-b112-36a304b66dad";

                    // these are per cloud
                    string publicCloudEnv = "https://login.microsoftonline.com/";
                    string msaTenantIdPublicCloud = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";

                    if (acc.HomeAccountId.TenantId == PersonalTenantIdV2AAD)
                    {
                        var msaAuthority = $"{publicCloudEnv}{msaTenantIdPublicCloud}";

                        builder = builder.WithAuthority(msaAuthority);
                    }
                }
                else
                {
                    builder = builder.WithAuthority(reqAuthority);
                }

                Log($"ATS with IAccount for {acc?.Username ?? acc.HomeAccountId.ToString() ?? "null"}");
                return await builder
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
            var scopes = GetScopes();

            var builder = pca.AcquireTokenInteractive(scopes)
                .WithUseEmbeddedWebView(true)
                //.WithExtraQueryParameters("domain_hint=live.com") -- will force AAD login with browser
                //.WithExtraQueryParameters("msafed=0")             -- will force MSA login with browser
                .WithEmbeddedWebViewOptions(
                new EmbeddedWebViewOptions()
                {
                    Title = "Hello world",
                })
                .WithParentActivityOrWindow(this.Handle);

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

            await Task.Delay(500).ConfigureAwait(false);
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
            Log("Clearing the cache ...");
            var pca = CreatePca();
            foreach (var acc in (await pca.GetAccountsAsync().ConfigureAwait(false)))
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            Log("Done clearing the cache.");
        }

        private void clientIdCbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientEntry clientEntry = (ClientEntry)clientIdCbx.SelectedItem;

            if (clientEntry.Id == "872cd9fa-d31f-45e0-9eab-6e460a02d1f1") // VS
            {
                cbxScopes.SelectedItem = "https://management.core.windows.net//.default";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/organizations";
            }

            if (clientEntry.Id == "04f0c124-f2bc-4f59-8241-bf6df9866bbd") // VS
            {
                cbxScopes.SelectedItem = "https://management.core.windows.net//.default";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/organizations";
            }

            if (clientEntry.Id == "d735b71b-9eee-4a4f-ad23-421660877ba6") // new GCM
            {
                cbxScopes.SelectedItem = "499b84ac-1321-427f-aa17-267ca6975798/vso.code_full";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/organizations";
            }

            if (clientEntry.Id == "c0186a6c-0bfc-4d83-9543-c2295b676f3b") // MSA-PT app
            {
                cbxScopes.SelectedItem = "api://51eb3dd6-d8b5-46f3-991d-b1d4870de7de/myaccess";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/61411618-6f67-4fc5-ba6a-4a0fe32d4eec";
            }
        }

        private async void btnExpire_Click(object sender, EventArgs e)
        {
            Log("Expiring tokens.");

            var pca = CreatePca();

            // do something that loads the cache first
            await pca.GetAccountsAsync().ConfigureAwait(false);

            var m = pca.UserTokenCache.GetType().GetRuntimeMethods().Where(n => n.Name == "ExpireAllAccessTokensForTestAsync");

            var task = pca.UserTokenCache.GetType()
                .GetRuntimeMethods()
                .Single(n => n.Name == "ExpireAllAccessTokensForTestAsync")
                .Invoke(pca.UserTokenCache, null);

            await (task as Task).ConfigureAwait(false);
           
            Log("Done expiring tokens.");
        }

        private async void btnRemoveAcc_Click(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
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

        public AccountModel(IAccount account, string displayValue = null)
        {
            Account = account;
            string env = string.IsNullOrEmpty(Account?.Environment) || Account.Environment == "login.microsoftonline.com" ?
                "" :
                $"({Account.Environment})";
            string homeTenantId = account?.HomeAccountId?.TenantId?.Substring(0, 5);

            DisplayValue = displayValue ?? $"{Account.Username} {env} {homeTenantId}";
        }
    }

    public class NullAccount : IAccount
    {
        public string Username => "";

        public string Environment => "";

        public AccountId HomeAccountId => null;
    }
}
