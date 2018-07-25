using System.Windows.Forms;

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
            this.publicClient = new System.Windows.Forms.Button();
            this.settings = new System.Windows.Forms.Button();
            this.cache = new System.Windows.Forms.Button();
            this.logs = new System.Windows.Forms.Button();
            this.logsTabPage = new System.Windows.Forms.TabPage();
            this.clearLogsButton = new System.Windows.Forms.Button();
            this.msalPIILogsTextBox = new System.Windows.Forms.TextBox();
            this.msalLogsTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cacheTabPage = new System.Windows.Forms.TabPage();
            this.cachePageTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.settingsTabPage = new System.Windows.Forms.TabPage();
            this.logLevel = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.validateAuthorityDisabled = new System.Windows.Forms.RadioButton();
            this.validateAuthorityEnabled = new System.Windows.Forms.RadioButton();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.PiiLoggingDisabled = new System.Windows.Forms.RadioButton();
            this.PiiLoggingEnabled = new System.Windows.Forms.RadioButton();
            this.label12 = new System.Windows.Forms.Label();
            this.extraQueryParams = new System.Windows.Forms.TextBox();
            this.environmentQP = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.publicClientTabPage = new System.Windows.Forms.TabPage();
            this.acquireTokenSilentAuthority = new System.Windows.Forms.Button();
            this.acquireTokenInteractiveAuthority = new System.Windows.Forms.Button();
            this.PiiLoggingLabel = new System.Windows.Forms.Label();
            this.authority = new System.Windows.Forms.TextBox();
            this.scopes = new System.Windows.Forms.TextBox();
            this.callResult = new System.Windows.Forms.TextBox();
            this.loginHintTextBox = new System.Windows.Forms.TextBox();
            this.overriddenAuthority = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.never = new System.Windows.Forms.RadioButton();
            this.consent = new System.Windows.Forms.RadioButton();
            this.forceLogin = new System.Windows.Forms.RadioButton();
            this.selectAccount = new System.Windows.Forms.RadioButton();
            this.label8 = new System.Windows.Forms.Label();
            this.userList = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.acquireTokenSilent = new System.Windows.Forms.Button();
            this.acquireTokenWIA = new System.Windows.Forms.Button();
            this.acquireTokenInteractive = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.logsTabPage.SuspendLayout();
            this.cacheTabPage.SuspendLayout();
            this.settingsTabPage.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.publicClientTabPage.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            //
            // publicClient
            //
            this.publicClient.Location = new System.Drawing.Point(1, 817);
            this.publicClient.Name = "publicClient";
            this.publicClient.Size = new System.Drawing.Size(116, 45);
            this.publicClient.TabIndex = 1;
            this.publicClient.Text = "Public Client";
            this.publicClient.UseVisualStyleBackColor = true;
            this.publicClient.Click += new System.EventHandler(this.acquire_Click);
            //
            // settings
            //
            this.settings.Location = new System.Drawing.Point(200, 817);
            this.settings.Name = "settings";
            this.settings.Size = new System.Drawing.Size(96, 45);
            this.settings.TabIndex = 2;
            this.settings.Text = "Settings";
            this.settings.UseVisualStyleBackColor = true;
            this.settings.Click += new System.EventHandler(this.settings_Click);
            //
            // cache
            //
            this.cache.Location = new System.Drawing.Point(387, 817);
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
            // logsTabPage
            //
            this.logsTabPage.Controls.Add(this.clearLogsButton);
            this.logsTabPage.Controls.Add(this.msalPIILogsTextBox);
            this.logsTabPage.Controls.Add(this.msalLogsTextBox);
            this.logsTabPage.Controls.Add(this.label2);
            this.logsTabPage.Controls.Add(this.label1);
            this.logsTabPage.Location = new System.Drawing.Point(4, 22);
            this.logsTabPage.Name = "logsTabPage";
            this.logsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.logsTabPage.Size = new System.Drawing.Size(668, 788);
            this.logsTabPage.TabIndex = 3;
            this.logsTabPage.Text = "logsTabPage";
            this.logsTabPage.UseVisualStyleBackColor = true;
            //
            // clearLogsButton
            //
            this.clearLogsButton.Location = new System.Drawing.Point(223, 692);
            this.clearLogsButton.Name = "clearLogsButton";
            this.clearLogsButton.Size = new System.Drawing.Size(151, 43);
            this.clearLogsButton.TabIndex = 4;
            this.clearLogsButton.Text = "Clear Logs";
            this.clearLogsButton.UseVisualStyleBackColor = true;
            this.clearLogsButton.Click += new System.EventHandler(this.clearLogsButton_Click);
            //
            // msalPIILogsTextBox
            //
            this.msalPIILogsTextBox.Location = new System.Drawing.Point(7, 365);
            this.msalPIILogsTextBox.Multiline = true;
            this.msalPIILogsTextBox.Name = "msalPIILogsTextBox";
            this.msalPIILogsTextBox.ReadOnly = true;
            this.msalPIILogsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalPIILogsTextBox.Size = new System.Drawing.Size(655, 304);
            this.msalPIILogsTextBox.TabIndex = 3;
            //
            // msalLogsTextBox
            //
            this.msalLogsTextBox.Location = new System.Drawing.Point(7, 21);
            this.msalLogsTextBox.Multiline = true;
            this.msalLogsTextBox.Name = "msalLogsTextBox";
            this.msalLogsTextBox.ReadOnly = true;
            this.msalLogsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalLogsTextBox.Size = new System.Drawing.Size(655, 304);
            this.msalLogsTextBox.TabIndex = 0;
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(143, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(336, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "========================= Logs =========================";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(143, 349);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(352, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "========================= PII Logs =========================";
            //
            // cacheTabPage
            //
            this.cacheTabPage.AutoScroll = true;
            this.cacheTabPage.Controls.Add(this.cachePageTableLayout);
            this.cacheTabPage.Location = new System.Drawing.Point(4, 22);
            this.cacheTabPage.Name = "cacheTabPage";
            this.cacheTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.cacheTabPage.Size = new System.Drawing.Size(668, 788);
            this.cacheTabPage.TabIndex = 2;
            this.cacheTabPage.Text = "cacheTabPage";
            this.cacheTabPage.UseVisualStyleBackColor = true;
            //
            // cachePageTableLayout
            //
            this.cachePageTableLayout.AutoSize = true;
            this.cachePageTableLayout.ColumnCount = 1;
            this.cachePageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.cachePageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.cachePageTableLayout.Location = new System.Drawing.Point(8, 7);
            this.cachePageTableLayout.Name = "cachePageTableLayout";
            this.cachePageTableLayout.RowCount = 2;
            this.cachePageTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.cachePageTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.cachePageTableLayout.Size = new System.Drawing.Size(654, 100);
            this.cachePageTableLayout.TabIndex = 0;
            //
            // settingsTabPage
            //
            this.settingsTabPage.Controls.Add(this.logLevel);
            this.settingsTabPage.Controls.Add(this.label14);
            this.settingsTabPage.Controls.Add(this.groupBox1);
            this.settingsTabPage.Controls.Add(this.label5);
            this.settingsTabPage.Controls.Add(this.groupBox6);
            this.settingsTabPage.Controls.Add(this.label12);
            this.settingsTabPage.Controls.Add(this.extraQueryParams);
            this.settingsTabPage.Controls.Add(this.environmentQP);
            this.settingsTabPage.Controls.Add(this.label11);
            this.settingsTabPage.Controls.Add(this.label10);
            this.settingsTabPage.Location = new System.Drawing.Point(4, 22);
            this.settingsTabPage.Name = "settingsTabPage";
            this.settingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.settingsTabPage.Size = new System.Drawing.Size(668, 788);
            this.settingsTabPage.TabIndex = 1;
            this.settingsTabPage.Text = "settingsTabPage";
            this.settingsTabPage.UseVisualStyleBackColor = true;
            //
            // logLevel
            //
            this.logLevel.FormattingEnabled = true;
            this.logLevel.Items.AddRange(new object[] {
            "Error",
            "Warning",
            "Info",
            "Verbose"});
            this.logLevel.Location = new System.Drawing.Point(253, 194);
            this.logLevel.Name = "logLevel";
            this.logLevel.Size = new System.Drawing.Size(121, 21);
            this.logLevel.TabIndex = 41;
            //
            // label14
            //
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(11, 194);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(92, 24);
            this.label14.TabIndex = 40;
            this.label14.Text = "Log Level";
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.validateAuthorityDisabled);
            this.groupBox1.Controls.Add(this.validateAuthorityEnabled);
            this.groupBox1.Location = new System.Drawing.Point(259, 357);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(205, 51);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            //
            // validateAuthorityDisabled
            //
            this.validateAuthorityDisabled.AutoSize = true;
            this.validateAuthorityDisabled.Location = new System.Drawing.Point(134, 18);
            this.validateAuthorityDisabled.Name = "validateAuthorityDisabled";
            this.validateAuthorityDisabled.Size = new System.Drawing.Size(66, 17);
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
            this.validateAuthorityEnabled.Size = new System.Drawing.Size(64, 17);
            this.validateAuthorityEnabled.TabIndex = 7;
            this.validateAuthorityEnabled.TabStop = true;
            this.validateAuthorityEnabled.Text = "Enabled";
            this.validateAuthorityEnabled.UseVisualStyleBackColor = true;
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(11, 375);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(155, 24);
            this.label5.TabIndex = 38;
            this.label5.Text = "Validate Authority";
            //
            // groupBox6
            //
            this.groupBox6.Controls.Add(this.PiiLoggingDisabled);
            this.groupBox6.Controls.Add(this.PiiLoggingEnabled);
            this.groupBox6.Location = new System.Drawing.Point(253, 132);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(189, 42);
            this.groupBox6.TabIndex = 37;
            this.groupBox6.TabStop = false;
            //
            // PiiLoggingDisabled
            //
            this.PiiLoggingDisabled.AutoSize = true;
            this.PiiLoggingDisabled.Checked = true;
            this.PiiLoggingDisabled.Location = new System.Drawing.Point(109, 10);
            this.PiiLoggingDisabled.Name = "PiiLoggingDisabled";
            this.PiiLoggingDisabled.Size = new System.Drawing.Size(66, 17);
            this.PiiLoggingDisabled.TabIndex = 31;
            this.PiiLoggingDisabled.TabStop = true;
            this.PiiLoggingDisabled.Text = "Disabled";
            this.PiiLoggingDisabled.UseVisualStyleBackColor = true;
            //
            // PiiLoggingEnabled
            //
            this.PiiLoggingEnabled.AutoSize = true;
            this.PiiLoggingEnabled.Location = new System.Drawing.Point(5, 10);
            this.PiiLoggingEnabled.Name = "PiiLoggingEnabled";
            this.PiiLoggingEnabled.Size = new System.Drawing.Size(64, 17);
            this.PiiLoggingEnabled.TabIndex = 30;
            this.PiiLoggingEnabled.Text = "Enabled";
            this.PiiLoggingEnabled.UseVisualStyleBackColor = true;
            //
            // label12
            //
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(7, 142);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(180, 24);
            this.label12.TabIndex = 36;
            this.label12.Text = "Pii Logging Enabled";
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
            // label11
            //
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(10, 78);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(178, 24);
            this.label11.TabIndex = 20;
            this.label11.Text = "Extra Query Params";
            //
            // label10
            //
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(10, 20);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(149, 24);
            this.label10.TabIndex = 17;
            this.label10.Text = "Environment QP";
            //
            // publicClientTabPage
            //
            this.publicClientTabPage.BackColor = System.Drawing.SystemColors.Control;
            this.publicClientTabPage.Controls.Add(this.acquireTokenSilentAuthority);
            this.publicClientTabPage.Controls.Add(this.acquireTokenInteractiveAuthority);
            this.publicClientTabPage.Controls.Add(this.PiiLoggingLabel);
            this.publicClientTabPage.Controls.Add(this.authority);
            this.publicClientTabPage.Controls.Add(this.scopes);
            this.publicClientTabPage.Controls.Add(this.callResult);
            this.publicClientTabPage.Controls.Add(this.loginHintTextBox);
            this.publicClientTabPage.Controls.Add(this.overriddenAuthority);
            this.publicClientTabPage.Controls.Add(this.label9);
            this.publicClientTabPage.Controls.Add(this.groupBox2);
            this.publicClientTabPage.Controls.Add(this.label8);
            this.publicClientTabPage.Controls.Add(this.userList);
            this.publicClientTabPage.Controls.Add(this.label7);
            this.publicClientTabPage.Controls.Add(this.acquireTokenSilent);
            this.publicClientTabPage.Controls.Add(this.acquireTokenWIA);
            this.publicClientTabPage.Controls.Add(this.acquireTokenInteractive);
            this.publicClientTabPage.Controls.Add(this.label6);
            this.publicClientTabPage.Controls.Add(this.label4);
            this.publicClientTabPage.Controls.Add(this.label3);
            this.publicClientTabPage.Location = new System.Drawing.Point(4, 22);
            this.publicClientTabPage.Name = "publicClientTabPage";
            this.publicClientTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.publicClientTabPage.Size = new System.Drawing.Size(668, 788);
            this.publicClientTabPage.TabIndex = 0;
            this.publicClientTabPage.Text = "publicClientTabPage";
            //
            // acquireTokenSilentAuthority
            //
            this.acquireTokenSilentAuthority.Location = new System.Drawing.Point(390, 732);
            this.acquireTokenSilentAuthority.Name = "acquireTokenSilentAuthority";
            this.acquireTokenSilentAuthority.Size = new System.Drawing.Size(227, 30);
            this.acquireTokenSilentAuthority.TabIndex = 31;
            this.acquireTokenSilentAuthority.Text = "Acquire Token Silent with Authority Override";
            this.acquireTokenSilentAuthority.UseVisualStyleBackColor = true;
            //
            // acquireTokenInteractiveAuthority
            //
            this.acquireTokenInteractiveAuthority.Location = new System.Drawing.Point(15, 732);
            this.acquireTokenInteractiveAuthority.Name = "acquireTokenInteractiveAuthority";
            this.acquireTokenInteractiveAuthority.Size = new System.Drawing.Size(282, 30);
            this.acquireTokenInteractiveAuthority.TabIndex = 30;
            this.acquireTokenInteractiveAuthority.Text = "Acquire Token Interactive with Authority Override";
            this.acquireTokenInteractiveAuthority.UseVisualStyleBackColor = true;
            this.acquireTokenInteractiveAuthority.Click += new System.EventHandler(this.acquireTokenInteractiveAuthority_Click);
            //
            // PiiLoggingLabel
            //
            this.PiiLoggingLabel.AutoSize = true;
            this.PiiLoggingLabel.Location = new System.Drawing.Point(25, 716);
            this.PiiLoggingLabel.Name = "PiiLoggingLabel";
            this.PiiLoggingLabel.Size = new System.Drawing.Size(0, 13);
            this.PiiLoggingLabel.TabIndex = 29;
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
            // scopes
            //
            this.scopes.Location = new System.Drawing.Point(256, 259);
            this.scopes.Name = "scopes";
            this.scopes.Size = new System.Drawing.Size(352, 20);
            this.scopes.TabIndex = 15;
            this.scopes.Text = "user.read";
            //
            // callResult
            //
            this.callResult.Location = new System.Drawing.Point(15, 403);
            this.callResult.Multiline = true;
            this.callResult.Name = "callResult";
            this.callResult.ReadOnly = true;
            this.callResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.callResult.Size = new System.Drawing.Size(645, 267);
            this.callResult.TabIndex = 13;
            //
            // loginHintTextBox
            //
            this.loginHintTextBox.Location = new System.Drawing.Point(256, 175);
            this.loginHintTextBox.Name = "loginHintTextBox";
            this.loginHintTextBox.Size = new System.Drawing.Size(352, 20);
            this.loginHintTextBox.TabIndex = 6;
            this.loginHintTextBox.TextChanged += new System.EventHandler(this.loginHint_TextChanged);
            //
            // overriddenAuthority
            //
            this.overriddenAuthority.Location = new System.Drawing.Point(256, 59);
            this.overriddenAuthority.Name = "overriddenAuthority";
            this.overriddenAuthority.Size = new System.Drawing.Size(352, 20);
            this.overriddenAuthority.TabIndex = 3;
            this.overriddenAuthority.TextChanged += new System.EventHandler(this.overriddenAuthority_TextChanged);
            //
            // label9
            //
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(8, 261);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(43, 13);
            this.label9.TabIndex = 14;
            this.label9.Text = "Scopes";
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.never);
            this.groupBox2.Controls.Add(this.consent);
            this.groupBox2.Controls.Add(this.forceLogin);
            this.groupBox2.Controls.Add(this.selectAccount);
            this.groupBox2.Location = new System.Drawing.Point(256, 103);
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
            this.never.Size = new System.Drawing.Size(54, 17);
            this.never.TabIndex = 10;
            this.never.Text = "Never";
            this.never.UseVisualStyleBackColor = true;
            //
            // consent
            //
            this.consent.AutoSize = true;
            this.consent.Location = new System.Drawing.Point(197, 18);
            this.consent.Name = "consent";
            this.consent.Size = new System.Drawing.Size(64, 17);
            this.consent.TabIndex = 9;
            this.consent.Text = "Consent";
            this.consent.UseVisualStyleBackColor = true;
            //
            // forceLogin
            //
            this.forceLogin.AutoSize = true;
            this.forceLogin.Location = new System.Drawing.Point(110, 18);
            this.forceLogin.Name = "forceLogin";
            this.forceLogin.Size = new System.Drawing.Size(81, 17);
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
            this.selectAccount.Size = new System.Drawing.Size(98, 17);
            this.selectAccount.TabIndex = 7;
            this.selectAccount.TabStop = true;
            this.selectAccount.Text = "Select Account";
            this.selectAccount.UseVisualStyleBackColor = true;
            //
            // label8
            //
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(8, 121);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(63, 13);
            this.label8.TabIndex = 9;
            this.label8.Text = "UI Behavior";
            //
            // userList
            //
            this.userList.AllowDrop = true;
            this.userList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.userList.FormattingEnabled = true;
            this.userList.Location = new System.Drawing.Point(256, 215);
            this.userList.Name = "userList";
            this.userList.Size = new System.Drawing.Size(352, 21);
            this.userList.TabIndex = 12;
            this.userList.SelectedIndexChanged += new System.EventHandler(this.userList_SelectedIndexChanged);
            //
            // label7
            //
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(8, 215);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 13);
            this.label7.TabIndex = 11;
            this.label7.Text = "User";
            //
            // acquireTokenSilent
            //
            this.acquireTokenSilent.Location = new System.Drawing.Point(453, 683);
            this.acquireTokenSilent.Name = "acquireTokenSilent";
            this.acquireTokenSilent.Size = new System.Drawing.Size(164, 30);
            this.acquireTokenSilent.TabIndex = 10;
            this.acquireTokenSilent.Text = "Acquire Token Silent";
            this.acquireTokenSilent.UseVisualStyleBackColor = true;
            this.acquireTokenSilent.Click += new System.EventHandler(this.acquireTokenSilent_Click);
            //
            // acquireTokenByWIA
            //
            this.acquireTokenWIA.Location = new System.Drawing.Point(180, 683);
            this.acquireTokenWIA.Name = "acquireTokenWIA";
            this.acquireTokenWIA.Size = new System.Drawing.Size(240, 30);
            this.acquireTokenWIA.TabIndex = 9;
            this.acquireTokenWIA.Text = "Acquire Token By WindowsIntegratedAuth";
            this.acquireTokenWIA.UseVisualStyleBackColor = true;
            this.acquireTokenWIA.Click += new System.EventHandler(this.acquireTokenByWindowsIntegratedAuth_Click);
            //
            // acquireTokenInteractive
            //
            this.acquireTokenInteractive.Location = new System.Drawing.Point(15, 683);
            this.acquireTokenInteractive.Name = "acquireTokenInteractive";
            this.acquireTokenInteractive.Size = new System.Drawing.Size(139, 30);
            this.acquireTokenInteractive.TabIndex = 9;
            this.acquireTokenInteractive.Text = "Acquire Token Interactive";
            this.acquireTokenInteractive.UseVisualStyleBackColor = true;
            this.acquireTokenInteractive.Click += new System.EventHandler(this.AcquireTokenInteractive_Click);
            //
            // label6
            //
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(8, 177);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 13);
            this.label6.TabIndex = 5;
            this.label6.Text = "Login Hint";
            //
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(8, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(146, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Overridden Authority for 1 call";
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(8, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Authority";
            //
            // tabControl1
            //
            this.tabControl1.Controls.Add(this.publicClientTabPage);
            this.tabControl1.Controls.Add(this.settingsTabPage);
            this.tabControl1.Controls.Add(this.cacheTabPage);
            this.tabControl1.Controls.Add(this.logsTabPage);
            this.tabControl1.Location = new System.Drawing.Point(1, -3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.RightToLeftLayout = true;
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(676, 814);
            this.tabControl1.TabIndex = 0;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(674, 768);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.cache);
            this.Controls.Add(this.settings);
            this.Controls.Add(this.publicClient);
            this.Controls.Add(this.tabControl1);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Dev Utility Test App";
            this.logsTabPage.ResumeLayout(false);
            this.logsTabPage.PerformLayout();
            this.cacheTabPage.ResumeLayout(false);
            this.cacheTabPage.PerformLayout();
            this.settingsTabPage.ResumeLayout(false);
            this.settingsTabPage.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.publicClientTabPage.ResumeLayout(false);
            this.publicClientTabPage.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button publicClient;
        private System.Windows.Forms.Button settings;
        private System.Windows.Forms.Button cache;
        private System.Windows.Forms.Button logs;
        private TabPage logsTabPage;
        private Button clearLogsButton;
        private TextBox msalPIILogsTextBox;
        private TextBox msalLogsTextBox;
        private Label label2;
        private Label label1;
        private TabPage cacheTabPage;
        private TableLayoutPanel cachePageTableLayout;
        private TabPage settingsTabPage;
        private ComboBox logLevel;
        private Label label14;
        private GroupBox groupBox1;
        private RadioButton validateAuthorityDisabled;
        private RadioButton validateAuthorityEnabled;
        private Label label5;
        private GroupBox groupBox6;
        private RadioButton PiiLoggingDisabled;
        private RadioButton PiiLoggingEnabled;
        private Label label12;
        private TextBox extraQueryParams;
        private TextBox environmentQP;
        private Label label11;
        private Label label10;
        private TabPage publicClientTabPage;
        private Button acquireTokenSilentAuthority;
        private Button acquireTokenInteractiveAuthority;
        private Label PiiLoggingLabel;
        private TextBox authority;
        private TextBox scopes;
        private TextBox callResult;
        private TextBox loginHintTextBox;
        private TextBox overriddenAuthority;
        private Label label9;
        private GroupBox groupBox2;
        private RadioButton never;
        private RadioButton consent;
        private RadioButton forceLogin;
        private RadioButton selectAccount;
        private Label label8;
        private ComboBox userList;
        private Label label7;
        private Button acquireTokenSilent;
        private Button acquireTokenWIA;
        private Button acquireTokenInteractive;
        private Label label6;
        private Label label4;
        private Label label3;
        private TabControl tabControl1;
    }
}


