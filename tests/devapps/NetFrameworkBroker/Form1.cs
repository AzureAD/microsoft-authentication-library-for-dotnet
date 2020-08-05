using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using Microsoft.Identity.Client;
using Windows.Storage.Streams;
using Windows.UI.WebUI;

namespace NetDesktopWinForms
{
    public partial class Form1 : Form
    {

        public static List<ClientEntry> s_clients = new List<ClientEntry>()
        {
            new ClientEntry() { Id = "1d18b3b0-251b-4714-a02a-9956cec86c2d", Name = "1d18b3b0-251b-4714-a02a-9956cec86c2d (App in 49f)"},
            new ClientEntry() { Id = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1", Name = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1 (VS)"},
            new ClientEntry() { Id = "655015be-5021-4afc-a683-a4223eb5d0e5", Name = "655015be-5021-4afc-a683-a4223eb5d0e5"}
        };

        public Form1()
        {
            InitializeComponent();
            var bindingSource1 = new BindingSource();
            bindingSource1.DataSource = s_clients;

            clientIdCbx.DataSource = bindingSource1.DataSource;

            clientIdCbx.DisplayMember = "Name";
            clientIdCbx.ValueMember = "Id";
        }

        public static readonly string UserCacheFile =
            System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.user.json";


        private IPublicClientApplication CreatePca()
        {
            string clientId = (this.clientIdCbx.SelectedItem as ClientEntry).Id;
            var pca = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(this.authorityCbx.Text)
                .WithBroker(this.useBrokerChk.Checked)
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

                LogResult(result);

            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }

        }

        private async Task<AuthenticationResult> RunAtsAsync(IPublicClientApplication pca)
        {
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            string loginHint = GetLoginHint();
            if (cbxUseAccount.Checked == true)
            {
                IAccount account =
                    string.IsNullOrEmpty(loginHint) ?
                        accounts.FirstOrDefault() :
                        accounts.First(aa => aa.Username.StartsWith(loginHint));
                Log($"ATS with IAccount for {account?.Username}");
                return await pca.AcquireTokenSilent(GetScopes(), account)
                    .ExecuteAsync()
                    .ConfigureAwait(false);


            }

            if (string.IsNullOrEmpty(loginHint))
            {
                Log($"ATS with no account or login hint ... will fail with UiRequiredEx");

                return await pca.AcquireTokenSilent(GetScopes(), (IAccount)null)
                .ExecuteAsync()
                .ConfigureAwait(false);
            }

            Log($"ATS with login hint: " + loginHint);
            return await pca.AcquireTokenSilent(GetScopes(), loginHint)
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

        private void LogResult(AuthenticationResult ar)
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

                LogResult(result);

            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        private async Task<AuthenticationResult> RunAtiAsync(IPublicClientApplication pca)
        {
            AuthenticationResult result = null;

            var builder = pca.AcquireTokenInteractive(GetScopes())
                .WithPrompt(GetPrompt());
            string loginHint = GetLoginHint();

            if (!string.IsNullOrEmpty(loginHint))
            {
                if (cbxUseAccount.Checked == true)
                {
                    var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                    var account = accounts.FirstOrDefault(aa => aa.Username.StartsWith(loginHint));
                    Log($"ATI WithAccount for account {account?.Username ?? "null" }");
                    builder = builder.WithAccount(account);
                }
                else
                {
                    builder = builder.WithLoginHint(loginHint);
                }
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

        private Prompt GetPrompt()
        {
            string prompt = null;
            promptCbx.Invoke((MethodInvoker)delegate
            {
                prompt = promptCbx.Text;
            });

            if (string.IsNullOrEmpty(prompt))
                return Prompt.SelectAccount;


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
                case "never":
                    return Prompt.Never;


                default:
                    throw new NotImplementedException();
            }
        }

        private async void accBtn_Click(object sender, EventArgs e)
        {
            var pca = CreatePca();
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            string msg = string.Join(
                " ",
                accounts.Select(acc => $"{acc.Username} {acc.Environment} {acc.HomeAccountId.TenantId}"));
            Log(msg);
        }

        private async void atsAtiBtn_Click(object sender, EventArgs e)
        {
            var pca = CreatePca();

            try
            {
                var result = await RunAtsAsync(pca).ConfigureAwait(false);

                LogResult(result);

            }
            catch (MsalUiRequiredException ex)
            {
                Log("UI required Exception! " + ex.ErrorCode + " " + ex.Message);
                try
                {
                    var result = await RunAtiAsync(pca).ConfigureAwait(false);
                    LogResult(result);
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
    }

    public class ClientEntry
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }
}
