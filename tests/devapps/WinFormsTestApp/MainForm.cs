// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;

namespace WinFormsTestApp;

public partial class MainForm : Form
{
    private IPublicClientApplication _pca = null!;
    private CheckBox _useEmbeddedWebViewCheckBox = null!;
    private Button _loginButton = null!;
    private TextBox _resultTextBox = null!;

    public MainForm()
    {
        InitializeComponent();
        InitializeMsal();
    }

    private void InitializeMsal()
    {
        _pca = PublicClientApplicationBuilder
            .Create("9c0cb93c-2744-45ee-8399-81688062c058")
            .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
            .WithRedirectUri("http://localhost")
            .WithWindowsEmbeddedBrowserSupport()
            .Build();
    }

    private void InitializeComponent()
    {
        Text = "MSAL.NET WinForms Test App";
        Size = new System.Drawing.Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;

        // Main layout panel
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };

        // Title label
        var titleLabel = new Label
        {
            Text = "MSAL.NET WinForms Test App",
            Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = false
        };

        // WebView option checkbox
        _useEmbeddedWebViewCheckBox = new CheckBox
        {
            Text = "Use Embedded WebView",
            AutoSize = true,
            Anchor = AnchorStyles.None
        };

        var checkBoxPanel = new Panel
        {
            Height = 30,
            Dock = DockStyle.Fill
        };
        checkBoxPanel.Controls.Add(_useEmbeddedWebViewCheckBox);
        _useEmbeddedWebViewCheckBox.Left = (checkBoxPanel.Width - _useEmbeddedWebViewCheckBox.Width) / 2;
        _useEmbeddedWebViewCheckBox.Top = (checkBoxPanel.Height - _useEmbeddedWebViewCheckBox.Height) / 2;

        // Login button
        _loginButton = new Button
        {
            Text = "Login with MSAL",
            Size = new System.Drawing.Size(150, 40),
            Anchor = AnchorStyles.None
        };
        _loginButton.Click += LoginButton_Click;

        var buttonPanel = new Panel
        {
            Height = 50,
            Dock = DockStyle.Fill
        };
        buttonPanel.Controls.Add(_loginButton);
        _loginButton.Left = (buttonPanel.Width - _loginButton.Width) / 2;
        _loginButton.Top = (buttonPanel.Height - _loginButton.Height) / 2;

        // Result text box
        _resultTextBox = new TextBox
        {
            Text = "Click login to test MSAL authentication",
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new System.Drawing.Font("Consolas", 9F)
        };

        // Add controls to main panel
        mainPanel.Controls.Add(titleLabel, 0, 0);
        mainPanel.Controls.Add(checkBoxPanel, 0, 1);
        mainPanel.Controls.Add(buttonPanel, 0, 2);
        mainPanel.Controls.Add(_resultTextBox, 0, 3);

        // Set row styles
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Handle resize to center controls
        buttonPanel.Resize += (s, e) =>
        {
            _loginButton.Left = (buttonPanel.Width - _loginButton.Width) / 2;
        };

        checkBoxPanel.Resize += (s, e) =>
        {
            _useEmbeddedWebViewCheckBox.Left = (checkBoxPanel.Width - _useEmbeddedWebViewCheckBox.Width) / 2;
        };

        Controls.Add(mainPanel);
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        _loginButton.Enabled = false;
        bool useEmbeddedWebView = _useEmbeddedWebViewCheckBox.Checked;
        string authMethod = useEmbeddedWebView ? "Embedded WebView" : "System Browser";

        try
        {
            _resultTextBox.Text = $"Authenticating using {authMethod}...";

            var authBuilder = _pca.AcquireTokenInteractive(new[] { "user.read" });

            if (useEmbeddedWebView)
            {
                // Use embedded WebView with WinForms parent window
                authBuilder = authBuilder.WithUseEmbeddedWebView(true)
                                         .WithParentActivityOrWindow(this);
            }
            else
            {
                // Use system browser (default behavior)
                authBuilder = authBuilder.WithUseEmbeddedWebView(false);
            }

            var result = await authBuilder
                .ExecuteAsync()
                .ConfigureAwait(false);

            // Update UI on the UI thread
            this.Invoke(new Action(() =>
            {
                _resultTextBox.Text = $"🎉 Authentication Successful! (via {authMethod})\n\n" +
                                    $"User: {result.Account.Username}\n" +
                                    $"Display Name: {result.Account.GetTenantProfiles()?.FirstOrDefault()?.ClaimsPrincipal?.FindFirst("name")?.Value ?? "N/A"}\n" +
                                    $"Tenant ID: {result.TenantId}\n" +
                                    $"Account ID: {result.Account.HomeAccountId?.Identifier}\n" +
                                    $"Token Type: {result.TokenType}\n" +
                                    $"Expires On: {result.ExpiresOn:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"Scopes: {string.Join(", ", result.Scopes)}\n" +
                                    $"Access Token (first 50 chars): {result.AccessToken.Substring(0, Math.Min(50, result.AccessToken.Length))}...\n" +
                                    $"Correlation ID: {result.CorrelationId}";
            }));
        }
        catch (Exception ex)
        {
            this.Invoke(new Action(() =>
            {
                _resultTextBox.Text = $"Authentication Failed! (via {authMethod})\n\nError: {ex.Message}\n\nException Type: {ex.GetType().Name}";
            }));
        }
        finally
        {
            this.Invoke(new Action(() =>
            {
                _loginButton.Enabled = true;
            }));
        }
    }
}
