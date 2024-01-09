// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.ApiConfig;

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
            new ClientEntry() { Id = "95de633a-083e-42f5-b444-a4295d8e9314", Name = "Whiteboard App"},
            new ClientEntry() { Id = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0", Name = "Lab Public Multi-Tenant"}, //https://docs.msidlab.com/accounts/adfsv4.html
            new ClientEntry() { Id = "682992e9-c9c6-49c9-a819-3fbca2dd5111", Name = "Lab 4 - Azure AD MyOrg"}, //https://docs.msidlab.com/accounts/cloudaccounts.html
            new ClientEntry() { Id = "9668f2bd-6103-4292-9024-84fa2d1b6fb2", Name = "Lab 4 - MSA APP"}, //https://docs.msidlab.com/accounts/msaprod.html
            new ClientEntry() { Id = "cb7faed4-b8c0-49ee-b421-f5ed16894c83", Name = "Lab - AzureUSGovernment MyOrg"}, //https://docs.msidlab.com/accounts/arlington-intro.html
            new ClientEntry() { Id = "952de729-a67a-471e-9717-45f407cb4fd7", Name = "Lab - AzureChinaCloud MyOrg"}, //https://docs.msidlab.com/accounts/mooncake.html
            new ClientEntry() { Id = "682992e9-c9c6-49c9-a819-3fbca2dd5111", Name = "Cross Cloud App"} //https://docs.msidlab.com/accounts/xc.html
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

            cbxUseWam.DataSource = Enum.GetValues(typeof(AuthMethod));
            cbxUseWam.SelectedIndex = 1;

        }

        private AuthMethod GetAuthMethod()
        {
            AuthMethod status;
            string result = string.Empty;

            cbxUseWam.Invoke((MethodInvoker)delegate
            {
                result = cbxUseWam.Text;
            });

            if (Enum.TryParse<AuthMethod>(result, out status))
            {
                return status;
            }
            throw new NotImplementedException();
        }

        public static readonly string UserCacheFile =
            System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";

        private IPublicClientApplication CreatePca(AuthMethod? authMethod)
        {
            string clientId = GetClientId();
            string authority = GetAuthority();

            bool msaPt = IsMsaPassthroughConfigured();

            var builder = PublicClientApplicationBuilder
                .Create(clientId)
                .WithRedirectUri("http://localhost")
                .WithClientCapabilities(new[] { "cp1"})
                .WithMultiCloudSupport(cbxMultiCloud2.Checked)
                .WithAuthority(authority);

            if (authMethod == null)
                authMethod = GetAuthMethod();

            switch (authMethod)
            {
                case AuthMethod.WAM:
                case AuthMethod.WAMRuntime:
                    builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
                    {
                        ListOperatingSystemAccounts = cbxListOsAccounts.Checked,
                        MsaPassthrough = cbxMsaPt.Checked,
                        Title = "MSAL Dev App .NET FX"
                    });
                    break;
                case AuthMethod.SystemBrowser:
                    builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.None));
                    builder = builder.WithBroker(false);
                    break;
                case AuthMethod.EmbeddedBrowser:
                    builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.None));
                    builder = builder.WithBroker(false);

                    break;
                default:
                    throw new NotImplementedException();
            }

            builder.WithLogging((x, y, z) => Debug.WriteLine($"{x} {y}"), LogLevel.Verbose, true, true);

            var pca = builder.Build();

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
                var pca = CreatePca(GetAuthMethod());
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
            IAccount acc = GetAccount();

            if (!string.IsNullOrEmpty(loginHint) && (acc != null))
            {
                throw new InvalidOperationException("[TEST APP FAILURE] Please use either the login hint or the account");
            }

            if (!string.IsNullOrEmpty(loginHint))
            {
                Log($"ATS with login hint: " + loginHint);
                var builder = pca.AcquireTokenSilent(GetScopes(), loginHint);

                if (cbxPOP.Checked)
                {
                    builder = builder.WithProofOfPossession(
                        Guid.NewGuid().ToString(),
                        System.Net.Http.HttpMethod.Get,
                        GetRandomDownstreamUri());
                }

                return await builder.ExecuteAsync(GetAutocancelToken()).ConfigureAwait(false);
            }

            if (acc != null)
            {
                var builder = pca.AcquireTokenSilent(GetScopes(), acc);
                if (IsMsaPassthroughConfigured() && (GetAuthMethod() == AuthMethod.SystemBrowser || GetAuthMethod() == AuthMethod.EmbeddedBrowser))
                {
                    // this is the same in all clouds
                    const string PersonalTenantIdV2AAD = "9188040d-6c67-4c5b-b112-36a304b66dad";

                    // these are per cloud
                    string publicCloudEnv = "https://login.microsoftonline.com/";
                    string msaTenantIdPublicCloud = "f8cdef31-a31e-4b4a-93e4-5f571e91255a";

                    if (acc.HomeAccountId.TenantId == PersonalTenantIdV2AAD)
                    {
                        var msaAuthority = $"{publicCloudEnv}{msaTenantIdPublicCloud}";

#pragma warning disable CS0618 // Type or member is obsolete
                        builder = builder.WithAuthority(msaAuthority);
                    }
                }
                else
                {
                    builder = builder.WithAuthority(reqAuthority);
                }
#pragma warning restore CS0618 // Type or member is obsolete

                if (cbxPOP.Checked)
                {
                    builder = builder.WithProofOfPossession(
                        Guid.NewGuid().ToString(),
                    System.Net.Http.HttpMethod.Get,
                    GetRandomDownstreamUri());
                }

                if (cbxWithForceRefresh.Checked)
                {
                    builder = builder.WithForceRefresh(true);
                }

                Log($"ATS with IAccount for {acc?.Username ?? acc.HomeAccountId.ToString() ?? "null"}");
                return await builder
                    .ExecuteAsync(GetAutocancelToken())
                    .ConfigureAwait(false);
            }

            Log($"ATS with no account or login hint ... will fail with UiRequiredEx");
            return await pca.AcquireTokenSilent(GetScopes(), (IAccount)null)

                .ExecuteAsync(GetAutocancelToken())
                .ConfigureAwait(false);
        }

        private string[] GetScopes()
        {
            string[] result = null;

            cbxScopes.Invoke((MethodInvoker)delegate
            {
                if (!string.IsNullOrWhiteSpace(cbxScopes.Text))
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

        private string GetAuthority()
        {
            string result = null;
            authorityCbx.Invoke((MethodInvoker)delegate
            {
                result = authorityCbx.Text;
            });

            return result;
        }

        private async Task LogResultAndRefreshAccountsAsync(AuthenticationResult ar, bool refreshAccounts = true)
        {
            string message =

                $"Account.Username {ar.Account.Username}" + Environment.NewLine +
                $"Account.HomeAccountId {ar.Account.HomeAccountId}" + Environment.NewLine +
                $"Account.Environment {ar.Account.Environment}" + Environment.NewLine +
                $"TenantId {ar.TenantId}" + Environment.NewLine +
                $"Expires {ar.ExpiresOn.ToLocalTime()} local time" + Environment.NewLine +
                $"Source {ar.AuthenticationResultMetadata.TokenSource}" + Environment.NewLine +
                $"Scopes {string.Join(" ", ar.Scopes)}" + Environment.NewLine +
                $"AccessToken: {ar.AccessToken} " + Environment.NewLine +
                $"IdToken {ar.IdToken}" + Environment.NewLine;

            Log(message);

            await _syncContext;

            Log("Refreshing accounts");

            if (refreshAccounts)
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
                var pca = CreatePca(GetAuthMethod());
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
            var guid = Guid.NewGuid();
            var builder = pca.AcquireTokenInteractive(scopes)
                             .WithParentActivityOrWindow(this.Handle);

            if (GetAuthMethod() == AuthMethod.SystemBrowser)
            {
                builder.WithSystemWebViewOptions(new SystemWebViewOptions() { HtmlMessageSuccess = "Successful login! You can close the tab." });
            }
            else
            {
                builder.WithUseEmbeddedWebView(true)
                //.WithExtraQueryParameters("domain_hint=live.com") -- will force AAD login with browser
                //.WithExtraQueryParameters("msafed=0")             -- will force MSA login with browser
                .WithEmbeddedWebViewOptions(
                new EmbeddedWebViewOptions()
                {
                    Title = "Hello world",
                });
            }

            if (cbxPOP.Checked)
            {
                builder = builder.WithProofOfPossession(
                    Guid.NewGuid().ToString(),
                    System.Net.Http.HttpMethod.Get,
                    GetRandomDownstreamUri());
            }

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
                Log($"ATI WithAccount for account {acc?.Username ?? "null"}");
                builder = builder.WithAccount(acc);
            }
            else
            {
                Log($"ATI without login_hint or account. It should display the account picker");
            }

            if (cbxBackgroundThread.Checked)
            {
                await Task.Delay(500).ConfigureAwait(false);
            }

            result = await builder.ExecuteAsync(GetAutocancelToken()).ConfigureAwait(false);

            return result;
        }

        private static Uri GetRandomDownstreamUri()
        {
            Uri downstreamApi = new Uri($"https://www.contoso.com/path1/path2/{Guid.NewGuid()}");

            return downstreamApi;
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

        private IAccount GetAccount()
        {
            IAccount acc = null;
            cbxAccount.Invoke((MethodInvoker)delegate
            {
                if (cbxAccount.SelectedIndex <= 0)
                    return;

                acc = (cbxAccount.SelectedItem as AccountModel).Account;
            });

            return acc;
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
            try
            {
                var pca = CreatePca(GetAuthMethod());
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
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private async void atsAtiBtn_Click(object sender, EventArgs e)
        {

            var pca = CreatePca(GetAuthMethod());

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

        private async void atUsernamePwdBtn_Click(object sender, EventArgs e)
        {
            var pca = CreatePca(GetAuthMethod());

            try
            {
                var result = await RunAtUsernamePwdAsync(pca).ConfigureAwait(false);

                await LogResultAndRefreshAccountsAsync(result).ConfigureAwait(false);

            }
            catch (Exception ex2)
            {
                Log("Exception: " + ex2);
            }
        }

        private async Task<AuthenticationResult> RunAtUsernamePwdAsync(IPublicClientApplication pca)
        {
            Tuple<string, string> credentials = GetUsernamePassword();
            string username = credentials.Item1;
            string password = credentials.Item2;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Log("[TEST APP FAILURE] Username or password is missing.");
                return null;
            }

            AuthenticationResult result = null;
            var scopes = GetScopes();

            var builder = pca.AcquireTokenByUsernamePassword(scopes, username, password);

            if (cbxPOP.Checked)
            {
                builder = builder.WithProofOfPossession(
                    Guid.NewGuid().ToString(),
                    System.Net.Http.HttpMethod.Get,
                    GetRandomDownstreamUri());
            }

            if (cbxBackgroundThread.Checked)
            {
                await Task.Delay(500).ConfigureAwait(false);
            }
            result = await builder.ExecuteAsync().ConfigureAwait(false);

            return result;
        }

        private Tuple<string, string> GetUsernamePassword()
        {
            string username = null;
            string password = null;

            UsernameTxt.Invoke((MethodInvoker)delegate
            {
                username = UsernameTxt.Text;
            });

            PasswordTxt.Invoke((MethodInvoker)delegate
            {
                password = PasswordTxt.Text;
            });

            return Tuple.Create(username, password);
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            resultTbx.Text = "";
        }

        private async void btnClearCache_Click(object sender, EventArgs e)
        {
            Log("Clearing the cache ...");
            var pca = CreatePca(GetAuthMethod());
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

            if (clientEntry.Id == "682992e9-c9c6-49c9-a819-3fbca2dd5111") // Lab MyOrg
            {
                cbxScopes.SelectedItem = "User.Read User.Read.All";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
            }
        }

        private async void btnExpire_Click(object sender, EventArgs e)
        {
            Log("Expiring tokens.");

            var pca = CreatePca(GetAuthMethod());

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
                if (cbxAccount.SelectedIndex <= 0)
                {
                    throw new InvalidOperationException("[TEST APP FAILURE] Please select an account");
                }

                var pca = CreatePca(GetAuthMethod());
                var acc = (cbxAccount.SelectedItem as AccountModel).Account;

                await pca.RemoveAsync(acc).ConfigureAwait(false);

                Log("Removed account " + acc.Username);
            }
            catch (Exception ex)
            {
                await _syncContext;
                Log("Exception: " + ex);
            }
        }

        private CancellationToken GetAutocancelToken()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            if (nudAutocancelSeconds.Value > 0)
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds((int)nudAutocancelSeconds.Value));
                cancellationToken = cts.Token;
            }

            return cancellationToken;
        }

        private async void btnATSperf_Click(object sender, EventArgs e)
        {
            var brokerTimer = new Stopwatch();

            try
            {
                //Old Broker
                btnExpire_Click(sender, e);
                var pca = CreatePca(AuthMethod.WAM);
                brokerTimer.Start();
                AuthenticationResult result1 = await RunAtsAsync(pca).ConfigureAwait(false);
                brokerTimer.Stop();

                var elapsedMilliseconds = brokerTimer.ElapsedMilliseconds;
                await LogResultAndRefreshAccountsAsync(result1, false).ConfigureAwait(false);
                Log($"Execution Time: {elapsedMilliseconds} ms");
                Log("---------------------------------------- ");

                //New Broker
                btnExpire_Click(sender, e);
                pca = CreatePca(AuthMethod.WAMRuntime);
                brokerTimer.Reset();
                brokerTimer.Start();
                AuthenticationResult result2 = await RunAtsAsync(pca).ConfigureAwait(false);
                brokerTimer.Stop();

                await LogResultAndRefreshAccountsAsync(result2).ConfigureAwait(false);
                Log($"Execution Time: {brokerTimer.ElapsedMilliseconds} ms");
                Log("------------------------------------------------------------------------------");
                Log($"\t Perf Results Comparing Old and New WAM. ");
                Log("------------------------------------------------------------------------------");
                Log($"\t \t Old Broker \t New Broker \t");
                Log($"Time Taken : \t {elapsedMilliseconds} ms. \t\t {brokerTimer.ElapsedMilliseconds} ms");
                Log($"Source : \t\t {result1.AuthenticationResultMetadata.TokenSource} \t\t {result2.AuthenticationResultMetadata.TokenSource}");
                Log("------------------------------------------------------------------------------");
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

    public enum AuthMethod
    {
        WAM = 1,
        WAMRuntime = 2,
        EmbeddedBrowser = 3,
        SystemBrowser = 4,
    }
}
