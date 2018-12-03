namespace AutomationApp
{
    partial class AutomationUI
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
            this.pageControl1 = new System.Windows.Forms.TabControl();
            this.mainPage = new System.Windows.Forms.TabPage();
            this.clearCache = new System.Windows.Forms.Button();
            this.readCache = new System.Windows.Forms.Button();
            this.invalidateToken = new System.Windows.Forms.Button();
            this.expireAccessToken = new System.Windows.Forms.Button();
            this.acquireTokenSilent = new System.Windows.Forms.Button();
            this.acquireToken = new System.Windows.Forms.Button();
            this.dataInputPage = new System.Windows.Forms.TabPage();
            this.GoBtn = new System.Windows.Forms.Button();
            this.dataInput = new System.Windows.Forms.TextBox();
            this.resultPage = new System.Windows.Forms.TabPage();
            this.messageResult = new System.Windows.Forms.Label();
            this.scopeResult = new System.Windows.Forms.ListBox();
            this.exceptionResult = new System.Windows.Forms.Label();
            this.scope = new System.Windows.Forms.Label();
            this.idTokenResult = new System.Windows.Forms.Label();
            this.idToken = new System.Windows.Forms.Label();
            this.userResult = new System.Windows.Forms.Label();
            this.user = new System.Windows.Forms.Label();
            this.tenantIdResult = new System.Windows.Forms.Label();
            this.tenantId = new System.Windows.Forms.Label();
            this.expiresOnResult = new System.Windows.Forms.Label();
            this.expiresOn = new System.Windows.Forms.Label();
            this.accessTokenResult = new System.Windows.Forms.Label();
            this.accessToken = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.msalLogs = new System.Windows.Forms.TextBox();
            this.Done = new System.Windows.Forms.Button();
            this.testResultBox = new System.Windows.Forms.TextBox();
            this.pageControl1.SuspendLayout();
            this.mainPage.SuspendLayout();
            this.dataInputPage.SuspendLayout();
            this.resultPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // pageControl1
            // 
            this.pageControl1.Controls.Add(this.mainPage);
            this.pageControl1.Controls.Add(this.dataInputPage);
            this.pageControl1.Controls.Add(this.resultPage);
            this.pageControl1.Location = new System.Drawing.Point(18, 15);
            this.pageControl1.Margin = new System.Windows.Forms.Padding(6);
            this.pageControl1.Name = "pageControl1";
            this.pageControl1.SelectedIndex = 0;
            this.pageControl1.Size = new System.Drawing.Size(1102, 1402);
            this.pageControl1.TabIndex = 0;
            // 
            // mainPage
            // 
            this.mainPage.Controls.Add(this.clearCache);
            this.mainPage.Controls.Add(this.readCache);
            this.mainPage.Controls.Add(this.invalidateToken);
            this.mainPage.Controls.Add(this.expireAccessToken);
            this.mainPage.Controls.Add(this.acquireTokenSilent);
            this.mainPage.Controls.Add(this.acquireToken);
            this.mainPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainPage.Location = new System.Drawing.Point(8, 39);
            this.mainPage.Margin = new System.Windows.Forms.Padding(6);
            this.mainPage.Name = "mainPage";
            this.mainPage.Padding = new System.Windows.Forms.Padding(6);
            this.mainPage.Size = new System.Drawing.Size(1086, 1355);
            this.mainPage.TabIndex = 0;
            this.mainPage.Text = "Main Page";
            this.mainPage.UseVisualStyleBackColor = true;
            // 
            // clearCache
            // 
            this.clearCache.Enabled = false;
            this.clearCache.Location = new System.Drawing.Point(324, 796);
            this.clearCache.Margin = new System.Windows.Forms.Padding(6);
            this.clearCache.Name = "clearCache";
            this.clearCache.Size = new System.Drawing.Size(470, 88);
            this.clearCache.TabIndex = 5;
            this.clearCache.Text = "Clear Cache";
            this.clearCache.UseVisualStyleBackColor = true;
            // 
            // readCache
            // 
            this.readCache.Enabled = false;
            this.readCache.Location = new System.Drawing.Point(322, 633);
            this.readCache.Margin = new System.Windows.Forms.Padding(6);
            this.readCache.Name = "readCache";
            this.readCache.Size = new System.Drawing.Size(472, 87);
            this.readCache.TabIndex = 4;
            this.readCache.Text = "Read Cache";
            this.readCache.UseVisualStyleBackColor = true;
            // 
            // invalidateToken
            // 
            this.invalidateToken.Enabled = false;
            this.invalidateToken.Location = new System.Drawing.Point(318, 483);
            this.invalidateToken.Margin = new System.Windows.Forms.Padding(6);
            this.invalidateToken.Name = "invalidateToken";
            this.invalidateToken.Size = new System.Drawing.Size(476, 85);
            this.invalidateToken.TabIndex = 3;
            this.invalidateToken.Text = "Invalidate Token";
            this.invalidateToken.UseVisualStyleBackColor = true;
            // 
            // expireAccessToken
            // 
            this.expireAccessToken.Location = new System.Drawing.Point(318, 348);
            this.expireAccessToken.Margin = new System.Windows.Forms.Padding(6);
            this.expireAccessToken.Name = "expireAccessToken";
            this.expireAccessToken.Size = new System.Drawing.Size(480, 85);
            this.expireAccessToken.TabIndex = 2;
            this.expireAccessToken.Text = "Expire Access Token";
            this.expireAccessToken.UseVisualStyleBackColor = true;
            this.expireAccessToken.Click += new System.EventHandler(this.expireAccessToken_Click);
            // 
            // acquireTokenSilent
            // 
            this.acquireTokenSilent.Location = new System.Drawing.Point(318, 212);
            this.acquireTokenSilent.Margin = new System.Windows.Forms.Padding(6);
            this.acquireTokenSilent.Name = "acquireTokenSilent";
            this.acquireTokenSilent.Size = new System.Drawing.Size(484, 88);
            this.acquireTokenSilent.TabIndex = 1;
            this.acquireTokenSilent.Text = "Acquire Token Silent";
            this.acquireTokenSilent.UseVisualStyleBackColor = true;
            this.acquireTokenSilent.Click += new System.EventHandler(this.acquireTokenSilent_Click);
            // 
            // acquireToken
            // 
            this.acquireToken.Location = new System.Drawing.Point(314, 67);
            this.acquireToken.Margin = new System.Windows.Forms.Padding(6);
            this.acquireToken.Name = "acquireToken";
            this.acquireToken.Size = new System.Drawing.Size(488, 92);
            this.acquireToken.TabIndex = 0;
            this.acquireToken.Text = "Acquire Token";
            this.acquireToken.UseVisualStyleBackColor = true;
            this.acquireToken.Click += new System.EventHandler(this.acquireToken_Click);
            // 
            // dataInputPage
            // 
            this.dataInputPage.Controls.Add(this.GoBtn);
            this.dataInputPage.Controls.Add(this.dataInput);
            this.dataInputPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dataInputPage.Location = new System.Drawing.Point(8, 39);
            this.dataInputPage.Margin = new System.Windows.Forms.Padding(6);
            this.dataInputPage.Name = "dataInputPage";
            this.dataInputPage.Padding = new System.Windows.Forms.Padding(6);
            this.dataInputPage.Size = new System.Drawing.Size(1086, 1355);
            this.dataInputPage.TabIndex = 1;
            this.dataInputPage.Text = "Data Input Page";
            this.dataInputPage.UseVisualStyleBackColor = true;
            // 
            // GoBtn
            // 
            this.GoBtn.BackColor = System.Drawing.Color.DarkOrange;
            this.GoBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GoBtn.Location = new System.Drawing.Point(382, 787);
            this.GoBtn.Margin = new System.Windows.Forms.Padding(6);
            this.GoBtn.Name = "GoBtn";
            this.GoBtn.Size = new System.Drawing.Size(270, 113);
            this.GoBtn.TabIndex = 1;
            this.GoBtn.Text = "Go";
            this.GoBtn.UseVisualStyleBackColor = false;
            this.GoBtn.Click += new System.EventHandler(this.GoBtn_Click);
            // 
            // dataInput
            // 
            this.dataInput.AccessibleName = "dataInput";
            this.dataInput.BackColor = System.Drawing.Color.Bisque;
            this.dataInput.Location = new System.Drawing.Point(206, 12);
            this.dataInput.Margin = new System.Windows.Forms.Padding(6);
            this.dataInput.Multiline = true;
            this.dataInput.Name = "dataInput";
            this.dataInput.Size = new System.Drawing.Size(654, 760);
            this.dataInput.TabIndex = 0;
            // 
            // resultPage
            // 
            this.resultPage.AutoScroll = true;
            this.resultPage.Controls.Add(this.testResultBox);
            this.resultPage.Controls.Add(this.messageResult);
            this.resultPage.Controls.Add(this.scopeResult);
            this.resultPage.Controls.Add(this.exceptionResult);
            this.resultPage.Controls.Add(this.scope);
            this.resultPage.Controls.Add(this.idTokenResult);
            this.resultPage.Controls.Add(this.idToken);
            this.resultPage.Controls.Add(this.userResult);
            this.resultPage.Controls.Add(this.user);
            this.resultPage.Controls.Add(this.tenantIdResult);
            this.resultPage.Controls.Add(this.tenantId);
            this.resultPage.Controls.Add(this.expiresOnResult);
            this.resultPage.Controls.Add(this.expiresOn);
            this.resultPage.Controls.Add(this.accessTokenResult);
            this.resultPage.Controls.Add(this.accessToken);
            this.resultPage.Controls.Add(this.label1);
            this.resultPage.Controls.Add(this.msalLogs);
            this.resultPage.Controls.Add(this.Done);
            this.resultPage.Location = new System.Drawing.Point(8, 39);
            this.resultPage.Margin = new System.Windows.Forms.Padding(6);
            this.resultPage.Name = "resultPage";
            this.resultPage.Padding = new System.Windows.Forms.Padding(6);
            this.resultPage.Size = new System.Drawing.Size(1086, 1355);
            this.resultPage.TabIndex = 2;
            this.resultPage.Text = "Result Page";
            this.resultPage.UseVisualStyleBackColor = true;
            // 
            // messageResult
            // 
            this.messageResult.AutoSize = true;
            this.messageResult.Location = new System.Drawing.Point(10, 517);
            this.messageResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.messageResult.Name = "messageResult";
            this.messageResult.Size = new System.Drawing.Size(268, 25);
            this.messageResult.TabIndex = 18;
            this.messageResult.Text = "Message Placeholder Text";
            // 
            // scopeResult
            // 
            this.scopeResult.FormattingEnabled = true;
            this.scopeResult.ItemHeight = 25;
            this.scopeResult.Location = new System.Drawing.Point(168, 265);
            this.scopeResult.Margin = new System.Windows.Forms.Padding(4);
            this.scopeResult.Name = "scopeResult";
            this.scopeResult.Size = new System.Drawing.Size(336, 179);
            this.scopeResult.TabIndex = 17;
            // 
            // exceptionResult
            // 
            this.exceptionResult.AutoSize = true;
            this.exceptionResult.Location = new System.Drawing.Point(460, 915);
            this.exceptionResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.exceptionResult.Name = "exceptionResult";
            this.exceptionResult.Size = new System.Drawing.Size(0, 25);
            this.exceptionResult.TabIndex = 16;
            // 
            // scope
            // 
            this.scope.AutoSize = true;
            this.scope.Location = new System.Drawing.Point(8, 265);
            this.scope.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.scope.Name = "scope";
            this.scope.Size = new System.Drawing.Size(73, 25);
            this.scope.TabIndex = 14;
            this.scope.Text = "Scope";
            // 
            // idTokenResult
            // 
            this.idTokenResult.AutoSize = true;
            this.idTokenResult.Location = new System.Drawing.Point(164, 215);
            this.idTokenResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.idTokenResult.Name = "idTokenResult";
            this.idTokenResult.Size = new System.Drawing.Size(174, 25);
            this.idTokenResult.TabIndex = 13;
            this.idTokenResult.Text = "Placeholder Text";
            // 
            // idToken
            // 
            this.idToken.AutoSize = true;
            this.idToken.Location = new System.Drawing.Point(8, 215);
            this.idToken.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.idToken.Name = "idToken";
            this.idToken.Size = new System.Drawing.Size(95, 25);
            this.idToken.TabIndex = 12;
            this.idToken.Text = "Id Token";
            // 
            // userResult
            // 
            this.userResult.AutoSize = true;
            this.userResult.Location = new System.Drawing.Point(164, 165);
            this.userResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.userResult.Name = "userResult";
            this.userResult.Size = new System.Drawing.Size(174, 25);
            this.userResult.TabIndex = 11;
            this.userResult.Text = "Placeholder Text";
            // 
            // user
            // 
            this.user.AutoSize = true;
            this.user.Location = new System.Drawing.Point(8, 165);
            this.user.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.user.Name = "user";
            this.user.Size = new System.Drawing.Size(57, 25);
            this.user.TabIndex = 10;
            this.user.Text = "User";
            // 
            // tenantIdResult
            // 
            this.tenantIdResult.AutoSize = true;
            this.tenantIdResult.Location = new System.Drawing.Point(164, 115);
            this.tenantIdResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.tenantIdResult.Name = "tenantIdResult";
            this.tenantIdResult.Size = new System.Drawing.Size(174, 25);
            this.tenantIdResult.TabIndex = 9;
            this.tenantIdResult.Text = "Placeholder Text";
            // 
            // tenantId
            // 
            this.tenantId.AutoSize = true;
            this.tenantId.Location = new System.Drawing.Point(8, 115);
            this.tenantId.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.tenantId.Name = "tenantId";
            this.tenantId.Size = new System.Drawing.Size(102, 25);
            this.tenantId.TabIndex = 8;
            this.tenantId.Text = "Tenant Id";
            // 
            // expiresOnResult
            // 
            this.expiresOnResult.AutoSize = true;
            this.expiresOnResult.Location = new System.Drawing.Point(164, 65);
            this.expiresOnResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.expiresOnResult.Name = "expiresOnResult";
            this.expiresOnResult.Size = new System.Drawing.Size(174, 25);
            this.expiresOnResult.TabIndex = 7;
            this.expiresOnResult.Text = "Placeholder Text";
            // 
            // expiresOn
            // 
            this.expiresOn.AutoSize = true;
            this.expiresOn.Location = new System.Drawing.Point(8, 65);
            this.expiresOn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.expiresOn.Name = "expiresOn";
            this.expiresOn.Size = new System.Drawing.Size(118, 25);
            this.expiresOn.TabIndex = 6;
            this.expiresOn.Text = "Expires On";
            // 
            // accessTokenResult
            // 
            this.accessTokenResult.AccessibleName = "accessToken";
            this.accessTokenResult.AutoSize = true;
            this.accessTokenResult.BackColor = System.Drawing.Color.Transparent;
            this.accessTokenResult.Location = new System.Drawing.Point(164, 15);
            this.accessTokenResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.accessTokenResult.Name = "accessTokenResult";
            this.accessTokenResult.Size = new System.Drawing.Size(174, 25);
            this.accessTokenResult.TabIndex = 5;
            this.accessTokenResult.Text = "Placeholder Text";
            // 
            // accessToken
            // 
            this.accessToken.AutoSize = true;
            this.accessToken.Location = new System.Drawing.Point(8, 15);
            this.accessToken.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.accessToken.Name = "accessToken";
            this.accessToken.Size = new System.Drawing.Size(148, 25);
            this.accessToken.TabIndex = 4;
            this.accessToken.Text = "Access Token";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(34, 987);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 25);
            this.label1.TabIndex = 3;
            this.label1.Text = "Msal Logs";
            // 
            // msalLogs
            // 
            this.msalLogs.BackColor = System.Drawing.Color.PowderBlue;
            this.msalLogs.Location = new System.Drawing.Point(18, 1017);
            this.msalLogs.Margin = new System.Windows.Forms.Padding(6);
            this.msalLogs.Multiline = true;
            this.msalLogs.Name = "msalLogs";
            this.msalLogs.Size = new System.Drawing.Size(1052, 319);
            this.msalLogs.TabIndex = 2;
            // 
            // Done
            // 
            this.Done.BackColor = System.Drawing.Color.LightSeaGreen;
            this.Done.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Done.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Done.Location = new System.Drawing.Point(362, 860);
            this.Done.Margin = new System.Windows.Forms.Padding(6);
            this.Done.Name = "Done";
            this.Done.Size = new System.Drawing.Size(358, 106);
            this.Done.TabIndex = 1;
            this.Done.Text = "Done";
            this.Done.UseVisualStyleBackColor = false;
            this.Done.Click += new System.EventHandler(this.Done_Click);
            // 
            // testResultBox
            // 
            this.testResultBox.AccessibleName = "testResult";
            this.testResultBox.Location = new System.Drawing.Point(13, 458);
            this.testResultBox.Name = "testResultBox";
            this.testResultBox.Size = new System.Drawing.Size(100, 31);
            this.testResultBox.TabIndex = 19;
            // 
            // AutomationUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1144, 973);
            this.Controls.Add(this.pageControl1);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "AutomationUI";
            this.Text = ".NET Automation App";
            this.Load += new System.EventHandler(this.AutomationUI_Load);
            this.pageControl1.ResumeLayout(false);
            this.mainPage.ResumeLayout(false);
            this.dataInputPage.ResumeLayout(false);
            this.dataInputPage.PerformLayout();
            this.resultPage.ResumeLayout(false);
            this.resultPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl pageControl1;
        private System.Windows.Forms.TabPage mainPage;
        private System.Windows.Forms.Button acquireToken;
        private System.Windows.Forms.TabPage dataInputPage;
        private System.Windows.Forms.TabPage resultPage;
        private System.Windows.Forms.Button clearCache;
        private System.Windows.Forms.Button readCache;
        private System.Windows.Forms.Button invalidateToken;
        private System.Windows.Forms.Button expireAccessToken;
        private System.Windows.Forms.Button acquireTokenSilent;
        private System.Windows.Forms.Button GoBtn;
        private System.Windows.Forms.TextBox dataInput;
        private System.Windows.Forms.Button Done;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox msalLogs;
        private System.Windows.Forms.Label accessToken;
        private System.Windows.Forms.Label expiresOnResult;
        private System.Windows.Forms.Label expiresOn;
        private System.Windows.Forms.Label accessTokenResult;
        private System.Windows.Forms.Label tenantIdResult;
        private System.Windows.Forms.Label tenantId;
        private System.Windows.Forms.Label userResult;
        private System.Windows.Forms.Label user;
        private System.Windows.Forms.Label idTokenResult;
        private System.Windows.Forms.Label idToken;
        private System.Windows.Forms.Label scope;
        private System.Windows.Forms.Label exceptionResult;
        private System.Windows.Forms.ListBox scopeResult;
        private System.Windows.Forms.Label messageResult;
        private System.Windows.Forms.TextBox testResultBox;
    }
}

