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
            this.pageControl1.Location = new System.Drawing.Point(9, 8);
            this.pageControl1.Name = "pageControl1";
            this.pageControl1.SelectedIndex = 0;
            this.pageControl1.Size = new System.Drawing.Size(551, 729);
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
            this.mainPage.Location = new System.Drawing.Point(8, 27);
            this.mainPage.Name = "mainPage";
            this.mainPage.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.mainPage.Size = new System.Drawing.Size(535, 694);
            this.mainPage.TabIndex = 0;
            this.mainPage.Text = "Main Page";
            this.mainPage.UseVisualStyleBackColor = true;
            // 
            // clearCache
            // 
            this.clearCache.Enabled = false;
            this.clearCache.Location = new System.Drawing.Point(162, 414);
            this.clearCache.Name = "clearCache";
            this.clearCache.Size = new System.Drawing.Size(235, 46);
            this.clearCache.TabIndex = 5;
            this.clearCache.Text = "Clear Cache";
            this.clearCache.UseVisualStyleBackColor = true;
            // 
            // readCache
            // 
            this.readCache.Enabled = false;
            this.readCache.Location = new System.Drawing.Point(161, 329);
            this.readCache.Name = "readCache";
            this.readCache.Size = new System.Drawing.Size(236, 45);
            this.readCache.TabIndex = 4;
            this.readCache.Text = "Read Cache";
            this.readCache.UseVisualStyleBackColor = true;
            // 
            // invalidateToken
            // 
            this.invalidateToken.Enabled = false;
            this.invalidateToken.Location = new System.Drawing.Point(159, 251);
            this.invalidateToken.Name = "invalidateToken";
            this.invalidateToken.Size = new System.Drawing.Size(238, 44);
            this.invalidateToken.TabIndex = 3;
            this.invalidateToken.Text = "Invalidate Token";
            this.invalidateToken.UseVisualStyleBackColor = true;
            // 
            // expireAccessToken
            // 
            this.expireAccessToken.Location = new System.Drawing.Point(159, 181);
            this.expireAccessToken.Name = "expireAccessToken";
            this.expireAccessToken.Size = new System.Drawing.Size(240, 44);
            this.expireAccessToken.TabIndex = 2;
            this.expireAccessToken.Text = "Expire Access Token";
            this.expireAccessToken.UseVisualStyleBackColor = true;
            this.expireAccessToken.Click += new System.EventHandler(this.expireAccessToken_Click);
            // 
            // acquireTokenSilent
            // 
            this.acquireTokenSilent.Location = new System.Drawing.Point(159, 110);
            this.acquireTokenSilent.Name = "acquireTokenSilent";
            this.acquireTokenSilent.Size = new System.Drawing.Size(242, 46);
            this.acquireTokenSilent.TabIndex = 1;
            this.acquireTokenSilent.Text = "Acquire Token Silent";
            this.acquireTokenSilent.UseVisualStyleBackColor = true;
            this.acquireTokenSilent.Click += new System.EventHandler(this.acquireTokenSilent_Click);
            // 
            // acquireToken
            // 
            this.acquireToken.Location = new System.Drawing.Point(157, 35);
            this.acquireToken.Name = "acquireToken";
            this.acquireToken.Size = new System.Drawing.Size(244, 48);
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
            this.dataInputPage.Location = new System.Drawing.Point(8, 27);
            this.dataInputPage.Name = "dataInputPage";
            this.dataInputPage.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.dataInputPage.Size = new System.Drawing.Size(535, 694);
            this.dataInputPage.TabIndex = 1;
            this.dataInputPage.Text = "Data Input Page";
            this.dataInputPage.UseVisualStyleBackColor = true;
            // 
            // GoBtn
            // 
            this.GoBtn.BackColor = System.Drawing.Color.DarkOrange;
            this.GoBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GoBtn.Location = new System.Drawing.Point(191, 409);
            this.GoBtn.Name = "GoBtn";
            this.GoBtn.Size = new System.Drawing.Size(135, 59);
            this.GoBtn.TabIndex = 1;
            this.GoBtn.Text = "Go";
            this.GoBtn.UseVisualStyleBackColor = false;
            this.GoBtn.Click += new System.EventHandler(this.GoBtn_Click);
            // 
            // dataInput
            // 
            this.dataInput.BackColor = System.Drawing.Color.Bisque;
            this.dataInput.Location = new System.Drawing.Point(103, 6);
            this.dataInput.Multiline = true;
            this.dataInput.Name = "dataInput";
            this.dataInput.Size = new System.Drawing.Size(329, 397);
            this.dataInput.TabIndex = 0;
            // 
            // resultPage
            // 
            this.resultPage.AutoScroll = true;
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
            this.resultPage.Location = new System.Drawing.Point(8, 27);
            this.resultPage.Name = "resultPage";
            this.resultPage.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.resultPage.Size = new System.Drawing.Size(535, 694);
            this.resultPage.TabIndex = 2;
            this.resultPage.Text = "Result Page";
            this.resultPage.UseVisualStyleBackColor = true;
            // 
            // messageResult
            // 
            this.messageResult.AutoSize = true;
            this.messageResult.Location = new System.Drawing.Point(6, 249);
            this.messageResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.messageResult.Name = "messageResult";
            this.messageResult.Size = new System.Drawing.Size(133, 13);
            this.messageResult.TabIndex = 18;
            this.messageResult.Text = "Message Placeholder Text";
            // 
            // scopeResult
            // 
            this.scopeResult.FormattingEnabled = true;
            this.scopeResult.Location = new System.Drawing.Point(84, 138);
            this.scopeResult.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.scopeResult.Name = "scopeResult";
            this.scopeResult.Size = new System.Drawing.Size(170, 95);
            this.scopeResult.TabIndex = 17;
            // 
            // exceptionResult
            // 
            this.exceptionResult.AutoSize = true;
            this.exceptionResult.Location = new System.Drawing.Point(230, 476);
            this.exceptionResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.exceptionResult.Name = "exceptionResult";
            this.exceptionResult.Size = new System.Drawing.Size(0, 13);
            this.exceptionResult.TabIndex = 16;
            // 
            // scope
            // 
            this.scope.AutoSize = true;
            this.scope.Location = new System.Drawing.Point(4, 138);
            this.scope.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.scope.Name = "scope";
            this.scope.Size = new System.Drawing.Size(38, 13);
            this.scope.TabIndex = 14;
            this.scope.Text = "Scope";
            // 
            // idTokenResult
            // 
            this.idTokenResult.AutoSize = true;
            this.idTokenResult.Location = new System.Drawing.Point(82, 112);
            this.idTokenResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.idTokenResult.Name = "idTokenResult";
            this.idTokenResult.Size = new System.Drawing.Size(87, 13);
            this.idTokenResult.TabIndex = 13;
            this.idTokenResult.Text = "Placeholder Text";
            // 
            // idToken
            // 
            this.idToken.AutoSize = true;
            this.idToken.Location = new System.Drawing.Point(4, 112);
            this.idToken.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.idToken.Name = "idToken";
            this.idToken.Size = new System.Drawing.Size(50, 13);
            this.idToken.TabIndex = 12;
            this.idToken.Text = "Id Token";
            // 
            // userResult
            // 
            this.userResult.AutoSize = true;
            this.userResult.Location = new System.Drawing.Point(82, 86);
            this.userResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.userResult.Name = "userResult";
            this.userResult.Size = new System.Drawing.Size(87, 13);
            this.userResult.TabIndex = 11;
            this.userResult.Text = "Placeholder Text";
            // 
            // user
            // 
            this.user.AutoSize = true;
            this.user.Location = new System.Drawing.Point(4, 86);
            this.user.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.user.Name = "user";
            this.user.Size = new System.Drawing.Size(29, 13);
            this.user.TabIndex = 10;
            this.user.Text = "User";
            // 
            // tenantIdResult
            // 
            this.tenantIdResult.AutoSize = true;
            this.tenantIdResult.Location = new System.Drawing.Point(82, 60);
            this.tenantIdResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.tenantIdResult.Name = "tenantIdResult";
            this.tenantIdResult.Size = new System.Drawing.Size(87, 13);
            this.tenantIdResult.TabIndex = 9;
            this.tenantIdResult.Text = "Placeholder Text";
            // 
            // tenantId
            // 
            this.tenantId.AutoSize = true;
            this.tenantId.Location = new System.Drawing.Point(4, 60);
            this.tenantId.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.tenantId.Name = "tenantId";
            this.tenantId.Size = new System.Drawing.Size(53, 13);
            this.tenantId.TabIndex = 8;
            this.tenantId.Text = "Tenant Id";
            // 
            // expiresOnResult
            // 
            this.expiresOnResult.AutoSize = true;
            this.expiresOnResult.Location = new System.Drawing.Point(82, 34);
            this.expiresOnResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.expiresOnResult.Name = "expiresOnResult";
            this.expiresOnResult.Size = new System.Drawing.Size(87, 13);
            this.expiresOnResult.TabIndex = 7;
            this.expiresOnResult.Text = "Placeholder Text";
            // 
            // expiresOn
            // 
            this.expiresOn.AutoSize = true;
            this.expiresOn.Location = new System.Drawing.Point(4, 34);
            this.expiresOn.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.expiresOn.Name = "expiresOn";
            this.expiresOn.Size = new System.Drawing.Size(58, 13);
            this.expiresOn.TabIndex = 6;
            this.expiresOn.Text = "Expires On";
            // 
            // accessTokenResult
            // 
            this.accessTokenResult.AutoSize = true;
            this.accessTokenResult.BackColor = System.Drawing.Color.Transparent;
            this.accessTokenResult.Location = new System.Drawing.Point(82, 8);
            this.accessTokenResult.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.accessTokenResult.Name = "accessTokenResult";
            this.accessTokenResult.Size = new System.Drawing.Size(87, 13);
            this.accessTokenResult.TabIndex = 5;
            this.accessTokenResult.Text = "Placeholder Text";
            // 
            // accessToken
            // 
            this.accessToken.AutoSize = true;
            this.accessToken.Location = new System.Drawing.Point(4, 8);
            this.accessToken.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.accessToken.Name = "accessToken";
            this.accessToken.Size = new System.Drawing.Size(76, 13);
            this.accessToken.TabIndex = 4;
            this.accessToken.Text = "Access Token";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 513);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Msal Logs";
            // 
            // msalLogs
            // 
            this.msalLogs.BackColor = System.Drawing.Color.PowderBlue;
            this.msalLogs.Location = new System.Drawing.Point(9, 529);
            this.msalLogs.Multiline = true;
            this.msalLogs.Name = "msalLogs";
            this.msalLogs.Size = new System.Drawing.Size(528, 168);
            this.msalLogs.TabIndex = 2;
            // 
            // Done
            // 
            this.Done.BackColor = System.Drawing.Color.LightSeaGreen;
            this.Done.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Done.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Done.Location = new System.Drawing.Point(181, 447);
            this.Done.Name = "Done";
            this.Done.Size = new System.Drawing.Size(179, 55);
            this.Done.TabIndex = 1;
            this.Done.Text = "Done";
            this.Done.UseVisualStyleBackColor = false;
            this.Done.Click += new System.EventHandler(this.Done_Click);
            // 
            // AutomationUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(572, 506);
            this.Controls.Add(this.pageControl1);
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
    }
}

