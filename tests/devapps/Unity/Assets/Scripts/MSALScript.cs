using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Threading;

public class MSALScript : MonoBehaviour
{
    public Text LogTextField;
    public Text DeviceCodeTextField;
    private readonly string clientId = "ebe2ab4d-12b3-4446-8480-5c3828d04c50";
    private readonly string redirectUrl = "https://login.microsoftonline.com/common/oauth2/nativeclient";
    private readonly string authority = "https://login.microsoftonline.com/common";
    private readonly List<string> scopes = new List<string>() { "User.Read" };
    private string _deviceCode = "-";

    // Start is called before the first frame update
    // https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    async void Start()
    {
        await Login();

    }

    // Update is called once per frame
    // https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    void Update()
    {
        DeviceCodeTextField.text = $"Device code: {_deviceCode}";
    }

    private async Task Login()
    {
        IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithRedirectUri(redirectUrl)
            .WithLogging((level, message, pii) => {
                Debug.Log($"MSAL [{level}] {pii} - {message}");
            }, LogLevel.Verbose, true)
            .Build();

        await GetToken(); // Acquires token with device code
        await GetToken(); // Acquires token silently

        async Task GetToken()
        {
            AuthenticationResult authResult = null;

            try
            {
                try
                {
                    var accounts = await app.GetAccountsAsync();
                    Log("MSAL acquiring token silently.");
                    authResult = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync(CancellationToken.None);
                    Log("MSAL acquired token silently.");
                }
                catch (MsalUiRequiredException)
                {
                    Log("MSAL acquiring token with device code.");
                    authResult = await app
                        .AcquireTokenWithDeviceCode(
                            scopes,
                            deviceCodeResult => {
                                _deviceCode = deviceCodeResult.UserCode;
                                //CopyToClipboard(deviceCodeResult.UserCode);

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync();
                    Log("MSAL acquired token with device code.");
                }
            }
            catch (Exception ex)
            {
                Log($"MSAL Exception acquiring token: {ex}");
            }

            if (authResult is null)
            {
                Log("MSAL auth result is null.");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"MSAL Username={authResult.Account.Username}");
                sb.AppendLine($"MSAL AccessToken={authResult.AccessToken}");
                sb.AppendLine($"MSAL IdToken={authResult.IdToken}");
                sb.AppendLine($"MSAL Account={authResult.Account}");
                sb.AppendLine($"MSAL ExpiresOn={authResult.ExpiresOn}");
                sb.AppendLine($"MSAL Scopes={string.Join(";", authResult.Scopes)}");
                sb.AppendLine($"MSAL Tenant={authResult.TenantId}");
                sb.AppendLine($"MSAL TokenSource={authResult.AuthenticationResultMetadata.TokenSource}");
                Log(sb.ToString(), false);
            }
        }
    }

    private void Log(string message, bool addToDebugLog = true)
    {
        LogTextField.text += message + Environment.NewLine;
        if (addToDebugLog)
        {
            Debug.Log(message);
        }
    }

    private void CopyToClipboard(string text)
    {
        // Must be called from a GUI thread
        // https://stackoverflow.com/questions/41330771/use-unity-api-from-another-thread-or-call-a-function-in-the-main-thread
        GUIUtility.systemCopyBuffer = text;
    }
}
