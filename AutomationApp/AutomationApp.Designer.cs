namespace AutomationApp
{
    partial class AutomationApp
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
            this.button1 = new System.Windows.Forms.Button();
            this.acquireToken = new System.Windows.Forms.Button();
            this.dataInputPage = new System.Windows.Forms.TabPage();
            this.go = new System.Windows.Forms.Button();
            this.dataInput = new System.Windows.Forms.TextBox();
            this.resultPage = new System.Windows.Forms.TabPage();
            this.Done = new System.Windows.Forms.Button();
            this.resultInfo = new System.Windows.Forms.TextBox();
            this.msalLogs = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.pageControl1.Size = new System.Drawing.Size(358, 515);
            this.pageControl1.TabIndex = 0;
            // 
            // mainPage
            // 
            this.mainPage.Controls.Add(this.clearCache);
            this.mainPage.Controls.Add(this.readCache);
            this.mainPage.Controls.Add(this.invalidateToken);
            this.mainPage.Controls.Add(this.expireAccessToken);
            this.mainPage.Controls.Add(this.button1);
            this.mainPage.Controls.Add(this.acquireToken);
            this.mainPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainPage.Location = new System.Drawing.Point(4, 22);
            this.mainPage.Name = "mainPage";
            this.mainPage.Padding = new System.Windows.Forms.Padding(3);
            this.mainPage.Size = new System.Drawing.Size(350, 489);
            this.mainPage.TabIndex = 0;
            this.mainPage.Text = "Main Page";
            this.mainPage.UseVisualStyleBackColor = true;
            // 
            // clearCache
            // 
            this.clearCache.Location = new System.Drawing.Point(52, 389);
            this.clearCache.Name = "clearCache";
            this.clearCache.Size = new System.Drawing.Size(235, 46);
            this.clearCache.TabIndex = 5;
            this.clearCache.Text = "Clear Cache";
            this.clearCache.UseVisualStyleBackColor = true;
            // 
            // readCache
            // 
            this.readCache.Location = new System.Drawing.Point(52, 315);
            this.readCache.Name = "readCache";
            this.readCache.Size = new System.Drawing.Size(236, 45);
            this.readCache.TabIndex = 4;
            this.readCache.Text = "Read Cache";
            this.readCache.UseVisualStyleBackColor = true;
            // 
            // invalidateToken
            // 
            this.invalidateToken.Location = new System.Drawing.Point(51, 243);
            this.invalidateToken.Name = "invalidateToken";
            this.invalidateToken.Size = new System.Drawing.Size(238, 44);
            this.invalidateToken.TabIndex = 3;
            this.invalidateToken.Text = "Invalidate Token";
            this.invalidateToken.UseVisualStyleBackColor = true;
            // 
            // expireAccessToken
            // 
            this.expireAccessToken.Location = new System.Drawing.Point(50, 175);
            this.expireAccessToken.Name = "expireAccessToken";
            this.expireAccessToken.Size = new System.Drawing.Size(240, 44);
            this.expireAccessToken.TabIndex = 2;
            this.expireAccessToken.Text = "Expire Access Token";
            this.expireAccessToken.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(49, 108);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(242, 46);
            this.button1.TabIndex = 1;
            this.button1.Text = "Acquire Token Silent";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // acquireToken
            // 
            this.acquireToken.Location = new System.Drawing.Point(48, 36);
            this.acquireToken.Name = "acquireToken";
            this.acquireToken.Size = new System.Drawing.Size(244, 48);
            this.acquireToken.TabIndex = 0;
            this.acquireToken.Text = "Acquire Token";
            this.acquireToken.UseVisualStyleBackColor = true;
            this.acquireToken.Click += new System.EventHandler(this.acquireToken_Click);
            // 
            // dataInputPage
            // 
            this.dataInputPage.Controls.Add(this.go);
            this.dataInputPage.Controls.Add(this.dataInput);
            this.dataInputPage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dataInputPage.Location = new System.Drawing.Point(4, 22);
            this.dataInputPage.Name = "dataInputPage";
            this.dataInputPage.Padding = new System.Windows.Forms.Padding(3);
            this.dataInputPage.Size = new System.Drawing.Size(350, 489);
            this.dataInputPage.TabIndex = 1;
            this.dataInputPage.Text = "Data Input Page";
            this.dataInputPage.UseVisualStyleBackColor = true;
            // 
            // go
            // 
            this.go.BackColor = System.Drawing.Color.DarkOrange;
            this.go.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.go.Location = new System.Drawing.Point(100, 414);
            this.go.Name = "go";
            this.go.Size = new System.Drawing.Size(135, 59);
            this.go.TabIndex = 1;
            this.go.Text = "Go";
            this.go.UseVisualStyleBackColor = false;
            this.go.Click += new System.EventHandler(this.go_Click);
            // 
            // dataInput
            // 
            this.dataInput.BackColor = System.Drawing.Color.Bisque;
            this.dataInput.Location = new System.Drawing.Point(10, 16);
            this.dataInput.Multiline = true;
            this.dataInput.Name = "dataInput";
            this.dataInput.Size = new System.Drawing.Size(329, 387);
            this.dataInput.TabIndex = 0;
            this.dataInput.Text = "Please enter the sign-in info here...";
            // 
            // resultPage
            // 
            this.resultPage.Controls.Add(this.label1);
            this.resultPage.Controls.Add(this.msalLogs);
            this.resultPage.Controls.Add(this.Done);
            this.resultPage.Controls.Add(this.resultInfo);
            this.resultPage.Location = new System.Drawing.Point(4, 22);
            this.resultPage.Name = "resultPage";
            this.resultPage.Padding = new System.Windows.Forms.Padding(3);
            this.resultPage.Size = new System.Drawing.Size(350, 489);
            this.resultPage.TabIndex = 2;
            this.resultPage.Text = "Result Page";
            this.resultPage.UseVisualStyleBackColor = true;
            // 
            // Done
            // 
            this.Done.BackColor = System.Drawing.Color.LightSeaGreen;
            this.Done.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Done.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Done.Location = new System.Drawing.Point(88, 232);
            this.Done.Name = "Done";
            this.Done.Size = new System.Drawing.Size(179, 55);
            this.Done.TabIndex = 1;
            this.Done.Text = "Done";
            this.Done.UseVisualStyleBackColor = false;
            this.Done.Click += new System.EventHandler(this.Done_Click);
            // 
            // resultInfo
            // 
            this.resultInfo.BackColor = System.Drawing.Color.PaleTurquoise;
            this.resultInfo.Location = new System.Drawing.Point(9, 7);
            this.resultInfo.Multiline = true;
            this.resultInfo.Name = "resultInfo";
            this.resultInfo.Size = new System.Drawing.Size(330, 219);
            this.resultInfo.TabIndex = 0;
            // 
            // msalLogs
            // 
            this.msalLogs.BackColor = System.Drawing.Color.PowderBlue;
            this.msalLogs.Location = new System.Drawing.Point(8, 311);
            this.msalLogs.Multiline = true;
            this.msalLogs.Name = "msalLogs";
            this.msalLogs.Size = new System.Drawing.Size(330, 168);
            this.msalLogs.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 295);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Msal Logs";
            // 
            // AutomationApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 535);
            this.Controls.Add(this.pageControl1);
            this.Name = "AutomationApp";
            this.Text = ".NET Automation App";
            this.Load += new System.EventHandler(this.AutomationApp_Load);
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button go;
        private System.Windows.Forms.TextBox dataInput;
        private System.Windows.Forms.Button Done;
        private System.Windows.Forms.TextBox resultInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox msalLogs;
    }
}

