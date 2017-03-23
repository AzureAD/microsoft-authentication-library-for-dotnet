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
            this.label1 = new System.Windows.Forms.Label();
            this.msalLogs = new System.Windows.Forms.TextBox();
            this.Done = new System.Windows.Forms.Button();
            this.resultInfo = new System.Windows.Forms.TextBox();
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
            this.mainPage.Location = new System.Drawing.Point(4, 34);
            this.mainPage.Margin = new System.Windows.Forms.Padding(6);
            this.mainPage.Name = "mainPage";
            this.mainPage.Padding = new System.Windows.Forms.Padding(6);
            this.mainPage.Size = new System.Drawing.Size(1094, 1364);
            this.mainPage.TabIndex = 0;
            this.mainPage.Text = "Main Page";
            this.mainPage.UseVisualStyleBackColor = true;
            // 
            // clearCache
            // 
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
            this.dataInputPage.Location = new System.Drawing.Point(4, 34);
            this.dataInputPage.Margin = new System.Windows.Forms.Padding(6);
            this.dataInputPage.Name = "dataInputPage";
            this.dataInputPage.Padding = new System.Windows.Forms.Padding(6);
            this.dataInputPage.Size = new System.Drawing.Size(1094, 1364);
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
            this.resultPage.Controls.Add(this.label1);
            this.resultPage.Controls.Add(this.msalLogs);
            this.resultPage.Controls.Add(this.Done);
            this.resultPage.Controls.Add(this.resultInfo);
            this.resultPage.Location = new System.Drawing.Point(4, 34);
            this.resultPage.Margin = new System.Windows.Forms.Padding(6);
            this.resultPage.Name = "resultPage";
            this.resultPage.Padding = new System.Windows.Forms.Padding(6);
            this.resultPage.Size = new System.Drawing.Size(1094, 1364);
            this.resultPage.TabIndex = 2;
            this.resultPage.Text = "Result Page";
            this.resultPage.UseVisualStyleBackColor = true;
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
            // resultInfo
            // 
            this.resultInfo.BackColor = System.Drawing.Color.PaleTurquoise;
            this.resultInfo.Location = new System.Drawing.Point(18, 13);
            this.resultInfo.Margin = new System.Windows.Forms.Padding(6);
            this.resultInfo.Multiline = true;
            this.resultInfo.Name = "resultInfo";
            this.resultInfo.Size = new System.Drawing.Size(1052, 831);
            this.resultInfo.TabIndex = 0;
            // 
            // AutomationUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1144, 1180);
            this.Controls.Add(this.pageControl1);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "AutomationUI";
            this.Text = ".NET Automation App";
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
        private System.Windows.Forms.TextBox resultInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox msalLogs;
    }
}

