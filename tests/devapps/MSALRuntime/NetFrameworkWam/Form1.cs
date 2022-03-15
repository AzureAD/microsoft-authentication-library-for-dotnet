using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using msalruntime;

namespace NetDesktopWinForms
{
    public partial class Form1 : Form
    {
        private readonly SynchronizationContext _syncContext;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        private static List<ClientEntry> s_clients = new List<ClientEntry>()
        {
            new ClientEntry() { Id = "04f0c124-f2bc-4f59-8241-bf6df9866bbd", Name = "04f0c124-f2bc-4f59-8241-bf6df9866bbd (new VS)"},
            new ClientEntry() { Id = "d735b71b-9eee-4a4f-ad23-421660877ba6", Name = "d735b71b-9eee-4a4f-ad23-421660877ba6 (new GCM)"},
            new ClientEntry() { Id = "1d18b3b0-251b-4714-a02a-9956cec86c2d", Name = "1d18b3b0-251b-4714-a02a-9956cec86c2d (App in 49f)"},
            new ClientEntry() { Id = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1", Name = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1 (VS)"},
            new ClientEntry() { Id = "655015be-5021-4afc-a683-a4223eb5d0e5", Name = "655015be-5021-4afc-a683-a4223eb5d0e5"},
            new ClientEntry() { Id = "c0186a6c-0bfc-4d83-9543-c2295b676f3b", Name = "MSA-PT (lab user and tenanted only)"},
            new ClientEntry() { Id = "95de633a-083e-42f5-b444-a4295d8e9314", Name = "Whiteboard App" },
            new ClientEntry() { Id = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0", Name = "msidlab4.com" }
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
                await RunAtsAsync().ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }

        }

        private async Task RunAtsAsync()
        {
            const string correlationId = "1c4c45ab-4dfc-4891-ad98-cdc13ce265fb";
            string loginHint = GetLoginHint();
            string scopes = "profile mail.read";//"[\"profile\"]";//GetScopes();

            AuthResult result = null;

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

                await Log($"ATS with login hint: " + loginHint).ConfigureAwait(false);

                try
                {
                    using (var core = new msalruntime.Core())
                    using (var authParams = new msalruntime.AuthParameters("26a7ee05-5602-4d76-a7ba-eae8b7b67941", "https://login.microsoftonline.com/common"))
                    {
                        authParams.RequestedScopes = $"[\"{scopes}\"]";
                        authParams.RedirectUri = "about:blank";

                        using (result = await core.SignInSilentlyAsync(authParams, correlationId).ConfigureAwait(false))
                        {
                            await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    await Log("Exception: " + ex).ConfigureAwait(false);
                }
            }

            //Acquire Token Silently 
            try
            {
                using (var core = new msalruntime.Core())
                using (var authParams = new msalruntime.AuthParameters("26a7ee05-5602-4d76-a7ba-eae8b7b67941", "https://login.microsoftonline.com/common"))
                {
                    authParams.RequestedScopes = $"[\"{scopes}\"]";
                    authParams.RedirectUri = "about:blank";

                    using (result = await core.SignInSilentlyAsync(authParams, correlationId).ConfigureAwait(false))
                    {
                        await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }
        }

        private string GetScopes()
        {
            string result = null;
            cbxScopes.Invoke((MethodInvoker)delegate
            {
                result = cbxScopes.Text;
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
            string authority = null;

            authorityCbx.Invoke((MethodInvoker)delegate
            {
                authority = this.authorityCbx.Text;
            });

            return authority;
        }

        private async Task LogRuntimeResultAndRefreshAccountsAsync(AuthResult ar)
        {
            //Old Message with WAM

            //string message =

            //    $"Account.Username {ar.Account.Username}" + Environment.NewLine +
            //    $"Account.HomeAccountId {ar.Account.HomeAccountId}" + Environment.NewLine +
            //    $"Account.Environment {ar.Account.Environment}" + Environment.NewLine +
            //    $"TenantId {ar.TenantId}" + Environment.NewLine +
            //    $"Expires {ar.ExpiresOn.ToLocalTime()} local time" + Environment.NewLine +
            //    $"Source {ar.AuthenticationResultMetadata.TokenSource}" + Environment.NewLine +
            //    $"Scopes {String.Join(" ", ar.Scopes)}" + Environment.NewLine +
            //    $"AccessToken: {ar.AccessToken} " + Environment.NewLine +
            //    $"IdToken {ar.IdToken}" + Environment.NewLine;

            if (ar.IsSuccess)
            {
                //Message Modified with MSAL Runtime
                string message =

                $"Account.Id {ar.Account.Id}" + Environment.NewLine +
                $"Account.ClientInfo {ar.Account.ClientInfo}" + Environment.NewLine +
                $"Expires {ar.ExpiresOn.ToLocalTime()} local time" + Environment.NewLine +
                $"Scopes {String.Join(" ", ar.GrantedScopes)}" + Environment.NewLine +
                $"TelemetryData {String.Join(" ", ar.TelemetryData)}" + Environment.NewLine +
                $"AccessToken: {ar.AccessToken} " + Environment.NewLine +
                $"IdToken {ar.IdToken}" + Environment.NewLine;

                await Log(message).ConfigureAwait(false);
            }
            else
            {
                if (ar.Error.Status.ToString() == "Status = InteractionRequired")
                {
                    throw new MsalUiRequiredException("U123", ar.Error.ToString());
                }
                else
                {
                    throw new msalruntime.Exception(ar.Error);
                }
            }

            await _syncContext;

            //await Log("Refreshing Accounts").ConfigureAwait(false);
            //await RefreshAccountsAsync().ConfigureAwait(true);
        }


        private async Task Log(string message)
        {
            await _syncContext;

            resultTbx.Invoke((MethodInvoker)delegate
            {
                resultTbx.AppendText(message + Environment.NewLine);
            });
        }

        private async void atiBtn_Click(object sender, EventArgs e)
        {
            try
            {
                await RunAtiAsync().ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }
        }

        private async Task RunAtiAsync()
        {
            const string correlationId = "1c4c45ab-4dfc-4891-ad98-cdc13ce265fb";
            string loginHint = GetLoginHint();
            string clientId = GetClientId();
            string authority = GetAuthority();

            IntPtr hWnd = this.Handle;

            if (!string.IsNullOrEmpty(loginHint) && cbxAccount.SelectedIndex > 0)
            {
                throw new InvalidOperationException("[TEST APP FAILURE] Please use either the login hint or the account, but not both");
            }

            AuthResult result = null;
            var scopes = GetScopes();

            Prompt? prompt = GetPrompt();
            if (prompt.HasValue)
            {
                await Log($"ATI Prompt has Value  {prompt.Value}").ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(loginHint))
            {
                await Log($"ATI WithLoginHint  {loginHint}").ConfigureAwait(false);
            }
            else if (cbxAccount.SelectedIndex > 0)
            {
                var acc = (cbxAccount.SelectedItem as AccountModel).Account;
                await Log($"ATI WithAccount for account {acc?.Username ?? "null"}").ConfigureAwait(false);
            }
            else
            {
                await Log($"ATI without login_hint or account. It should display the account picker").ConfigureAwait(false);
            }

            try
            {
                using (var core = new msalruntime.Core())
                using (var authParams = new msalruntime.AuthParameters(clientId, authority))
                {
                    authParams.RequestedScopes = $"[\"{scopes}\"]";
                    authParams.RedirectUri = "about:blank";

                    using (result = await core.SignInInteractivelyAsync(this.Handle, authParams, correlationId).ConfigureAwait(false))
                    {
                        await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                    }
                }
            }
            catch (msalruntime.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }

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
            await Log(msg).ConfigureAwait(false);
        }

        private async Task ReadAccountsAsync()
        {
            const string correlationId = "1c4c45ab-4dfc-4891-ad98-cdc13ce265fb";
            string loginHint = GetLoginHint();
            string clientId = GetClientId();
            string authority = GetAuthority();
            string scopes = GetScopes(); 
            string accountId = "idlab@msidlab4.onmicrosoft.com";

            Account result = null;

            //Read Accounts
            try
            {
                using (var core = new msalruntime.Core())
                using (var authParams = new msalruntime.AuthParameters(clientId, authority))
                {
                    authParams.RequestedScopes = $"[\"{scopes}\"]";
                    authParams.RedirectUri = "about:blank";

                    using (result = await core.ReadAccountByIdAsync(accountId, correlationId).ConfigureAwait(false))
                    {
                        //await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                    }
                }
            }
            catch (msalruntime.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }

        }

        private async void atsAtiBtn_Click(object sender, EventArgs e)
        {
            const string correlationId = "1c4c45ab-4dfc-4891-ad98-cdc13ce265fb";
            string loginHint = GetLoginHint();
            string clientId = GetClientId();
            string authority = GetAuthority();
            string scopes = GetScopes(); //            "profile mail.read";//"[\"profile\"]";//GetScopes();
            AuthResult result = null;

            //Acquire Token Silently 
            try
            {
                using (var core = new msalruntime.Core())
                using (var authParams = new msalruntime.AuthParameters(clientId, authority))
                {
                    authParams.RequestedScopes = $"[\"{scopes}\"]";
                    authParams.RedirectUri = "about:blank";

                    using (result = await core.SignInSilentlyAsync(authParams, correlationId).ConfigureAwait(true))
                    {
                        await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                    }
                }
            }
            catch (msalruntime.Exception ex)
            {
                try
                {
                    using (var core = new msalruntime.Core())
                    using (var authParams = new msalruntime.AuthParameters(clientId, authority))
                    {
                        authParams.RequestedScopes = $"[\"{scopes}\"]";
                        authParams.RedirectUri = "about:blank";

                        using (result = await core.SignInInteractivelyAsync(this.Handle, authParams, correlationId).ConfigureAwait(false))
                        {
                            await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                        }
                    }

                }
                catch (System.Exception ex2)
                {
                    await Log("Exception: " + ex2).ConfigureAwait(false);
                }
            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            resultTbx.Text = "";
        }

        private async void btnClearCache_Click(object sender, EventArgs e)
        {
            await Log("Clearing the cache ...").ConfigureAwait(false);
            var pca = CreatePca();
            foreach (var acc in (await pca.GetAccountsAsync().ConfigureAwait(false)))
            {
                await pca.RemoveAsync(acc).ConfigureAwait(false);
            }

            await Log("Done clearing the cache.").ConfigureAwait(false);
        }


        private void clientIdCbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClientEntry clientEntry = (ClientEntry)clientIdCbx.SelectedItem;

            if (clientEntry.Id == "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0") // MSIDLAB
            {
                cbxScopes.SelectedItem = "profile";
                authorityCbx.SelectedItem = "https://login.microsoftonline.com/common";
            }

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
            await Log("Expiring tokens.").ConfigureAwait(false);

            var pca = CreatePca();

            // do something that loads the cache first
            await pca.GetAccountsAsync().ConfigureAwait(false);

            var m = pca.UserTokenCache.GetType().GetRuntimeMethods().Where(n => n.Name == "ExpireAllAccessTokensForTestAsync");

            var task = pca.UserTokenCache.GetType()
                .GetRuntimeMethods()
                .Single(n => n.Name == "ExpireAllAccessTokensForTestAsync")
                .Invoke(pca.UserTokenCache, null);

            await (task as Task).ConfigureAwait(false);
           
            await Log("Done expiring tokens.").ConfigureAwait(false);
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

                await Log("Removed account " + acc.Username).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }
        }

        private void cbxScopes_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void sgnBtn_Click(object sender, EventArgs e)
        {
            const string correlationId = "1c4c45ab-4dfc-4891-ad98-cdc13ce265fb";
            string loginHint = GetLoginHint();
            string clientId = GetClientId();
            string authority = GetAuthority();
            string scopes = GetScopes(); //            "profile mail.read";//"[\"profile\"]";//GetScopes();
            IntPtr hwnd = this.Handle;

            AuthResult result = null;

            //Sign In
            try
            {
                using (var core = new msalruntime.Core())
                using (var authParams = new msalruntime.AuthParameters(clientId, authority))
                {
                    authParams.RequestedScopes = $"[\"{scopes}\"]";
                    authParams.RedirectUri = "about:blank";

                    using (result = await core.SignInAsync(hwnd, authParams, correlationId).ConfigureAwait(false))
                    {
                        await LogRuntimeResultAndRefreshAccountsAsync(result).ConfigureAwait(false);
                    }
                }
            }
            catch (msalruntime.Exception ex)
            {
                await Log("Exception: " + ex).ConfigureAwait(false);
            }
        }

        private async void readBtn_Click(object sender, EventArgs e)
        {
            await ReadAccountsAsync().ConfigureAwait(false);
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
