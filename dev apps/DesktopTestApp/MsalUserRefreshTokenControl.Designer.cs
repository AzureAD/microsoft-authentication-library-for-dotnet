namespace DesktopTestApp
{
    partial class MsalUserRefreshTokenControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.signOutBtn = new System.Windows.Forms.Button();
            this.invalidateRefreshTokenBtn = new System.Windows.Forms.Button();
            this.upnLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // signOutBtn
            // 
            this.signOutBtn.BackColor = System.Drawing.Color.CadetBlue;
            this.signOutBtn.Location = new System.Drawing.Point(470, 7);
            this.signOutBtn.Name = "signOutBtn";
            this.signOutBtn.Size = new System.Drawing.Size(112, 31);
            this.signOutBtn.TabIndex = 11;
            this.signOutBtn.Text = "Remove";
            this.signOutBtn.UseVisualStyleBackColor = false;
            this.signOutBtn.Click += new System.EventHandler(this.signOutUserOneBtn_Click);
            // 
            // invalidateRefreshTokenBtn
            // 
            this.invalidateRefreshTokenBtn.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.invalidateRefreshTokenBtn.Location = new System.Drawing.Point(313, 7);
            this.invalidateRefreshTokenBtn.Name = "invalidateRefreshTokenBtn";
            this.invalidateRefreshTokenBtn.Size = new System.Drawing.Size(135, 32);
            this.invalidateRefreshTokenBtn.TabIndex = 10;
            this.invalidateRefreshTokenBtn.Text = "Invalidate Refresh Token";
            this.invalidateRefreshTokenBtn.UseVisualStyleBackColor = false;
            this.invalidateRefreshTokenBtn.Click += new System.EventHandler(this.InvalidateRefreshTokenBtn_Click);
            // 
            // upnLabel
            // 
            this.upnLabel.AutoSize = true;
            this.upnLabel.Location = new System.Drawing.Point(3, 16);
            this.upnLabel.Name = "upnLabel";
            this.upnLabel.Size = new System.Drawing.Size(89, 13);
            this.upnLabel.TabIndex = 9;
            this.upnLabel.Text = "UPN Placeholder";
            // 
            // MsalUserRefreshTokenControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.signOutBtn);
            this.Controls.Add(this.invalidateRefreshTokenBtn);
            this.Controls.Add(this.upnLabel);
            this.Name = "MsalUserRefreshTokenControl";
            this.Size = new System.Drawing.Size(588, 47);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button signOutBtn;
        private System.Windows.Forms.Button invalidateRefreshTokenBtn;
        private System.Windows.Forms.Label upnLabel;
    }
}
