namespace DesktopTestApp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.authority = new System.Windows.Forms.TextBox();
            this.extraQueryParams = new System.Windows.Forms.TextBox();
            this.environmentQP = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.publicClientTabPage = new System.Windows.Forms.TabPage();
            this.scopes = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.never = new System.Windows.Forms.RadioButton();
            this.consent = new System.Windows.Forms.RadioButton();
            this.forceLogin = new System.Windows.Forms.RadioButton();
            this.selectAccount = new System.Windows.Forms.RadioButton();
            this.callResult = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.userList = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.acquireTokenSilent = new System.Windows.Forms.Button();
            this.acquireTokenInteractive = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.validateAuthorityDisabled = new System.Windows.Forms.RadioButton();
            this.validateAuthorityEnabled = new System.Windows.Forms.RadioButton();
            this.loginHint = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.overriddenAuthority = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.settingsTabPage = new System.Windows.Forms.TabPage();
            this.label11 = new System.Windows.Forms.Label();
            this.applySettings = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            this.cacheTabPage = new System.Windows.Forms.TabPage();
            this.logsTabPage = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.msalPIILogs = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.msalLogs = new System.Windows.Forms.TextBox();
            this.confidentialClientTabPage = new System.Windows.Forms.TabPage();
            this.publicClient = new System.Windows.Forms.Button();
            this.settings = new System.Windows.Forms.Button();
            this.cache = new System.Windows.Forms.Button();
            this.logs = new System.Windows.Forms.Button();
            this.confidentialClient = new System.Windows.Forms.Button();
            this.AccessToken = new System.Windows.Forms.Label();
            this.AccessTokenResult = new System.Windows.Forms.Label();
            this.ExpiresOn = new System.Windows.Forms.Label();
            this.ExpiresOnResult = new System.Windows.Forms.Label();
            this.TenantId = new System.Windows.Forms.Label();
            this.TenantIdResult = new System.Windows.Forms.Label();
            this.User = new System.Windows.Forms.Label();
            this.UserResult = new System.Windows.Forms.Label();
            this.IdToken = new System.Windows.Forms.Label();
            this.IdTokenResult = new System.Windows.Forms.Label();
            this.Scope = new System.Windows.Forms.Label();
            this.ScopeResult = new System.Windows.Forms.ListBox();
            this.tabControl1.SuspendLayout();
            this.publicClientTabPage.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.settingsTabPage.SuspendLayout();
            this.logsTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // authority
            // 
            this.authority.AccessibleName = "authority";
            this.authority.Location = new System.Drawing.Point(256, 19);
            this.authority.Name = "authority";
            this.authority.Size = new System.Drawing.Size(352, 20);
            this.authority.TabIndex = 16;
            this.authority.Text = "https://login.microsoftonline.com/common";
            // 
            // extraQueryParams
            // 
            this.extraQueryParams.AccessibleName = "";
            this.extraQueryParams.Location = new System.Drawing.Point(248, 90);
            this.extraQueryParams.Name = "extraQueryParams";
            this.extraQueryParams.Size = new System.Drawing.Size(352, 20);
            this.extraQueryParams.TabIndex = 21;
            // 
            // environmentQP
            // 
            this.environmentQP.AccessibleName = "";
            this.environmentQP.Location = new System.Drawing.Point(248, 32);
            this.environmentQP.Name = "environmentQP";
            this.environmentQP.Size = new System.Drawing.Size(352, 20);
            this.environmentQP.TabIndex = 18;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.publicClientTabPage);
            this.tabControl1.Controls.Add(this.settingsTabPage);
            this.tabControl1.Controls.Add(this.cacheTabPage);
            this.tabControl1.Controls.Add(this.logsTabPage);
            this.tabControl1.Controls.Add(this.confidentialClientTabPage);
            this.tabControl1.Location = new System.Drawing.Point(1, 1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(676, 814);
            this.tabControl1.TabIndex = 0;
            // 
            // publicClientTabPage
            // 
            this.publicClientTabPage.BackColor = System.Drawing.SystemColors.Control;
            this.publicClientTabPage.Controls.Add(this.ScopeResult);
            this.publicClientTabPage.Controls.Add(this.Scope);
            this.publicClientTabPage.Controls.Add(this.IdTokenResult);
            this.publicClientTabPage.Controls.Add(this.IdToken);
            this.publicClientTabPage.Controls.Add(this.UserResult);
            this.publicClientTabPage.Controls.Add(this.User);
            this.publicClientTabPage.Controls.Add(this.TenantIdResult);
            this.publicClientTabPage.Controls.Add(this.TenantId);
            this.publicClientTabPage.Controls.Add(this.ExpiresOnResult);
            this.publicClientTabPage.Controls.Add(this.ExpiresOn);
            this.publicClientTabPage.Controls.Add(this.AccessTokenResult);
            this.publicClientTabPage.Controls.Add(this.AccessToken);
            this.publicClientTabPage.Controls.Add(this.authority);
            this.publicClientTabPage.Controls.Add(this.scopes);
            this.publicClientTabPage.Controls.Add(this.label9);
            this.publicClientTabPage.Controls.Add(this.groupBox2);
            this.publicClientTabPage.Controls.Add(this.callResult);
            this.publicClientTabPage.Controls.Add(this.label8);
            this.publicClientTabPage.Controls.Add(this.userList);
            this.publicClientTabPage.Controls.Add(this.label7);
            this.publicClientTabPage.Controls.Add(this.acquireTokenSilent);
            this.publicClientTabPage.Controls.Add(this.acquireTokenInteractive);
            this.publicClientTabPage.Controls.Add(this.groupBox1);
            this.publicClientTabPage.Controls.Add(this.loginHint);
            this.publicClientTabPage.Controls.Add(this.label6);
            this.publicClientTabPage.Controls.Add(this.label5);
            this.publicClientTabPage.Controls.Add(this.overriddenAuthority);
            this.publicClientTabPage.Controls.Add(this.label4);
            this.publicClientTabPage.Controls.Add(this.label3);
            this.publicClientTabPage.Location = new System.Drawing.Point(8, 27);
            this.publicClientTabPage.Name = "publicClientTabPage";
            this.publicClientTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.publicClientTabPage.Size = new System.Drawing.Size(660, 779);
            this.publicClientTabPage.TabIndex = 0;
            this.publicClientTabPage.Text = "publicClientTabPage";
            // 
            // scopes
            // 
            this.scopes.Location = new System.Drawing.Point(256, 310);
            this.scopes.Name = "scopes";
            this.scopes.Size = new System.Drawing.Size(352, 20);
            this.scopes.TabIndex = 15;
            this.scopes.Text = "mail.read";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(8, 312);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(146, 44);
            this.label9.TabIndex = 14;
            this.label9.Text = "Scopes";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.never);
            this.groupBox2.Controls.Add(this.consent);
            this.groupBox2.Controls.Add(this.forceLogin);
            this.groupBox2.Controls.Add(this.selectAccount);
            this.groupBox2.Location = new System.Drawing.Point(256, 154);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(332, 51);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            // 
            // never
            // 
            this.never.AutoSize = true;
            this.never.Location = new System.Drawing.Point(267, 18);
            this.never.Name = "never";
            this.never.Size = new System.Drawing.Size(67, 26);
            this.never.TabIndex = 10;
            this.never.Text = "Never";
            this.never.UseVisualStyleBackColor = true;
            // 
            // consent
            // 
            this.consent.AutoSize = true;
            this.consent.Location = new System.Drawing.Point(197, 18);
            this.consent.Name = "consent";
            this.consent.Size = new System.Drawing.Size(77, 26);
            this.consent.TabIndex = 9;
            this.consent.Text = "Consent";
            this.consent.UseVisualStyleBackColor = true;
            // 
            // forceLogin
            // 
            this.forceLogin.AutoSize = true;
            this.forceLogin.Location = new System.Drawing.Point(110, 18);
            this.forceLogin.Name = "forceLogin";
            this.forceLogin.Size = new System.Drawing.Size(94, 26);
            this.forceLogin.TabIndex = 8;
            this.forceLogin.Text = "Force Login";
            this.forceLogin.UseVisualStyleBackColor = true;
            // 
            // selectAccount
            // 
            this.selectAccount.AutoSize = true;
            this.selectAccount.Checked = true;
            this.selectAccount.Location = new System.Drawing.Point(6, 19);
            this.selectAccount.Name = "selectAccount";
            this.selectAccount.Size = new System.Drawing.Size(111, 26);
            this.selectAccount.TabIndex = 7;
            this.selectAccount.TabStop = true;
            this.selectAccount.Text = "Select Account";
            this.selectAccount.UseVisualStyleBackColor = true;
            // 
            // callResult
            // 
            this.callResult.Location = new System.Drawing.Point(12, 352);
            this.callResult.Multiline = true;
            this.callResult.Name = "callResult";
            this.callResult.ReadOnly = true;
            this.callResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.callResult.Size = new System.Drawing.Size(645, 353);
            this.callResult.TabIndex = 13;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(8, 172);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(217, 44);
            this.label8.TabIndex = 9;
            this.label8.Text = "UI Behavior";
            // 
            // userList
            // 
            this.userList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.userList.FormattingEnabled = true;
            this.userList.Location = new System.Drawing.Point(256, 266);
            this.userList.Name = "userList";
            this.userList.Size = new System.Drawing.Size(352, 21);
            this.userList.TabIndex = 12;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(8, 266);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 44);
            this.label7.TabIndex = 11;
            this.label7.Text = "User";
            // 
            // acquireTokenSilent
            // 
            this.acquireTokenSilent.Location = new System.Drawing.Point(444, 711);
            this.acquireTokenSilent.Name = "acquireTokenSilent";
            this.acquireTokenSilent.Size = new System.Drawing.Size(164, 46);
            this.acquireTokenSilent.TabIndex = 10;
            this.acquireTokenSilent.Text = "Acquire Token Silent";
            this.acquireTokenSilent.UseVisualStyleBackColor = true;
            //this.acquireTokenSilent.Click += new System.EventHandler(this.acquireTokenSilent_Click);
            // 
            // acquireTokenInteractive
            // 
            this.acquireTokenInteractive.Location = new System.Drawing.Point(42, 711);
            this.acquireTokenInteractive.Name = "acquireTokenInteractive";
            this.acquireTokenInteractive.Size = new System.Drawing.Size(164, 46);
            this.acquireTokenInteractive.TabIndex = 9;
            this.acquireTokenInteractive.Text = "Acquire Token Interactive";
            this.acquireTokenInteractive.UseVisualStyleBackColor = true;
            this.acquireTokenInteractive.Click += new System.EventHandler(this.acquireTokenInteractive_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.validateAuthorityDisabled);
            this.groupBox1.Controls.Add(this.validateAuthorityEnabled);
            this.groupBox1.Location = new System.Drawing.Point(256, 91);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(205, 51);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            // 
            // validateAuthorityDisabled
            // 
            this.validateAuthorityDisabled.AutoSize = true;
            this.validateAuthorityDisabled.Location = new System.Drawing.Point(134, 18);
            this.validateAuthorityDisabled.Name = "validateAuthorityDisabled";
            this.validateAuthorityDisabled.Size = new System.Drawing.Size(79, 26);
            this.validateAuthorityDisabled.TabIndex = 8;
            this.validateAuthorityDisabled.Text = "Disabled";
            this.validateAuthorityDisabled.UseVisualStyleBackColor = true;
            // 
            // validateAuthorityEnabled
            // 
            this.validateAuthorityEnabled.AutoSize = true;
            this.validateAuthorityEnabled.Checked = true;
            this.validateAuthorityEnabled.Location = new System.Drawing.Point(6, 19);
            this.validateAuthorityEnabled.Name = "validateAuthorityEnabled";
            this.validateAuthorityEnabled.Size = new System.Drawing.Size(77, 26);
            this.validateAuthorityEnabled.TabIndex = 7;
            this.validateAuthorityEnabled.TabStop = true;
            this.validateAuthorityEnabled.Text = "Enabled";
            this.validateAuthorityEnabled.UseVisualStyleBackColor = true;
            // 
            // loginHint
            // 
            this.loginHint.Location = new System.Drawing.Point(256, 226);
            this.loginHint.Name = "loginHint";
            this.loginHint.Size = new System.Drawing.Size(352, 20);
            this.loginHint.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(8, 228);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(191, 44);
            this.label6.TabIndex = 5;
            this.label6.Text = "Login Hint";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(8, 109);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(319, 44);
            this.label5.TabIndex = 4;
            this.label5.Text = "Validate Authority";
            // 
            // overridenAuthority
            // 
            this.overriddenAuthority.Location = new System.Drawing.Point(256, 59);
            this.overriddenAuthority.Name = "overriddenAuthority";
            this.overriddenAuthority.Size = new System.Drawing.Size(352, 20);
            this.overriddenAuthority.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(8, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(503, 44);
            this.label4.TabIndex = 2;
            this.label4.Text = "Overriden Authority for 1 call";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(8, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(171, 44);
            this.label3.TabIndex = 0;
            this.label3.Text = "Authority";
            // 
            // settingsTabPage
            // 
            this.settingsTabPage.Controls.Add(this.extraQueryParams);
            this.settingsTabPage.Controls.Add(this.label11);
            this.settingsTabPage.Controls.Add(this.applySettings);
            this.settingsTabPage.Controls.Add(this.environmentQP);
            this.settingsTabPage.Controls.Add(this.label10);
            this.settingsTabPage.Location = new System.Drawing.Point(8, 27);
            this.settingsTabPage.Name = "settingsTabPage";
            this.settingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.settingsTabPage.Size = new System.Drawing.Size(660, 779);
            this.settingsTabPage.TabIndex = 1;
            this.settingsTabPage.Text = "settingsTabPage";
            this.settingsTabPage.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(10, 78);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(365, 44);
            this.label11.TabIndex = 20;
            this.label11.Text = "Extra Query Params";
            // 
            // applySettings
            // 
            this.applySettings.Location = new System.Drawing.Point(437, 704);
            this.applySettings.Name = "applySettings";
            this.applySettings.Size = new System.Drawing.Size(140, 46);
            this.applySettings.TabIndex = 19;
            this.applySettings.Text = "Apply";
            this.applySettings.UseVisualStyleBackColor = true;
            this.applySettings.Click += new System.EventHandler(this.applySettings_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(10, 20);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(300, 44);
            this.label10.TabIndex = 17;
            this.label10.Text = "Environment QP";
            // 
            // cacheTabPage
            // 
            this.cacheTabPage.Location = new System.Drawing.Point(8, 27);
            this.cacheTabPage.Name = "cacheTabPage";
            this.cacheTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.cacheTabPage.Size = new System.Drawing.Size(660, 779);
            this.cacheTabPage.TabIndex = 2;
            this.cacheTabPage.Text = "cacheTabPage";
            this.cacheTabPage.UseVisualStyleBackColor = true;
            // 
            // logsTabPage
            // 
            this.logsTabPage.Controls.Add(this.button1);
            this.logsTabPage.Controls.Add(this.msalPIILogs);
            this.logsTabPage.Controls.Add(this.label2);
            this.logsTabPage.Controls.Add(this.label1);
            this.logsTabPage.Controls.Add(this.msalLogs);
            this.logsTabPage.Location = new System.Drawing.Point(8, 27);
            this.logsTabPage.Name = "logsTabPage";
            this.logsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.logsTabPage.Size = new System.Drawing.Size(660, 779);
            this.logsTabPage.TabIndex = 3;
            this.logsTabPage.Text = "logsTabPage";
            this.logsTabPage.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(223, 692);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(151, 43);
            this.button1.TabIndex = 4;
            this.button1.Text = "Clear Logs";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // msalPIILogs
            // 
            this.msalPIILogs.Location = new System.Drawing.Point(7, 365);
            this.msalPIILogs.Multiline = true;
            this.msalPIILogs.Name = "msalPIILogs";
            this.msalPIILogs.ReadOnly = true;
            this.msalPIILogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalPIILogs.Size = new System.Drawing.Size(655, 304);
            this.msalPIILogs.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(143, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(336, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "========================= Logs =========================";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(143, 349);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(352, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "========================= PII Logs =========================";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // msalLogs
            // 
            this.msalLogs.Location = new System.Drawing.Point(7, 21);
            this.msalLogs.Multiline = true;
            this.msalLogs.Name = "msalLogs";
            this.msalLogs.ReadOnly = true;
            this.msalLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalLogs.Size = new System.Drawing.Size(655, 304);
            this.msalLogs.TabIndex = 0;
            // 
            // confidentialClientTabPage
            // 
            this.confidentialClientTabPage.Location = new System.Drawing.Point(8, 27);
            this.confidentialClientTabPage.Name = "confidentialClientTabPage";
            this.confidentialClientTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.confidentialClientTabPage.Size = new System.Drawing.Size(660, 779);
            this.confidentialClientTabPage.TabIndex = 4;
            this.confidentialClientTabPage.Text = "confidentialClientTabPage";
            this.confidentialClientTabPage.UseVisualStyleBackColor = true;
            // 
            // publicClient
            // 
            this.publicClient.Location = new System.Drawing.Point(1, 817);
            this.publicClient.Name = "publicClient";
            this.publicClient.Size = new System.Drawing.Size(97, 45);
            this.publicClient.TabIndex = 1;
            this.publicClient.Text = "Public Client";
            this.publicClient.UseVisualStyleBackColor = true;
            this.publicClient.Click += new System.EventHandler(this.acquire_Click);
            // 
            // settings
            // 
            this.settings.Location = new System.Drawing.Point(307, 817);
            this.settings.Name = "settings";
            this.settings.Size = new System.Drawing.Size(92, 45);
            this.settings.TabIndex = 2;
            this.settings.Text = "Settings";
            this.settings.UseVisualStyleBackColor = true;
            this.settings.Click += new System.EventHandler(this.settings_Click);
            // 
            // cache
            // 
            this.cache.Location = new System.Drawing.Point(434, 817);
            this.cache.Name = "cache";
            this.cache.Size = new System.Drawing.Size(110, 45);
            this.cache.TabIndex = 3;
            this.cache.Text = "Cache";
            this.cache.UseVisualStyleBackColor = true;
            this.cache.Click += new System.EventHandler(this.cache_Click);
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(581, 817);
            this.logs.Name = "logs";
            this.logs.Size = new System.Drawing.Size(92, 45);
            this.logs.TabIndex = 4;
            this.logs.Text = "Logs";
            this.logs.UseVisualStyleBackColor = true;
            this.logs.Click += new System.EventHandler(this.logs_Click);
            // 
            // confidentialClient
            // 
            this.confidentialClient.Location = new System.Drawing.Point(147, 817);
            this.confidentialClient.Name = "confidentialClient";
            this.confidentialClient.Size = new System.Drawing.Size(115, 45);
            this.confidentialClient.TabIndex = 5;
            this.confidentialClient.Text = "Confidential Client";
            this.confidentialClient.UseVisualStyleBackColor = true;
            this.confidentialClient.Click += new System.EventHandler(this.confidentialClient_Click);
            // 
            // AccessToken
            // 
            this.AccessToken.AutoSize = true;
            this.AccessToken.Location = new System.Drawing.Point(32, 372);
            this.AccessToken.Name = "AccessToken";
            this.AccessToken.Size = new System.Drawing.Size(76, 13);
            this.AccessToken.TabIndex = 17;
            this.AccessToken.Text = "Access Token";
            // 
            // AccessTokenResult
            // 
            this.AccessTokenResult.AutoSize = true;
            this.AccessTokenResult.Location = new System.Drawing.Point(132, 368);
            this.AccessTokenResult.Name = "AccessTokenResult";
            this.AccessTokenResult.Size = new System.Drawing.Size(135, 13);
            this.AccessTokenResult.TabIndex = 18;
            this.AccessTokenResult.Text = "Access Token Placeholder";
            // 
            // ExpiresOn
            // 
            this.ExpiresOn.AutoSize = true;
            this.ExpiresOn.Location = new System.Drawing.Point(34, 406);
            this.ExpiresOn.Name = "ExpiresOn";
            this.ExpiresOn.Size = new System.Drawing.Size(58, 13);
            this.ExpiresOn.TabIndex = 19;
            this.ExpiresOn.Text = "Expires On";
            // 
            // ExpiresOnResult
            // 
            this.ExpiresOnResult.AutoSize = true;
            this.ExpiresOnResult.Location = new System.Drawing.Point(132, 406);
            this.ExpiresOnResult.Name = "ExpiresOnResult";
            this.ExpiresOnResult.Size = new System.Drawing.Size(117, 13);
            this.ExpiresOnResult.TabIndex = 20;
            this.ExpiresOnResult.Text = "Expires On Placeholder";
            // 
            // TenantId
            // 
            this.TenantId.AutoSize = true;
            this.TenantId.Location = new System.Drawing.Point(32, 438);
            this.TenantId.Name = "TenantId";
            this.TenantId.Size = new System.Drawing.Size(53, 13);
            this.TenantId.TabIndex = 21;
            this.TenantId.Text = "Tenant Id";
            // 
            // TenantIdResult
            // 
            this.TenantIdResult.AutoSize = true;
            this.TenantIdResult.Location = new System.Drawing.Point(132, 438);
            this.TenantIdResult.Name = "TenantIdResult";
            this.TenantIdResult.Size = new System.Drawing.Size(112, 13);
            this.TenantIdResult.TabIndex = 22;
            this.TenantIdResult.Text = "Tenant Id Placeholder";
            // 
            // User
            // 
            this.User.AutoSize = true;
            this.User.Location = new System.Drawing.Point(34, 472);
            this.User.Name = "User";
            this.User.Size = new System.Drawing.Size(29, 13);
            this.User.TabIndex = 23;
            this.User.Text = "User";
            // 
            // UserResult
            // 
            this.UserResult.AutoSize = true;
            this.UserResult.Location = new System.Drawing.Point(132, 472);
            this.UserResult.Name = "UserResult";
            this.UserResult.Size = new System.Drawing.Size(88, 13);
            this.UserResult.TabIndex = 24;
            this.UserResult.Text = "User Placeholder";
            // 
            // IdToken
            // 
            this.IdToken.AutoSize = true;
            this.IdToken.Location = new System.Drawing.Point(34, 504);
            this.IdToken.Name = "IdToken";
            this.IdToken.Size = new System.Drawing.Size(50, 13);
            this.IdToken.TabIndex = 25;
            this.IdToken.Text = "Id Token";
            // 
            // IdTokenResult
            // 
            this.IdTokenResult.AutoSize = true;
            this.IdTokenResult.Location = new System.Drawing.Point(132, 504);
            this.IdTokenResult.Name = "IdTokenResult";
            this.IdTokenResult.Size = new System.Drawing.Size(109, 13);
            this.IdTokenResult.TabIndex = 26;
            this.IdTokenResult.Text = "Id Token Placeholder";
            // 
            // Scope
            // 
            this.Scope.AutoSize = true;
            this.Scope.Location = new System.Drawing.Point(34, 532);
            this.Scope.Name = "Scope";
            this.Scope.Size = new System.Drawing.Size(38, 13);
            this.Scope.TabIndex = 27;
            this.Scope.Text = "Scope";
            // 
            // ScopeResult
            // 
            this.ScopeResult.FormattingEnabled = true;
            this.ScopeResult.Location = new System.Drawing.Point(134, 534);
            this.ScopeResult.Name = "ScopeResult";
            this.ScopeResult.Size = new System.Drawing.Size(150, 69);
            this.ScopeResult.TabIndex = 28;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(674, 863);
            this.Controls.Add(this.confidentialClient);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.cache);
            this.Controls.Add(this.settings);
            this.Controls.Add(this.publicClient);
            this.Controls.Add(this.tabControl1);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Dev Utility Test App";
            this.tabControl1.ResumeLayout(false);
            this.publicClientTabPage.ResumeLayout(false);
            this.publicClientTabPage.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.settingsTabPage.ResumeLayout(false);
            this.settingsTabPage.PerformLayout();
            this.logsTabPage.ResumeLayout(false);
            this.logsTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage publicClientTabPage;
        private System.Windows.Forms.TabPage settingsTabPage;
        private System.Windows.Forms.Button publicClient;
        private System.Windows.Forms.Button settings;
        private System.Windows.Forms.Button cache;
        private System.Windows.Forms.Button logs;
        private System.Windows.Forms.TabPage cacheTabPage;
        private System.Windows.Forms.TabPage logsTabPage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox msalLogs;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox msalPIILogs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox overriddenAuthority;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox loginHint;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton validateAuthorityEnabled;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton validateAuthorityDisabled;
        private System.Windows.Forms.Button acquireTokenSilent;
        private System.Windows.Forms.Button acquireTokenInteractive;
        private System.Windows.Forms.ComboBox userList;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox callResult;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton forceLogin;
        private System.Windows.Forms.RadioButton selectAccount;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.RadioButton never;
        private System.Windows.Forms.RadioButton consent;
        private System.Windows.Forms.TextBox scopes;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button applySettings;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button confidentialClient;
        private System.Windows.Forms.TabPage confidentialClientTabPage;
        private System.Windows.Forms.TextBox authority;
        private System.Windows.Forms.TextBox extraQueryParams;
        private System.Windows.Forms.TextBox environmentQP;
        private System.Windows.Forms.Label AccessTokenResult;
        private System.Windows.Forms.Label AccessToken;
        private System.Windows.Forms.Label Scope;
        private System.Windows.Forms.Label IdTokenResult;
        private System.Windows.Forms.Label IdToken;
        private System.Windows.Forms.Label UserResult;
        private System.Windows.Forms.Label User;
        private System.Windows.Forms.Label TenantIdResult;
        private System.Windows.Forms.Label TenantId;
        private System.Windows.Forms.Label ExpiresOnResult;
        private System.Windows.Forms.Label ExpiresOn;
        private System.Windows.Forms.ListBox ScopeResult;
    }
}

