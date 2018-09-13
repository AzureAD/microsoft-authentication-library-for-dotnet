namespace SampleApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.signInPage = new System.Windows.Forms.TabPage();
            this.acquireTokenUsernamePasswordButton = new System.Windows.Forms.Button();
            this.signOutButton1 = new System.Windows.Forms.Button();
            this.tokenResultBox = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.calendarPage = new System.Windows.Forms.TabPage();
            this.calendarTextBox = new System.Windows.Forms.TextBox();
            this.acquireTokenWIAButton = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.signInPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.signInPage);
            this.tabControl1.Controls.Add(this.calendarPage);
            this.tabControl1.Location = new System.Drawing.Point(-1, -1);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(542, 383);
            this.tabControl1.TabIndex = 0;
            // 
            // signInPage
            // 
            this.signInPage.Controls.Add(this.acquireTokenWIAButton);
            this.signInPage.Controls.Add(this.acquireTokenUsernamePasswordButton);
            this.signInPage.Controls.Add(this.signOutButton1);
            this.signInPage.Controls.Add(this.tokenResultBox);
            this.signInPage.Controls.Add(this.pictureBox1);
            this.signInPage.Location = new System.Drawing.Point(4, 22);
            this.signInPage.Margin = new System.Windows.Forms.Padding(2);
            this.signInPage.Name = "signInPage";
            this.signInPage.Padding = new System.Windows.Forms.Padding(2);
            this.signInPage.Size = new System.Drawing.Size(534, 357);
            this.signInPage.TabIndex = 0;
            this.signInPage.Text = "signInPage";
            this.signInPage.UseVisualStyleBackColor = true;
            // 
            // acquireTokenUsernamePasswordButton
            // 
            this.acquireTokenUsernamePasswordButton.Location = new System.Drawing.Point(21, 83);
            this.acquireTokenUsernamePasswordButton.Name = "acquireTokenUsernamePasswordButton";
            this.acquireTokenUsernamePasswordButton.Size = new System.Drawing.Size(227, 40);
            this.acquireTokenUsernamePasswordButton.TabIndex = 3;
            this.acquireTokenUsernamePasswordButton.Text = "Sign in with username/password";
            this.acquireTokenUsernamePasswordButton.UseVisualStyleBackColor = true;
            this.acquireTokenUsernamePasswordButton.Click += new System.EventHandler(this.acquireTokenUsernamePasswordButton_Click);
            // 
            // signOutButton1
            // 
            this.signOutButton1.Location = new System.Drawing.Point(426, 319);
            this.signOutButton1.Name = "signOutButton1";
            this.signOutButton1.Size = new System.Drawing.Size(79, 25);
            this.signOutButton1.TabIndex = 2;
            this.signOutButton1.Text = "Sign Out";
            this.signOutButton1.UseVisualStyleBackColor = true;
            this.signOutButton1.Click += new System.EventHandler(this.signOutButton1_Click);
            // 
            // tokenResultBox
            // 
            this.tokenResultBox.Location = new System.Drawing.Point(19, 213);
            this.tokenResultBox.Multiline = true;
            this.tokenResultBox.Name = "tokenResultBox";
            this.tokenResultBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tokenResultBox.Size = new System.Drawing.Size(366, 96);
            this.tokenResultBox.TabIndex = 1;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(19, 27);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(230, 40);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // calendarPage
            // 
            this.calendarPage.Location = new System.Drawing.Point(4, 22);
            this.calendarPage.Name = "calendarPage";
            this.calendarPage.Size = new System.Drawing.Size(534, 357);
            this.calendarPage.TabIndex = 1;
            // 
            // calendarTextBox
            // 
            this.calendarTextBox.Location = new System.Drawing.Point(0, 0);
            this.calendarTextBox.Name = "calendarTextBox";
            this.calendarTextBox.Size = new System.Drawing.Size(100, 20);
            this.calendarTextBox.TabIndex = 0;
            // 
            // acquireTokenWIAButton
            // 
            this.acquireTokenWIAButton.Location = new System.Drawing.Point(21, 139);
            this.acquireTokenWIAButton.Name = "acquireTokenWIAButton";
            this.acquireTokenWIAButton.Size = new System.Drawing.Size(227, 41);
            this.acquireTokenWIAButton.TabIndex = 4;
            this.acquireTokenWIAButton.Text = "Acquire token with Integrated Windows Auth";
            this.acquireTokenWIAButton.UseVisualStyleBackColor = true;
            this.acquireTokenWIAButton.Click += new System.EventHandler(this.acquireTokenWIAButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 378);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.tabControl1.ResumeLayout(false);
            this.signInPage.ResumeLayout(false);
            this.signInPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage signInPage;
        private System.Windows.Forms.TabPage calendarPage;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox tokenResultBox;
        private System.Windows.Forms.TextBox calendarTextBox;
        private System.Windows.Forms.Button signOutButton;
        private System.Windows.Forms.Button signOutButton1;
        private System.Windows.Forms.Button acquireTokenUsernamePasswordButton;
        private System.Windows.Forms.Button acquireTokenWIAButton;
    }
}

