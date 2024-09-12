namespace NetDesktopWinForms
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            resultTbx = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            authorityCbx = new System.Windows.Forms.ComboBox();
            clientIdCbx = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            loginHintTxt = new System.Windows.Forms.TextBox();
            promptCbx = new System.Windows.Forms.ComboBox();
            atsBtn = new System.Windows.Forms.Button();
            atiBtn = new System.Windows.Forms.Button();
            atsAtiBtn = new System.Windows.Forms.Button();
            accBtn = new System.Windows.Forms.Button();
            clearBtn = new System.Windows.Forms.Button();
            btnClearCache = new System.Windows.Forms.Button();
            cbxScopes = new System.Windows.Forms.ComboBox();
            label5 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            cbxAccount = new System.Windows.Forms.ComboBox();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            cbxMsaPt = new System.Windows.Forms.CheckBox();
            btnExpire = new System.Windows.Forms.Button();
            btnRemoveAccount = new System.Windows.Forms.Button();
            cbxBackgroundThread = new System.Windows.Forms.CheckBox();
            cbxListOsAccounts = new System.Windows.Forms.CheckBox();
            cbxUseWam = new System.Windows.Forms.ComboBox();
            cbxPOP = new System.Windows.Forms.CheckBox();
            UsernameTxt = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            PasswordTxt = new System.Windows.Forms.TextBox();
            atUsernamePwdBtn = new System.Windows.Forms.Button();
            btnATSperf = new System.Windows.Forms.Button();
            nudAutocancelSeconds = new System.Windows.Forms.NumericUpDown();
            label9 = new System.Windows.Forms.Label();
            cbxMultiCloud2 = new System.Windows.Forms.CheckBox();
            cbxWithForceRefresh = new System.Windows.Forms.CheckBox();
            btn_ATSDeviceCodeFlow = new System.Windows.Forms.Button();
            atiSshBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)nudAutocancelSeconds).BeginInit();
            SuspendLayout();
            // 
            // resultTbx
            // 
            resultTbx.Location = new System.Drawing.Point(14, 421);
            resultTbx.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            resultTbx.Multiline = true;
            resultTbx.Name = "resultTbx";
            resultTbx.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            resultTbx.Size = new System.Drawing.Size(810, 519);
            resultTbx.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(25, 69);
            label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(70, 20);
            label1.TabIndex = 2;
            label1.Text = "Authority";
            // 
            // authorityCbx
            // 
            authorityCbx.FormattingEnabled = true;
            authorityCbx.Items.AddRange(new object[] { "https://login.microsoftonline.com/common", "https://login.microsoftonline.com/organizations", "https://login.microsoftonline.com/consumers", "https://login.microsoftonline.com/49f548d0-12b7-4169-a390-bb5304d24462", "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47", "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a", "https://login.windows-ppe.net/organizations", "https://login.windows-ppe.net/72f988bf-86f1-41af-91ab-2d7cd011db47", "https://login.partner.microsoftonline.cn/organizations", "https://login.microsoftonline.us/organizations" });
            authorityCbx.Location = new System.Drawing.Point(97, 64);
            authorityCbx.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            authorityCbx.Name = "authorityCbx";
            authorityCbx.Size = new System.Drawing.Size(649, 28);
            authorityCbx.TabIndex = 3;
            authorityCbx.Text = "https://login.microsoftonline.com/common";
            // 
            // clientIdCbx
            // 
            clientIdCbx.FormattingEnabled = true;
            clientIdCbx.Location = new System.Drawing.Point(97, 23);
            clientIdCbx.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            clientIdCbx.Name = "clientIdCbx";
            clientIdCbx.Size = new System.Drawing.Size(649, 28);
            clientIdCbx.TabIndex = 4;
            clientIdCbx.Text = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
            clientIdCbx.SelectedIndexChanged += clientIdCbx_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(33, 28);
            label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(60, 20);
            label2.TabIndex = 5;
            label2.Text = "ClientId";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(11, 151);
            label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(82, 20);
            label3.TabIndex = 7;
            label3.Text = "Login Hint ";
            // 
            // loginHintTxt
            // 
            loginHintTxt.Location = new System.Drawing.Point(97, 148);
            loginHintTxt.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            loginHintTxt.Name = "loginHintTxt";
            loginHintTxt.Size = new System.Drawing.Size(292, 27);
            loginHintTxt.TabIndex = 8;
            // 
            // promptCbx
            // 
            promptCbx.FormattingEnabled = true;
            promptCbx.Items.AddRange(new object[] { "", "select_account", "force_login", "no_prompt", "consent", "never" });
            promptCbx.Location = new System.Drawing.Point(663, 199);
            promptCbx.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            promptCbx.Name = "promptCbx";
            promptCbx.Size = new System.Drawing.Size(159, 28);
            promptCbx.TabIndex = 10;
            // 
            // atsBtn
            // 
            atsBtn.Location = new System.Drawing.Point(11, 329);
            atsBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            atsBtn.Name = "atsBtn";
            atsBtn.Size = new System.Drawing.Size(144, 36);
            atsBtn.TabIndex = 11;
            atsBtn.Text = "ATS";
            atsBtn.UseVisualStyleBackColor = true;
            atsBtn.Click += atsBtn_Click;
            // 
            // atiBtn
            // 
            atiBtn.Location = new System.Drawing.Point(165, 329);
            atiBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            atiBtn.Name = "atiBtn";
            atiBtn.Size = new System.Drawing.Size(144, 36);
            atiBtn.TabIndex = 12;
            atiBtn.Text = "ATI";
            atiBtn.UseVisualStyleBackColor = true;
            atiBtn.Click += atiBtn_Click;
            // 
            // atsAtiBtn
            // 
            atsAtiBtn.Location = new System.Drawing.Point(318, 329);
            atsAtiBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            atsAtiBtn.Name = "atsAtiBtn";
            atsAtiBtn.Size = new System.Drawing.Size(144, 36);
            atsAtiBtn.TabIndex = 13;
            atsAtiBtn.Text = "ATS + ATI";
            atsAtiBtn.UseVisualStyleBackColor = true;
            atsAtiBtn.Click += atsAtiBtn_Click;
            // 
            // accBtn
            // 
            accBtn.Location = new System.Drawing.Point(261, 373);
            accBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            accBtn.Name = "accBtn";
            accBtn.Size = new System.Drawing.Size(144, 36);
            accBtn.TabIndex = 15;
            accBtn.Text = "Get Accounts";
            accBtn.UseVisualStyleBackColor = true;
            accBtn.Click += getAccountsBtn_Click;
            // 
            // clearBtn
            // 
            clearBtn.Location = new System.Drawing.Point(734, 1005);
            clearBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            clearBtn.Name = "clearBtn";
            clearBtn.Size = new System.Drawing.Size(90, 36);
            clearBtn.TabIndex = 16;
            clearBtn.Text = "Clear Log";
            clearBtn.UseVisualStyleBackColor = true;
            clearBtn.Click += clearBtn_Click;
            // 
            // btnClearCache
            // 
            btnClearCache.Location = new System.Drawing.Point(581, 1005);
            btnClearCache.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnClearCache.Name = "btnClearCache";
            btnClearCache.Size = new System.Drawing.Size(144, 36);
            btnClearCache.TabIndex = 17;
            btnClearCache.Text = "Clear MSAL Cache";
            btnClearCache.UseVisualStyleBackColor = true;
            btnClearCache.Click += btnClearCache_Click;
            // 
            // cbxScopes
            // 
            cbxScopes.FormattingEnabled = true;
            cbxScopes.Items.AddRange(new object[] { "User.Read", "User.Read User.Read.All", "https://management.core.windows.net//.default", "https://graph.microsoft.com/.default", "499b84ac-1321-427f-aa17-267ca6975798/vso.code_full", "api://51eb3dd6-d8b5-46f3-991d-b1d4870de7de/myaccess", "https://management.core.chinacloudapi.cn//.default", "https://management.core.usgovcloudapi.net//.default" });
            cbxScopes.Location = new System.Drawing.Point(97, 107);
            cbxScopes.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            cbxScopes.Name = "cbxScopes";
            cbxScopes.Size = new System.Drawing.Size(725, 28);
            cbxScopes.TabIndex = 18;
            cbxScopes.Text = "User.Read";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(32, 111);
            label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(56, 20);
            label5.TabIndex = 19;
            label5.Text = "Scopes";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(601, 204);
            label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(58, 20);
            label4.TabIndex = 9;
            label4.Text = "Prompt";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(399, 151);
            label6.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(83, 20);
            label6.TabIndex = 21;
            label6.Text = "Or Account";
            // 
            // cbxAccount
            // 
            cbxAccount.FormattingEnabled = true;
            cbxAccount.Location = new System.Drawing.Point(488, 147);
            cbxAccount.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            cbxAccount.Name = "cbxAccount";
            cbxAccount.Size = new System.Drawing.Size(334, 28);
            cbxAccount.TabIndex = 22;
            // 
            // cbxMsaPt
            // 
            cbxMsaPt.AutoSize = true;
            cbxMsaPt.Location = new System.Drawing.Point(242, 263);
            cbxMsaPt.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            cbxMsaPt.Name = "cbxMsaPt";
            cbxMsaPt.Size = new System.Drawing.Size(147, 24);
            cbxMsaPt.TabIndex = 23;
            cbxMsaPt.Text = "MSA-Passthrough";
            cbxMsaPt.UseVisualStyleBackColor = true;
            // 
            // btnExpire
            // 
            btnExpire.Location = new System.Drawing.Point(427, 1007);
            btnExpire.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnExpire.Name = "btnExpire";
            btnExpire.Size = new System.Drawing.Size(144, 36);
            btnExpire.TabIndex = 24;
            btnExpire.Text = "Expire ATs";
            btnExpire.UseVisualStyleBackColor = true;
            btnExpire.Click += btnExpire_Click;
            // 
            // btnRemoveAccount
            // 
            btnRemoveAccount.Location = new System.Drawing.Point(414, 373);
            btnRemoveAccount.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnRemoveAccount.Name = "btnRemoveAccount";
            btnRemoveAccount.Size = new System.Drawing.Size(144, 36);
            btnRemoveAccount.TabIndex = 25;
            btnRemoveAccount.Text = "Remove Acc";
            btnRemoveAccount.UseVisualStyleBackColor = true;
            btnRemoveAccount.Click += btnRemoveAcc_Click;
            // 
            // cbxBackgroundThread
            // 
            cbxBackgroundThread.AutoSize = true;
            cbxBackgroundThread.Location = new System.Drawing.Point(399, 263);
            cbxBackgroundThread.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            cbxBackgroundThread.Name = "cbxBackgroundThread";
            cbxBackgroundThread.Size = new System.Drawing.Size(197, 24);
            cbxBackgroundThread.TabIndex = 26;
            cbxBackgroundThread.Text = "Force background thread";
            cbxBackgroundThread.UseVisualStyleBackColor = true;
            // 
            // cbxListOsAccounts
            // 
            cbxListOsAccounts.AutoSize = true;
            cbxListOsAccounts.Location = new System.Drawing.Point(242, 296);
            cbxListOsAccounts.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            cbxListOsAccounts.Name = "cbxListOsAccounts";
            cbxListOsAccounts.Size = new System.Drawing.Size(138, 24);
            cbxListOsAccounts.TabIndex = 27;
            cbxListOsAccounts.Text = "List OS accounts";
            cbxListOsAccounts.UseVisualStyleBackColor = true;
            // 
            // cbxUseWam
            // 
            cbxUseWam.FormattingEnabled = true;
            cbxUseWam.Location = new System.Drawing.Point(11, 257);
            cbxUseWam.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            cbxUseWam.Name = "cbxUseWam";
            cbxUseWam.Size = new System.Drawing.Size(214, 28);
            cbxUseWam.TabIndex = 28;
            // 
            // cbxPOP
            // 
            cbxPOP.AutoSize = true;
            cbxPOP.Location = new System.Drawing.Point(399, 296);
            cbxPOP.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            cbxPOP.Name = "cbxPOP";
            cbxPOP.Size = new System.Drawing.Size(191, 24);
            cbxPOP.TabIndex = 29;
            cbxPOP.Text = "With Proof-of-Possesion";
            cbxPOP.UseVisualStyleBackColor = true;
            // 
            // UsernameTxt
            // 
            UsernameTxt.Location = new System.Drawing.Point(97, 197);
            UsernameTxt.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            UsernameTxt.Name = "UsernameTxt";
            UsernameTxt.Size = new System.Drawing.Size(175, 27);
            UsernameTxt.TabIndex = 30;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(11, 203);
            label7.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(75, 20);
            label7.TabIndex = 31;
            label7.Text = "Username";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(282, 201);
            label8.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(70, 20);
            label8.TabIndex = 32;
            label8.Text = "Password";
            // 
            // PasswordTxt
            // 
            PasswordTxt.Location = new System.Drawing.Point(367, 199);
            PasswordTxt.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            PasswordTxt.Name = "PasswordTxt";
            PasswordTxt.Size = new System.Drawing.Size(175, 27);
            PasswordTxt.TabIndex = 33;
            PasswordTxt.UseSystemPasswordChar = true;
            // 
            // atUsernamePwdBtn
            // 
            atUsernamePwdBtn.Location = new System.Drawing.Point(165, 373);
            atUsernamePwdBtn.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            atUsernamePwdBtn.Name = "atUsernamePwdBtn";
            atUsernamePwdBtn.Size = new System.Drawing.Size(87, 36);
            atUsernamePwdBtn.TabIndex = 34;
            atUsernamePwdBtn.Text = "AT U/P";
            atUsernamePwdBtn.UseVisualStyleBackColor = true;
            atUsernamePwdBtn.Click += atUsernamePwdBtn_Click;
            // 
            // btnATSperf
            // 
            btnATSperf.Location = new System.Drawing.Point(11, 373);
            btnATSperf.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            btnATSperf.Name = "btnATSperf";
            btnATSperf.Size = new System.Drawing.Size(144, 36);
            btnATSperf.TabIndex = 30;
            btnATSperf.Text = "ATS Perf";
            btnATSperf.UseVisualStyleBackColor = true;
            btnATSperf.Click += btnATSperf_Click;
            // 
            // nudAutocancelSeconds
            // 
            nudAutocancelSeconds.Location = new System.Drawing.Point(167, 292);
            nudAutocancelSeconds.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            nudAutocancelSeconds.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            nudAutocancelSeconds.Name = "nudAutocancelSeconds";
            nudAutocancelSeconds.Size = new System.Drawing.Size(66, 27);
            nudAutocancelSeconds.TabIndex = 30;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(11, 297);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(142, 20);
            label9.TabIndex = 31;
            label9.Text = "Autocancel Seconds";
            // 
            // cbxMultiCloud2
            // 
            cbxMultiCloud2.AutoSize = true;
            cbxMultiCloud2.Location = new System.Drawing.Point(590, 265);
            cbxMultiCloud2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbxMultiCloud2.Name = "cbxMultiCloud2";
            cbxMultiCloud2.Size = new System.Drawing.Size(165, 24);
            cbxMultiCloud2.TabIndex = 35;
            cbxMultiCloud2.Text = "Multi Cloud Support";
            cbxMultiCloud2.UseVisualStyleBackColor = true;
            // 
            // cbxWithForceRefresh
            // 
            cbxWithForceRefresh.AutoSize = true;
            cbxWithForceRefresh.Location = new System.Drawing.Point(590, 299);
            cbxWithForceRefresh.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            cbxWithForceRefresh.Name = "cbxWithForceRefresh";
            cbxWithForceRefresh.Size = new System.Drawing.Size(147, 24);
            cbxWithForceRefresh.TabIndex = 36;
            cbxWithForceRefresh.Text = "WithForceRefresh";
            cbxWithForceRefresh.UseVisualStyleBackColor = true;
            // 
            // btn_ATSDeviceCodeFlow
            // 
            btn_ATSDeviceCodeFlow.Location = new System.Drawing.Point(566, 373);
            btn_ATSDeviceCodeFlow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btn_ATSDeviceCodeFlow.Name = "btn_ATSDeviceCodeFlow";
            btn_ATSDeviceCodeFlow.Size = new System.Drawing.Size(160, 36);
            btn_ATSDeviceCodeFlow.TabIndex = 37;
            btn_ATSDeviceCodeFlow.Text = "AT DeviceCodeFlow";
            btn_ATSDeviceCodeFlow.UseVisualStyleBackColor = true;
            btn_ATSDeviceCodeFlow.Click += btn_ATSDeviceCodeFlow_Click;
            // 
            // atiSshBtn
            // 
            atiSshBtn.Location = new System.Drawing.Point(470, 331);
            atiSshBtn.Name = "atiSshBtn";
            atiSshBtn.Size = new System.Drawing.Size(196, 33);
            atiSshBtn.TabIndex = 38;
            atiSshBtn.Text = "ATI w/ SSHAuth";
            atiSshBtn.UseVisualStyleBackColor = true;
            atiSshBtn.Click += atiSshBtn_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(843, 1059);
            Controls.Add(atiSshBtn);
            Controls.Add(btn_ATSDeviceCodeFlow);
            Controls.Add(cbxWithForceRefresh);
            Controls.Add(cbxMultiCloud2);
            Controls.Add(atUsernamePwdBtn);
            Controls.Add(PasswordTxt);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(UsernameTxt);
            Controls.Add(btnATSperf);
            Controls.Add(label9);
            Controls.Add(nudAutocancelSeconds);
            Controls.Add(cbxPOP);
            Controls.Add(cbxUseWam);
            Controls.Add(cbxListOsAccounts);
            Controls.Add(cbxBackgroundThread);
            Controls.Add(btnRemoveAccount);
            Controls.Add(btnExpire);
            Controls.Add(cbxMsaPt);
            Controls.Add(cbxAccount);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(cbxScopes);
            Controls.Add(btnClearCache);
            Controls.Add(clearBtn);
            Controls.Add(accBtn);
            Controls.Add(atsAtiBtn);
            Controls.Add(atiBtn);
            Controls.Add(atsBtn);
            Controls.Add(promptCbx);
            Controls.Add(label4);
            Controls.Add(loginHintTxt);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(clientIdCbx);
            Controls.Add(authorityCbx);
            Controls.Add(label1);
            Controls.Add(resultTbx);
            Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)nudAutocancelSeconds).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox resultTbx;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox authorityCbx;
        private System.Windows.Forms.ComboBox clientIdCbx;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox loginHintTxt;
        private System.Windows.Forms.ComboBox promptCbx;
        private System.Windows.Forms.Button atsBtn;
        private System.Windows.Forms.Button atiBtn;
        private System.Windows.Forms.Button atsAtiBtn;
        private System.Windows.Forms.Button accBtn;
        private System.Windows.Forms.Button clearBtn;
        private System.Windows.Forms.Button btnClearCache;
        private System.Windows.Forms.ComboBox cbxScopes;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbxAccount;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox cbxMsaPt;
        private System.Windows.Forms.Button btnExpire;
        private System.Windows.Forms.Button btnRemoveAccount;
        private System.Windows.Forms.CheckBox cbxBackgroundThread;
        private System.Windows.Forms.CheckBox cbxListOsAccounts;
        private System.Windows.Forms.ComboBox cbxUseWam;
        private System.Windows.Forms.CheckBox cbxPOP;
        private System.Windows.Forms.TextBox UsernameTxt;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox PasswordTxt;
        private System.Windows.Forms.Button atUsernamePwdBtn;
        private System.Windows.Forms.Button btnATSperf;
        private System.Windows.Forms.NumericUpDown nudAutocancelSeconds;
        private System.Windows.Forms.Label label9;

        private System.Windows.Forms.CheckBox cbxMultiCloud2;
        private System.Windows.Forms.CheckBox cbxWithForceRefresh;
        private System.Windows.Forms.Button btn_ATSDeviceCodeFlow;
        private System.Windows.Forms.Button atiSshBtn;
    }
}

