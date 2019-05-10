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
            this.b2cSilentCustomDomain = new System.Windows.Forms.Button();
            this.b2cCustomDomain = new System.Windows.Forms.Button();
            this.forceRefreshCheckBox = new System.Windows.Forms.CheckBox();
            this.b2cEditProfile = new System.Windows.Forms.Button();
            this.b2cSilentFlow = new System.Windows.Forms.Button();
            this.b2cLogin = new System.Windows.Forms.Button();
            this.cancelOperationButton = new System.Windows.Forms.Button();
            this.acquireTokenDeviceCode = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.IgnoreUserCbx = new System.Windows.Forms.CheckBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.userPasswordTextBox = new System.Windows.Forms.TextBox();
            this.acquireTokenByUPButton = new System.Windows.Forms.Button();
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
            this.noPrompt = new System.Windows.Forms.RadioButton();
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
            this.ropcB2CAT = new System.Windows.Forms.Button();
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
            this.publicClient.Location = new System.Drawing.Point(2, 1571);
            this.publicClient.Margin = new System.Windows.Forms.Padding(6);
            this.publicClient.Name = "publicClient";
            this.publicClient.Size = new System.Drawing.Size(232, 87);
            this.publicClient.TabIndex = 1;
            this.publicClient.Text = "Public Client";
            this.publicClient.UseVisualStyleBackColor = true;
            this.publicClient.Click += new System.EventHandler(this.acquire_Click);
            // 
            // settings
            // 
            this.settings.Location = new System.Drawing.Point(400, 1571);
            this.settings.Margin = new System.Windows.Forms.Padding(6);
            this.settings.Name = "settings";
            this.settings.Size = new System.Drawing.Size(192, 87);
            this.settings.TabIndex = 2;
            this.settings.Text = "Settings";
            this.settings.UseVisualStyleBackColor = true;
            this.settings.Click += new System.EventHandler(this.settings_Click);
            // 
            // cache
            // 
            this.cache.Location = new System.Drawing.Point(774, 1571);
            this.cache.Margin = new System.Windows.Forms.Padding(6);
            this.cache.Name = "cache";
            this.cache.Size = new System.Drawing.Size(220, 87);
            this.cache.TabIndex = 3;
            this.cache.Text = "Cache";
            this.cache.UseVisualStyleBackColor = true;
            this.cache.Click += new System.EventHandler(this.cache_Click);
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(1162, 1571);
            this.logs.Margin = new System.Windows.Forms.Padding(6);
            this.logs.Name = "logs";
            this.logs.Size = new System.Drawing.Size(184, 87);
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
            this.logsTabPage.Location = new System.Drawing.Point(8, 39);
            this.logsTabPage.Margin = new System.Windows.Forms.Padding(6);
            this.logsTabPage.Name = "logsTabPage";
            this.logsTabPage.Padding = new System.Windows.Forms.Padding(6);
            this.logsTabPage.Size = new System.Drawing.Size(1336, 1518);
            this.logsTabPage.TabIndex = 3;
            this.logsTabPage.Text = "logsTabPage";
            this.logsTabPage.UseVisualStyleBackColor = true;
            // 
            // clearLogsButton
            // 
            this.clearLogsButton.Location = new System.Drawing.Point(446, 1331);
            this.clearLogsButton.Margin = new System.Windows.Forms.Padding(6);
            this.clearLogsButton.Name = "clearLogsButton";
            this.clearLogsButton.Size = new System.Drawing.Size(302, 83);
            this.clearLogsButton.TabIndex = 4;
            this.clearLogsButton.Text = "Clear Logs";
            this.clearLogsButton.UseVisualStyleBackColor = true;
            this.clearLogsButton.Click += new System.EventHandler(this.clearLogsButton_Click);
            // 
            // msalPIILogsTextBox
            // 
            this.msalPIILogsTextBox.Location = new System.Drawing.Point(14, 702);
            this.msalPIILogsTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.msalPIILogsTextBox.Multiline = true;
            this.msalPIILogsTextBox.Name = "msalPIILogsTextBox";
            this.msalPIILogsTextBox.ReadOnly = true;
            this.msalPIILogsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalPIILogsTextBox.Size = new System.Drawing.Size(1306, 581);
            this.msalPIILogsTextBox.TabIndex = 3;
            // 
            // msalLogsTextBox
            // 
            this.msalLogsTextBox.Location = new System.Drawing.Point(14, 40);
            this.msalLogsTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.msalLogsTextBox.Multiline = true;
            this.msalLogsTextBox.Name = "msalLogsTextBox";
            this.msalLogsTextBox.ReadOnly = true;
            this.msalLogsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.msalLogsTextBox.Size = new System.Drawing.Size(1306, 581);
            this.msalLogsTextBox.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(286, 10);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(671, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "========================= Logs =========================";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(286, 671);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(701, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "========================= PII Logs =========================";
            // 
            // cacheTabPage
            // 
            this.cacheTabPage.AutoScroll = true;
            this.cacheTabPage.Controls.Add(this.cachePageTableLayout);
            this.cacheTabPage.Location = new System.Drawing.Point(8, 39);
            this.cacheTabPage.Margin = new System.Windows.Forms.Padding(6);
            this.cacheTabPage.Name = "cacheTabPage";
            this.cacheTabPage.Padding = new System.Windows.Forms.Padding(6);
            this.cacheTabPage.Size = new System.Drawing.Size(1336, 1518);
            this.cacheTabPage.TabIndex = 2;
            this.cacheTabPage.Text = "cacheTabPage";
            this.cacheTabPage.UseVisualStyleBackColor = true;
            // 
            // cachePageTableLayout
            // 
            this.cachePageTableLayout.AutoSize = true;
            this.cachePageTableLayout.ColumnCount = 1;
            this.cachePageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.cachePageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.cachePageTableLayout.Location = new System.Drawing.Point(16, 13);
            this.cachePageTableLayout.Margin = new System.Windows.Forms.Padding(6);
            this.cachePageTableLayout.Name = "cachePageTableLayout";
            this.cachePageTableLayout.RowCount = 2;
            this.cachePageTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.cachePageTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.cachePageTableLayout.Size = new System.Drawing.Size(1308, 192);
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
            this.settingsTabPage.Location = new System.Drawing.Point(8, 39);
            this.settingsTabPage.Margin = new System.Windows.Forms.Padding(6);
            this.settingsTabPage.Name = "settingsTabPage";
            this.settingsTabPage.Padding = new System.Windows.Forms.Padding(6);
            this.settingsTabPage.Size = new System.Drawing.Size(1336, 1518);
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
            this.logLevel.Location = new System.Drawing.Point(506, 373);
            this.logLevel.Margin = new System.Windows.Forms.Padding(6);
            this.logLevel.Name = "logLevel";
            this.logLevel.Size = new System.Drawing.Size(238, 33);
            this.logLevel.TabIndex = 41;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(22, 373);
            this.label14.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(184, 44);
            this.label14.TabIndex = 40;
            this.label14.Text = "Log Level";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.validateAuthorityDisabled);
            this.groupBox1.Controls.Add(this.validateAuthorityEnabled);
            this.groupBox1.Location = new System.Drawing.Point(518, 687);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox1.Size = new System.Drawing.Size(410, 98);
            this.groupBox1.TabIndex = 39;
            this.groupBox1.TabStop = false;
            // 
            // validateAuthorityDisabled
            // 
            this.validateAuthorityDisabled.AutoSize = true;
            this.validateAuthorityDisabled.Location = new System.Drawing.Point(268, 35);
            this.validateAuthorityDisabled.Margin = new System.Windows.Forms.Padding(6);
            this.validateAuthorityDisabled.Name = "validateAuthorityDisabled";
            this.validateAuthorityDisabled.Size = new System.Drawing.Size(127, 29);
            this.validateAuthorityDisabled.TabIndex = 8;
            this.validateAuthorityDisabled.Text = "Disabled";
            this.validateAuthorityDisabled.UseVisualStyleBackColor = true;
            // 
            // validateAuthorityEnabled
            // 
            this.validateAuthorityEnabled.AutoSize = true;
            this.validateAuthorityEnabled.Checked = true;
            this.validateAuthorityEnabled.Location = new System.Drawing.Point(12, 37);
            this.validateAuthorityEnabled.Margin = new System.Windows.Forms.Padding(6);
            this.validateAuthorityEnabled.Name = "validateAuthorityEnabled";
            this.validateAuthorityEnabled.Size = new System.Drawing.Size(122, 29);
            this.validateAuthorityEnabled.TabIndex = 7;
            this.validateAuthorityEnabled.TabStop = true;
            this.validateAuthorityEnabled.Text = "Enabled";
            this.validateAuthorityEnabled.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(22, 721);
            this.label5.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(319, 44);
            this.label5.TabIndex = 38;
            this.label5.Text = "Validate Authority";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.PiiLoggingDisabled);
            this.groupBox6.Controls.Add(this.PiiLoggingEnabled);
            this.groupBox6.Location = new System.Drawing.Point(506, 254);
            this.groupBox6.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox6.Size = new System.Drawing.Size(378, 81);
            this.groupBox6.TabIndex = 37;
            this.groupBox6.TabStop = false;
            // 
            // PiiLoggingDisabled
            // 
            this.PiiLoggingDisabled.AutoSize = true;
            this.PiiLoggingDisabled.Checked = true;
            this.PiiLoggingDisabled.Location = new System.Drawing.Point(218, 19);
            this.PiiLoggingDisabled.Margin = new System.Windows.Forms.Padding(6);
            this.PiiLoggingDisabled.Name = "PiiLoggingDisabled";
            this.PiiLoggingDisabled.Size = new System.Drawing.Size(127, 29);
            this.PiiLoggingDisabled.TabIndex = 31;
            this.PiiLoggingDisabled.TabStop = true;
            this.PiiLoggingDisabled.Text = "Disabled";
            this.PiiLoggingDisabled.UseVisualStyleBackColor = true;
            // 
            // PiiLoggingEnabled
            // 
            this.PiiLoggingEnabled.AutoSize = true;
            this.PiiLoggingEnabled.Location = new System.Drawing.Point(10, 19);
            this.PiiLoggingEnabled.Margin = new System.Windows.Forms.Padding(6);
            this.PiiLoggingEnabled.Name = "PiiLoggingEnabled";
            this.PiiLoggingEnabled.Size = new System.Drawing.Size(122, 29);
            this.PiiLoggingEnabled.TabIndex = 30;
            this.PiiLoggingEnabled.Text = "Enabled";
            this.PiiLoggingEnabled.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(14, 273);
            this.label12.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(357, 44);
            this.label12.TabIndex = 36;
            this.label12.Text = "Pii Logging Enabled";
            // 
            // extraQueryParams
            // 
            this.extraQueryParams.AccessibleName = "";
            this.extraQueryParams.Location = new System.Drawing.Point(496, 173);
            this.extraQueryParams.Margin = new System.Windows.Forms.Padding(6);
            this.extraQueryParams.Name = "extraQueryParams";
            this.extraQueryParams.Size = new System.Drawing.Size(700, 31);
            this.extraQueryParams.TabIndex = 21;
            // 
            // environmentQP
            // 
            this.environmentQP.AccessibleName = "";
            this.environmentQP.Location = new System.Drawing.Point(496, 62);
            this.environmentQP.Margin = new System.Windows.Forms.Padding(6);
            this.environmentQP.Name = "environmentQP";
            this.environmentQP.Size = new System.Drawing.Size(700, 31);
            this.environmentQP.TabIndex = 18;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(20, 150);
            this.label11.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(365, 44);
            this.label11.TabIndex = 20;
            this.label11.Text = "Extra Query Params";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(20, 38);
            this.label10.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(300, 44);
            this.label10.TabIndex = 17;
            this.label10.Text = "Environment QP";
            // 
            // publicClientTabPage
            // 
            this.publicClientTabPage.BackColor = System.Drawing.SystemColors.Control;
            this.publicClientTabPage.Controls.Add(this.ropcB2CAT);
            this.publicClientTabPage.Controls.Add(this.b2cSilentCustomDomain);
            this.publicClientTabPage.Controls.Add(this.b2cCustomDomain);
            this.publicClientTabPage.Controls.Add(this.forceRefreshCheckBox);
            this.publicClientTabPage.Controls.Add(this.b2cEditProfile);
            this.publicClientTabPage.Controls.Add(this.b2cSilentFlow);
            this.publicClientTabPage.Controls.Add(this.b2cLogin);
            this.publicClientTabPage.Controls.Add(this.cancelOperationButton);
            this.publicClientTabPage.Controls.Add(this.acquireTokenDeviceCode);
            this.publicClientTabPage.Controls.Add(this.progressBar1);
            this.publicClientTabPage.Controls.Add(this.IgnoreUserCbx);
            this.publicClientTabPage.Controls.Add(this.passwordLabel);
            this.publicClientTabPage.Controls.Add(this.userPasswordTextBox);
            this.publicClientTabPage.Controls.Add(this.acquireTokenByUPButton);
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
            this.publicClientTabPage.Location = new System.Drawing.Point(8, 39);
            this.publicClientTabPage.Margin = new System.Windows.Forms.Padding(6);
            this.publicClientTabPage.Name = "publicClientTabPage";
            this.publicClientTabPage.Padding = new System.Windows.Forms.Padding(6);
            this.publicClientTabPage.Size = new System.Drawing.Size(1336, 1518);
            this.publicClientTabPage.TabIndex = 0;
            this.publicClientTabPage.Text = "publicClientTabPage";
            // 
            // b2cSilentCustomDomain
            // 
            this.b2cSilentCustomDomain.Location = new System.Drawing.Point(307, 1325);
            this.b2cSilentCustomDomain.Margin = new System.Windows.Forms.Padding(6);
            this.b2cSilentCustomDomain.Name = "b2cSilentCustomDomain";
            this.b2cSilentCustomDomain.Size = new System.Drawing.Size(252, 58);
            this.b2cSilentCustomDomain.TabIndex = 45;
            this.b2cSilentCustomDomain.Text = "B2C Silent Custom Domain";
            this.b2cSilentCustomDomain.UseVisualStyleBackColor = true;
            this.b2cSilentCustomDomain.Click += new System.EventHandler(this.B2cSilentCustomDomain_Click);
            // 
            // b2cCustomDomain
            // 
            this.b2cCustomDomain.Location = new System.Drawing.Point(307, 1256);
            this.b2cCustomDomain.Margin = new System.Windows.Forms.Padding(6);
            this.b2cCustomDomain.Name = "b2cCustomDomain";
            this.b2cCustomDomain.Size = new System.Drawing.Size(252, 58);
            this.b2cCustomDomain.TabIndex = 44;
            this.b2cCustomDomain.Text = "B2C Custom Domain";
            this.b2cCustomDomain.UseVisualStyleBackColor = true;
            this.b2cCustomDomain.Click += new System.EventHandler(this.B2cCustomDomain_Click);
            // 
            // forceRefreshCheckBox
            // 
            this.forceRefreshCheckBox.AutoSize = true;
            this.forceRefreshCheckBox.Location = new System.Drawing.Point(524, 163);
            this.forceRefreshCheckBox.Margin = new System.Windows.Forms.Padding(6);
            this.forceRefreshCheckBox.Name = "forceRefreshCheckBox";
            this.forceRefreshCheckBox.Size = new System.Drawing.Size(180, 29);
            this.forceRefreshCheckBox.TabIndex = 43;
            this.forceRefreshCheckBox.Text = "Force Refresh";
            this.forceRefreshCheckBox.UseVisualStyleBackColor = true;
            // 
            // b2cEditProfile
            // 
            this.b2cEditProfile.Location = new System.Drawing.Point(30, 1390);
            this.b2cEditProfile.Margin = new System.Windows.Forms.Padding(6);
            this.b2cEditProfile.Name = "b2cEditProfile";
            this.b2cEditProfile.Size = new System.Drawing.Size(252, 58);
            this.b2cEditProfile.TabIndex = 42;
            this.b2cEditProfile.Text = "B2C Edit Profile";
            this.b2cEditProfile.UseVisualStyleBackColor = true;
            this.b2cEditProfile.Click += new System.EventHandler(this.b2cEditProfile_Click);
            // 
            // b2cSilentFlow
            // 
            this.b2cSilentFlow.Location = new System.Drawing.Point(30, 1325);
            this.b2cSilentFlow.Margin = new System.Windows.Forms.Padding(6);
            this.b2cSilentFlow.Name = "b2cSilentFlow";
            this.b2cSilentFlow.Size = new System.Drawing.Size(252, 58);
            this.b2cSilentFlow.TabIndex = 41;
            this.b2cSilentFlow.Text = "B2C Silent AT";
            this.b2cSilentFlow.UseVisualStyleBackColor = true;
            this.b2cSilentFlow.Click += new System.EventHandler(this.b2cSilentFlow_Click);
            // 
            // b2cLogin
            // 
            this.b2cLogin.Location = new System.Drawing.Point(30, 1256);
            this.b2cLogin.Margin = new System.Windows.Forms.Padding(6);
            this.b2cLogin.Name = "b2cLogin";
            this.b2cLogin.Size = new System.Drawing.Size(252, 58);
            this.b2cLogin.TabIndex = 40;
            this.b2cLogin.Text = "B2C Acquire Token";
            this.b2cLogin.UseVisualStyleBackColor = true;
            this.b2cLogin.Click += new System.EventHandler(this.b2cLogin_Click);
            // 
            // cancelOperationButton
            // 
            this.cancelOperationButton.Location = new System.Drawing.Point(996, 1392);
            this.cancelOperationButton.Margin = new System.Windows.Forms.Padding(6);
            this.cancelOperationButton.Name = "cancelOperationButton";
            this.cancelOperationButton.Size = new System.Drawing.Size(328, 56);
            this.cancelOperationButton.TabIndex = 39;
            this.cancelOperationButton.Text = "Cancel Operation";
            this.cancelOperationButton.UseVisualStyleBackColor = true;
            this.cancelOperationButton.Click += new System.EventHandler(this.cancelOperationButton_Click);
            // 
            // acquireTokenDeviceCode
            // 
            this.acquireTokenDeviceCode.Location = new System.Drawing.Point(992, 1119);
            this.acquireTokenDeviceCode.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenDeviceCode.Name = "acquireTokenDeviceCode";
            this.acquireTokenDeviceCode.Size = new System.Drawing.Size(328, 56);
            this.acquireTokenDeviceCode.TabIndex = 38;
            this.acquireTokenDeviceCode.Text = "Acquire Token By DeviceCode";
            this.acquireTokenDeviceCode.UseVisualStyleBackColor = true;
            this.acquireTokenDeviceCode.Click += new System.EventHandler(this.acquireTokenDeviceCode_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(30, 1460);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(6);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1290, 44);
            this.progressBar1.TabIndex = 37;
            // 
            // IgnoreUserCbx
            // 
            this.IgnoreUserCbx.AutoSize = true;
            this.IgnoreUserCbx.Location = new System.Drawing.Point(1010, 431);
            this.IgnoreUserCbx.Margin = new System.Windows.Forms.Padding(6);
            this.IgnoreUserCbx.Name = "IgnoreUserCbx";
            this.IgnoreUserCbx.Size = new System.Drawing.Size(104, 29);
            this.IgnoreUserCbx.TabIndex = 36;
            this.IgnoreUserCbx.Text = "Ignore";
            this.IgnoreUserCbx.UseVisualStyleBackColor = true;
            // 
            // passwordLabel
            // 
            this.passwordLabel.AutoSize = true;
            this.passwordLabel.Location = new System.Drawing.Point(16, 387);
            this.passwordLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(497, 25);
            this.passwordLabel.TabIndex = 34;
            this.passwordLabel.Text = "Enter Password (for username/password flow only)";
            // 
            // userPasswordTextBox
            // 
            this.userPasswordTextBox.Location = new System.Drawing.Point(514, 381);
            this.userPasswordTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.userPasswordTextBox.Name = "userPasswordTextBox";
            this.userPasswordTextBox.PasswordChar = '*';
            this.userPasswordTextBox.Size = new System.Drawing.Size(478, 31);
            this.userPasswordTextBox.TabIndex = 33;
            // 
            // acquireTokenByUPButton
            // 
            this.acquireTokenByUPButton.Location = new System.Drawing.Point(414, 1119);
            this.acquireTokenByUPButton.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenByUPButton.Name = "acquireTokenByUPButton";
            this.acquireTokenByUPButton.Size = new System.Drawing.Size(480, 56);
            this.acquireTokenByUPButton.TabIndex = 32;
            this.acquireTokenByUPButton.Text = "Acquire Token By Username/Password";
            this.acquireTokenByUPButton.UseVisualStyleBackColor = true;
            this.acquireTokenByUPButton.Click += new System.EventHandler(this.acquireTokenByUPButton_Click);
            // 
            // acquireTokenSilentAuthority
            // 
            this.acquireTokenSilentAuthority.Location = new System.Drawing.Point(866, 1187);
            this.acquireTokenSilentAuthority.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenSilentAuthority.Name = "acquireTokenSilentAuthority";
            this.acquireTokenSilentAuthority.Size = new System.Drawing.Size(454, 58);
            this.acquireTokenSilentAuthority.TabIndex = 31;
            this.acquireTokenSilentAuthority.Text = "Acquire Token Silent with Authority Override";
            this.acquireTokenSilentAuthority.UseVisualStyleBackColor = true;
            // 
            // acquireTokenInteractiveAuthority
            // 
            this.acquireTokenInteractiveAuthority.Location = new System.Drawing.Point(30, 1187);
            this.acquireTokenInteractiveAuthority.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenInteractiveAuthority.Name = "acquireTokenInteractiveAuthority";
            this.acquireTokenInteractiveAuthority.Size = new System.Drawing.Size(564, 58);
            this.acquireTokenInteractiveAuthority.TabIndex = 30;
            this.acquireTokenInteractiveAuthority.Text = "Acquire Token Interactive with Authority Override";
            this.acquireTokenInteractiveAuthority.UseVisualStyleBackColor = true;
            this.acquireTokenInteractiveAuthority.Click += new System.EventHandler(this.acquireTokenInteractiveAuthority_Click);
            // 
            // PiiLoggingLabel
            // 
            this.PiiLoggingLabel.AutoSize = true;
            this.PiiLoggingLabel.Location = new System.Drawing.Point(50, 1175);
            this.PiiLoggingLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.PiiLoggingLabel.Name = "PiiLoggingLabel";
            this.PiiLoggingLabel.Size = new System.Drawing.Size(0, 25);
            this.PiiLoggingLabel.TabIndex = 29;
            // 
            // authority
            // 
            this.authority.AccessibleName = "authority";
            this.authority.Location = new System.Drawing.Point(512, 38);
            this.authority.Margin = new System.Windows.Forms.Padding(6);
            this.authority.Name = "authority";
            this.authority.Size = new System.Drawing.Size(700, 31);
            this.authority.TabIndex = 16;
            this.authority.Text = "https://login.microsoftonline.com/common";
            this.authority.Leave += new System.EventHandler(this.authority_FocusLeave);
            // 
            // scopes
            // 
            this.scopes.Location = new System.Drawing.Point(512, 498);
            this.scopes.Margin = new System.Windows.Forms.Padding(6);
            this.scopes.Name = "scopes";
            this.scopes.Size = new System.Drawing.Size(700, 31);
            this.scopes.TabIndex = 15;
            this.scopes.Text = "user.read";
            // 
            // callResult
            // 
            this.callResult.Location = new System.Drawing.Point(30, 573);
            this.callResult.Margin = new System.Windows.Forms.Padding(6);
            this.callResult.Multiline = true;
            this.callResult.Name = "callResult";
            this.callResult.ReadOnly = true;
            this.callResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.callResult.Size = new System.Drawing.Size(1286, 462);
            this.callResult.TabIndex = 13;
            // 
            // loginHintTextBox
            // 
            this.loginHintTextBox.Location = new System.Drawing.Point(514, 337);
            this.loginHintTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.loginHintTextBox.Name = "loginHintTextBox";
            this.loginHintTextBox.Size = new System.Drawing.Size(698, 31);
            this.loginHintTextBox.TabIndex = 6;
            this.loginHintTextBox.TextChanged += new System.EventHandler(this.loginHint_TextChanged);
            // 
            // overriddenAuthority
            // 
            this.overriddenAuthority.Location = new System.Drawing.Point(512, 113);
            this.overriddenAuthority.Margin = new System.Windows.Forms.Padding(6);
            this.overriddenAuthority.Name = "overriddenAuthority";
            this.overriddenAuthority.Size = new System.Drawing.Size(700, 31);
            this.overriddenAuthority.TabIndex = 3;
            this.overriddenAuthority.TextChanged += new System.EventHandler(this.overriddenAuthority_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(16, 502);
            this.label9.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(84, 25);
            this.label9.TabIndex = 14;
            this.label9.Text = "Scopes";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.noPrompt);
            this.groupBox2.Controls.Add(this.never);
            this.groupBox2.Controls.Add(this.consent);
            this.groupBox2.Controls.Add(this.forceLogin);
            this.groupBox2.Controls.Add(this.selectAccount);
            this.groupBox2.Location = new System.Drawing.Point(512, 198);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox2.Size = new System.Drawing.Size(684, 127);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            // 
            // noPrompt
            // 
            this.noPrompt.AutoSize = true;
            this.noPrompt.Location = new System.Drawing.Point(12, 83);
            this.noPrompt.Margin = new System.Windows.Forms.Padding(6);
            this.noPrompt.Name = "noPrompt";
            this.noPrompt.Size = new System.Drawing.Size(191, 29);
            this.noPrompt.TabIndex = 11;
            this.noPrompt.Text = "B2C No Prompt";
            this.noPrompt.UseVisualStyleBackColor = true;
            // 
            // never
            // 
            this.never.AutoSize = true;
            this.never.Location = new System.Drawing.Point(534, 35);
            this.never.Margin = new System.Windows.Forms.Padding(6);
            this.never.Name = "never";
            this.never.Size = new System.Drawing.Size(100, 29);
            this.never.TabIndex = 10;
            this.never.Text = "Never";
            this.never.UseVisualStyleBackColor = true;
            // 
            // consent
            // 
            this.consent.AutoSize = true;
            this.consent.Location = new System.Drawing.Point(394, 37);
            this.consent.Margin = new System.Windows.Forms.Padding(6);
            this.consent.Name = "consent";
            this.consent.Size = new System.Drawing.Size(123, 29);
            this.consent.TabIndex = 9;
            this.consent.Text = "Consent";
            this.consent.UseVisualStyleBackColor = true;
            // 
            // forceLogin
            // 
            this.forceLogin.AutoSize = true;
            this.forceLogin.Location = new System.Drawing.Point(220, 37);
            this.forceLogin.Margin = new System.Windows.Forms.Padding(6);
            this.forceLogin.Name = "forceLogin";
            this.forceLogin.Size = new System.Drawing.Size(157, 29);
            this.forceLogin.TabIndex = 8;
            this.forceLogin.Text = "Force Login";
            this.forceLogin.UseVisualStyleBackColor = true;
            // 
            // selectAccount
            // 
            this.selectAccount.AutoSize = true;
            this.selectAccount.Checked = true;
            this.selectAccount.Location = new System.Drawing.Point(12, 37);
            this.selectAccount.Margin = new System.Windows.Forms.Padding(6);
            this.selectAccount.Name = "selectAccount";
            this.selectAccount.Size = new System.Drawing.Size(187, 29);
            this.selectAccount.TabIndex = 7;
            this.selectAccount.TabStop = true;
            this.selectAccount.Text = "Select Account";
            this.selectAccount.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(16, 233);
            this.label8.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(123, 25);
            this.label8.TabIndex = 9;
            this.label8.Text = "UI Behavior";
            // 
            // userList
            // 
            this.userList.AllowDrop = true;
            this.userList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.userList.FormattingEnabled = true;
            this.userList.Location = new System.Drawing.Point(512, 431);
            this.userList.Margin = new System.Windows.Forms.Padding(6);
            this.userList.Name = "userList";
            this.userList.Size = new System.Drawing.Size(480, 33);
            this.userList.TabIndex = 12;
            this.userList.SelectedIndexChanged += new System.EventHandler(this.userList_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(14, 437);
            this.label7.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 25);
            this.label7.TabIndex = 11;
            this.label7.Text = "User";
            // 
            // acquireTokenSilent
            // 
            this.acquireTokenSilent.Location = new System.Drawing.Point(992, 1050);
            this.acquireTokenSilent.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenSilent.Name = "acquireTokenSilent";
            this.acquireTokenSilent.Size = new System.Drawing.Size(328, 58);
            this.acquireTokenSilent.TabIndex = 10;
            this.acquireTokenSilent.Text = "Acquire Token Silent";
            this.acquireTokenSilent.UseVisualStyleBackColor = true;
            this.acquireTokenSilent.Click += new System.EventHandler(this.acquireTokenSilent_Click);
            // 
            // acquireTokenWIA
            // 
            this.acquireTokenWIA.Location = new System.Drawing.Point(414, 1050);
            this.acquireTokenWIA.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenWIA.Name = "acquireTokenWIA";
            this.acquireTokenWIA.Size = new System.Drawing.Size(480, 58);
            this.acquireTokenWIA.TabIndex = 9;
            this.acquireTokenWIA.Text = "Acquire Token By WindowsIntegratedAuth";
            this.acquireTokenWIA.UseVisualStyleBackColor = true;
            this.acquireTokenWIA.Click += new System.EventHandler(this.acquireTokenByWindowsIntegratedAuth_Click);
            // 
            // acquireTokenInteractive
            // 
            this.acquireTokenInteractive.Location = new System.Drawing.Point(30, 1050);
            this.acquireTokenInteractive.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenInteractive.Name = "acquireTokenInteractive";
            this.acquireTokenInteractive.Size = new System.Drawing.Size(296, 58);
            this.acquireTokenInteractive.TabIndex = 9;
            this.acquireTokenInteractive.Text = "Acquire Token Interactive";
            this.acquireTokenInteractive.UseVisualStyleBackColor = true;
            this.acquireTokenInteractive.Click += new System.EventHandler(this.AcquireTokenInteractive_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(16, 340);
            this.label6.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(386, 25);
            this.label6.TabIndex = 5;
            this.label6.Text = "Login Hint (Username for IWA and U/P)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(16, 112);
            this.label4.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(297, 25);
            this.label4.TabIndex = 2;
            this.label4.Text = "Overridden Authority for 1 call";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(16, 37);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 25);
            this.label3.TabIndex = 0;
            this.label3.Text = "Authority";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.publicClientTabPage);
            this.tabControl1.Controls.Add(this.settingsTabPage);
            this.tabControl1.Controls.Add(this.cacheTabPage);
            this.tabControl1.Controls.Add(this.logsTabPage);
            this.tabControl1.Location = new System.Drawing.Point(2, -6);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(6);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.RightToLeftLayout = true;
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1352, 1565);
            this.tabControl1.TabIndex = 0;
            // 
            // ropcB2CAT
            // 
            this.ropcB2CAT.Location = new System.Drawing.Point(307, 1390);
            this.ropcB2CAT.Margin = new System.Windows.Forms.Padding(6);
            this.ropcB2CAT.Name = "ropcB2CAT";
            this.ropcB2CAT.Size = new System.Drawing.Size(252, 58);
            this.ropcB2CAT.TabIndex = 46;
            this.ropcB2CAT.Text = "B2C ROPC";
            this.ropcB2CAT.UseVisualStyleBackColor = true;
            this.ropcB2CAT.Click += new System.EventHandler(this.RopcB2CAT_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1348, 1540);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.cache);
            this.Controls.Add(this.settings);
            this.Controls.Add(this.publicClient);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(6);
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
        private Button acquireTokenByUPButton;
        private TextBox userPasswordTextBox;
        private Label passwordLabel;
        private CheckBox IgnoreUserCbx;
        private ProgressBar progressBar1;
        private Button acquireTokenDeviceCode;
        private Button cancelOperationButton;
        private Button b2cLogin;
        private Button b2cSilentFlow;
        private Button b2cEditProfile;
        private RadioButton noPrompt;
        private CheckBox forceRefreshCheckBox;
        private Button b2cCustomDomain;
        private Button b2cSilentCustomDomain;
        private Button ropcB2CAT;
    }
}


