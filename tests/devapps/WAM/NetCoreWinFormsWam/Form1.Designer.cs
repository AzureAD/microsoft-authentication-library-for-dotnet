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
            this.components = new System.ComponentModel.Container();
            this.resultTbx = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.authorityCbx = new System.Windows.Forms.ComboBox();
            this.clientIdCbx = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.loginHintTxt = new System.Windows.Forms.TextBox();
            this.promptCbx = new System.Windows.Forms.ComboBox();
            this.atsBtn = new System.Windows.Forms.Button();
            this.atiBtn = new System.Windows.Forms.Button();
            this.atsAtiBtn = new System.Windows.Forms.Button();
            this.accBtn = new System.Windows.Forms.Button();
            this.clearBtn = new System.Windows.Forms.Button();
            this.btnClearCache = new System.Windows.Forms.Button();
            this.cbxScopes = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cbxAccount = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.cbxMsaPt = new System.Windows.Forms.CheckBox();
            this.btnExpire = new System.Windows.Forms.Button();
            this.btnRemoveAccount = new System.Windows.Forms.Button();
            this.cbxBackgroundThread = new System.Windows.Forms.CheckBox();
            this.cbxListOsAccounts = new System.Windows.Forms.CheckBox();
            this.cbxUseWam = new System.Windows.Forms.ComboBox();
            this.cbxPOP = new System.Windows.Forms.CheckBox();
            this.UsernameTxt = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.PasswordTxt = new System.Windows.Forms.TextBox();
            this.atUsernamePwdBtn = new System.Windows.Forms.Button();
            this.btnATSperf = new System.Windows.Forms.Button();
            this.nudAutocancelSeconds = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudAutocancelSeconds)).BeginInit();
            this.cbxMultiCloud2 = new System.Windows.Forms.CheckBox();

            this.SuspendLayout();
            // 
            // resultTbx
            // 
            this.resultTbx.Location = new System.Drawing.Point(12, 316);
            this.resultTbx.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.resultTbx.Multiline = true;
            this.resultTbx.Name = "resultTbx";
            this.resultTbx.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.resultTbx.Size = new System.Drawing.Size(709, 390);
            this.resultTbx.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 52);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Authority";
            // 
            // authorityCbx
            // 
            this.authorityCbx.FormattingEnabled = true;
            this.authorityCbx.Items.AddRange(new object[] {
            "https://login.microsoftonline.com/common",
            "https://login.microsoftonline.com/organizations",
            "https://login.microsoftonline.com/consumers",
            "https://login.microsoftonline.com/49f548d0-12b7-4169-a390-bb5304d24462",
            "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca",
            "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47",
            "https://login.microsoftonline.com/f8cdef31-a31e-4b4a-93e4-5f571e91255a",
            "https://login.windows-ppe.net/organizations",
            "https://login.windows-ppe.net/72f988bf-86f1-41af-91ab-2d7cd011db47",
            "https://login.partner.microsoftonline.cn/organizations",
            "https://login.microsoftonline.us/organizations"});
            this.authorityCbx.Location = new System.Drawing.Point(85, 48);
            this.authorityCbx.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.authorityCbx.Name = "authorityCbx";
            this.authorityCbx.Size = new System.Drawing.Size(568, 23);
            this.authorityCbx.TabIndex = 3;
            this.authorityCbx.Text = "https://login.microsoftonline.com/common";
            // 
            // clientIdCbx
            // 
            this.clientIdCbx.FormattingEnabled = true;
            this.clientIdCbx.Location = new System.Drawing.Point(85, 17);
            this.clientIdCbx.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.clientIdCbx.Name = "clientIdCbx";
            this.clientIdCbx.Size = new System.Drawing.Size(568, 23);
            this.clientIdCbx.TabIndex = 4;
            this.clientIdCbx.Text = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
            this.clientIdCbx.SelectedIndexChanged += new System.EventHandler(this.clientIdCbx_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "ClientId";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 113);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 15);
            this.label3.TabIndex = 7;
            this.label3.Text = "Login Hint ";
            // 
            // loginHintTxt
            // 
            this.loginHintTxt.Location = new System.Drawing.Point(85, 111);
            this.loginHintTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.loginHintTxt.Name = "loginHintTxt";
            this.loginHintTxt.Size = new System.Drawing.Size(256, 23);
            this.loginHintTxt.TabIndex = 8;
            // 
            // promptCbx
            // 
            this.promptCbx.FormattingEnabled = true;
            this.promptCbx.Items.AddRange(new object[] {
            "",
            "select_account",
            "force_login",
            "no_prompt",
            "consent",
            "never"});
            this.promptCbx.Location = new System.Drawing.Point(580, 149);
            this.promptCbx.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.promptCbx.Name = "promptCbx";
            this.promptCbx.Size = new System.Drawing.Size(140, 23);
            this.promptCbx.TabIndex = 10;
            // 
            // atsBtn
            // 
            this.atsBtn.Location = new System.Drawing.Point(10, 247);
            this.atsBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.atsBtn.Name = "atsBtn";
            this.atsBtn.Size = new System.Drawing.Size(126, 27);
            this.atsBtn.TabIndex = 11;
            this.atsBtn.Text = "ATS";
            this.atsBtn.UseVisualStyleBackColor = true;
            this.atsBtn.Click += new System.EventHandler(this.atsBtn_Click);
            // 
            // atiBtn
            // 
            this.atiBtn.Location = new System.Drawing.Point(144, 247);
            this.atiBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.atiBtn.Name = "atiBtn";
            this.atiBtn.Size = new System.Drawing.Size(126, 27);
            this.atiBtn.TabIndex = 12;
            this.atiBtn.Text = "ATI";
            this.atiBtn.UseVisualStyleBackColor = true;
            this.atiBtn.Click += new System.EventHandler(this.atiBtn_Click);
            // 
            // atsAtiBtn
            // 
            this.atsAtiBtn.Location = new System.Drawing.Point(278, 247);
            this.atsAtiBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.atsAtiBtn.Name = "atsAtiBtn";
            this.atsAtiBtn.Size = new System.Drawing.Size(126, 27);
            this.atsAtiBtn.TabIndex = 13;
            this.atsAtiBtn.Text = "ATS + ATI";
            this.atsAtiBtn.UseVisualStyleBackColor = true;
            this.atsAtiBtn.Click += new System.EventHandler(this.atsAtiBtn_Click);
            // 
            // accBtn
            // 
            this.accBtn.Location = new System.Drawing.Point(228, 280);
            this.accBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.accBtn.Name = "accBtn";
            this.accBtn.Size = new System.Drawing.Size(126, 27);
            this.accBtn.TabIndex = 15;
            this.accBtn.Text = "Get Accounts";
            this.accBtn.UseVisualStyleBackColor = true;
            this.accBtn.Click += new System.EventHandler(this.getAccountsBtn_Click);
            // 
            // clearBtn
            // 
            this.clearBtn.Location = new System.Drawing.Point(642, 754);
            this.clearBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(79, 27);
            this.clearBtn.TabIndex = 16;
            this.clearBtn.Text = "Clear Log";
            this.clearBtn.UseVisualStyleBackColor = true;
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // btnClearCache
            // 
            this.btnClearCache.Location = new System.Drawing.Point(508, 754);
            this.btnClearCache.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnClearCache.Name = "btnClearCache";
            this.btnClearCache.Size = new System.Drawing.Size(126, 27);
            this.btnClearCache.TabIndex = 17;
            this.btnClearCache.Text = "Clear MSAL Cache";
            this.btnClearCache.UseVisualStyleBackColor = true;
            this.btnClearCache.Click += new System.EventHandler(this.btnClearCache_Click);
            // 
            // cbxScopes
            // 
            this.cbxScopes.FormattingEnabled = true;
            this.cbxScopes.Items.AddRange(new object[] {
            "User.Read",
            "User.Read User.Read.All",
            "https://management.core.windows.net//.default",
            "https://graph.microsoft.com/.default",
            "499b84ac-1321-427f-aa17-267ca6975798/vso.code_full",
            "api://51eb3dd6-d8b5-46f3-991d-b1d4870de7de/myaccess",
            "https://management.core.chinacloudapi.cn//.default",
            "https://management.core.usgovcloudapi.net//.default"});
            this.cbxScopes.Location = new System.Drawing.Point(85, 80);
            this.cbxScopes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbxScopes.Name = "cbxScopes";
            this.cbxScopes.Size = new System.Drawing.Size(635, 23);
            this.cbxScopes.TabIndex = 18;
            this.cbxScopes.Text = "User.Read";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(28, 83);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 15);
            this.label5.TabIndex = 19;
            this.label5.Text = "Scopes";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(526, 153);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 15);
            this.label4.TabIndex = 9;
            this.label4.Text = "Prompt";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(349, 113);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 15);
            this.label6.TabIndex = 21;
            this.label6.Text = "Or Account";
            // 
            // cbxAccount
            // 
            this.cbxAccount.FormattingEnabled = true;
            this.cbxAccount.Location = new System.Drawing.Point(427, 110);
            this.cbxAccount.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbxAccount.Name = "cbxAccount";
            this.cbxAccount.Size = new System.Drawing.Size(293, 23);
            this.cbxAccount.TabIndex = 22;
            // 
            // cbxMsaPt
            // 
            this.cbxMsaPt.AutoSize = true;
            this.cbxMsaPt.Location = new System.Drawing.Point(212, 197);
            this.cbxMsaPt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbxMsaPt.Name = "cbxMsaPt";
            this.cbxMsaPt.Size = new System.Drawing.Size(122, 19);
            this.cbxMsaPt.TabIndex = 23;
            this.cbxMsaPt.Text = "MSA-Passthrough";
            this.cbxMsaPt.UseVisualStyleBackColor = true;
            // 
            // btnExpire
            // 
            this.btnExpire.Location = new System.Drawing.Point(374, 755);
            this.btnExpire.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnExpire.Name = "btnExpire";
            this.btnExpire.Size = new System.Drawing.Size(126, 27);
            this.btnExpire.TabIndex = 24;
            this.btnExpire.Text = "Expire ATs";
            this.btnExpire.UseVisualStyleBackColor = true;
            this.btnExpire.Click += new System.EventHandler(this.btnExpire_Click);
            // 
            // btnRemoveAccount
            // 
            this.btnRemoveAccount.Location = new System.Drawing.Point(362, 280);
            this.btnRemoveAccount.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnRemoveAccount.Name = "btnRemoveAccount";
            this.btnRemoveAccount.Size = new System.Drawing.Size(126, 27);
            this.btnRemoveAccount.TabIndex = 25;
            this.btnRemoveAccount.Text = "Remove Acc";
            this.btnRemoveAccount.UseVisualStyleBackColor = true;
            this.btnRemoveAccount.Click += new System.EventHandler(this.btnRemoveAcc_Click);
            // 
            // cbxBackgroundThread
            // 
            this.cbxBackgroundThread.AutoSize = true;
            this.cbxBackgroundThread.Location = new System.Drawing.Point(349, 197);
            this.cbxBackgroundThread.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbxBackgroundThread.Name = "cbxBackgroundThread";
            this.cbxBackgroundThread.Size = new System.Drawing.Size(159, 19);
            this.cbxBackgroundThread.TabIndex = 26;
            this.cbxBackgroundThread.Text = "Force background thread";
            this.cbxBackgroundThread.UseVisualStyleBackColor = true;
            // 
            // cbxListOsAccounts
            // 
            this.cbxListOsAccounts.AutoSize = true;
            this.cbxListOsAccounts.Location = new System.Drawing.Point(212, 222);
            this.cbxListOsAccounts.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbxListOsAccounts.Name = "cbxListOsAccounts";
            this.cbxListOsAccounts.Size = new System.Drawing.Size(113, 19);
            this.cbxListOsAccounts.TabIndex = 27;
            this.cbxListOsAccounts.Text = "List OS accounts";
            this.cbxListOsAccounts.UseVisualStyleBackColor = true;
            // 
            // cbxUseWam
            // 
            this.cbxUseWam.FormattingEnabled = true;
            this.cbxUseWam.Location = new System.Drawing.Point(10, 193);
            this.cbxUseWam.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.cbxUseWam.Name = "cbxUseWam";
            this.cbxUseWam.Size = new System.Drawing.Size(188, 23);
            this.cbxUseWam.TabIndex = 28;
            // 
            // cbxPOP
            // 
            this.cbxPOP.AutoSize = true;
            this.cbxPOP.Location = new System.Drawing.Point(349, 222);
            this.cbxPOP.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.cbxPOP.Name = "cbxPOP";
            this.cbxPOP.Size = new System.Drawing.Size(156, 19);
            this.cbxPOP.TabIndex = 29;
            this.cbxPOP.Text = "With Proof-of-Possesion";
            this.cbxPOP.UseVisualStyleBackColor = true;
            // 
            // UsernameTxt
            // 
            this.UsernameTxt.Location = new System.Drawing.Point(85, 148);
            this.UsernameTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.UsernameTxt.Name = "UsernameTxt";
            this.UsernameTxt.Size = new System.Drawing.Size(154, 23);
            this.UsernameTxt.TabIndex = 30;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 152);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(60, 15);
            this.label7.TabIndex = 31;
            this.label7.Text = "Username";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(247, 151);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(57, 15);
            this.label8.TabIndex = 32;
            this.label8.Text = "Password";
            // 
            // PasswordTxt
            // 
            this.PasswordTxt.Location = new System.Drawing.Point(321, 149);
            this.PasswordTxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.PasswordTxt.Name = "PasswordTxt";
            this.PasswordTxt.Size = new System.Drawing.Size(154, 23);
            this.PasswordTxt.TabIndex = 33;
            this.PasswordTxt.UseSystemPasswordChar = true;
            // 
            // atUsernamePwdBtn
            // 
            this.atUsernamePwdBtn.Location = new System.Drawing.Point(144, 280);
            this.atUsernamePwdBtn.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.atUsernamePwdBtn.Name = "atUsernamePwdBtn";
            this.atUsernamePwdBtn.Size = new System.Drawing.Size(76, 27);
            this.atUsernamePwdBtn.TabIndex = 34;
            this.atUsernamePwdBtn.Text = "AT U/P";
            this.atUsernamePwdBtn.UseVisualStyleBackColor = true;
            this.atUsernamePwdBtn.Click += new System.EventHandler(this.atUsernamePwdBtn_Click);
            // 
            // btnATSperf
            // 
            this.btnATSperf.Location = new System.Drawing.Point(10, 280);
            this.btnATSperf.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnATSperf.Name = "btnATSperf";
            this.btnATSperf.Size = new System.Drawing.Size(126, 27);
            this.btnATSperf.TabIndex = 30;
            this.btnATSperf.Text = "ATS Perf";
            this.btnATSperf.UseVisualStyleBackColor = true;
            this.btnATSperf.Click += new System.EventHandler(this.btnATSperf_Click);
            // 
            // nudAutocancelSeconds
            // 
            this.nudAutocancelSeconds.Location = new System.Drawing.Point(146, 219);
            this.nudAutocancelSeconds.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.nudAutocancelSeconds.Name = "nudAutocancelSeconds";
            this.nudAutocancelSeconds.Size = new System.Drawing.Size(58, 23);
            this.nudAutocancelSeconds.TabIndex = 30;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(10, 223);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(114, 15);
            this.label9.TabIndex = 31;
            this.label9.Text = "Autocancel Seconds";

            // cbxMultiCloud2
            // 
            this.cbxMultiCloud2.AutoSize = true;
            this.cbxMultiCloud2.Location = new System.Drawing.Point(516, 199);
            this.cbxMultiCloud2.Name = "cbxMultiCloud2";
            this.cbxMultiCloud2.Size = new System.Drawing.Size(134, 19);
            this.cbxMultiCloud2.TabIndex = 35;
            this.cbxMultiCloud2.Text = "Multi Cloud Support";
            this.cbxMultiCloud2.UseVisualStyleBackColor = true;

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(735, 740);

            this.ClientSize = new System.Drawing.Size(738, 794);
            this.Controls.Add(this.cbxMultiCloud2);

            this.Controls.Add(this.atUsernamePwdBtn);
            this.Controls.Add(this.PasswordTxt);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.UsernameTxt);
            this.Controls.Add(this.btnATSperf);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.nudAutocancelSeconds);
            this.Controls.Add(this.cbxPOP);
            this.Controls.Add(this.cbxUseWam);
            this.Controls.Add(this.cbxListOsAccounts);
            this.Controls.Add(this.cbxBackgroundThread);
            this.Controls.Add(this.btnRemoveAccount);
            this.Controls.Add(this.btnExpire);
            this.Controls.Add(this.cbxMsaPt);
            this.Controls.Add(this.cbxAccount);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbxScopes);
            this.Controls.Add(this.btnClearCache);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.accBtn);
            this.Controls.Add(this.atsAtiBtn);
            this.Controls.Add(this.atiBtn);
            this.Controls.Add(this.atsBtn);
            this.Controls.Add(this.promptCbx);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.loginHintTxt);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.clientIdCbx);
            this.Controls.Add(this.authorityCbx);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.resultTbx);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.nudAutocancelSeconds)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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

    }
}

