namespace DesktopTestApp
{
    partial class MsalUserAccessTokenControl
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
            this.deleteAccessTokenButton = new System.Windows.Forms.Button();
            this.expireAccessTokenButton = new System.Windows.Forms.Button();
            this.scopesLabel = new System.Windows.Forms.Label();
            this.expiresOnLabel = new System.Windows.Forms.Label();
            this.expiresOnAT1Label = new System.Windows.Forms.Label();
            this.accessTokenOneLabel = new System.Windows.Forms.Label();
            this.accessTokenScopesLabel = new System.Windows.Forms.Label();
            this.accessTokenAuthorityLabel = new System.Windows.Forms.Label();
            this.authorityLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // deleteAccessTokenButton
            // 
            this.deleteAccessTokenButton.BackColor = System.Drawing.Color.PaleTurquoise;
            this.deleteAccessTokenButton.Location = new System.Drawing.Point(512, 4);
            this.deleteAccessTokenButton.Name = "deleteAccessTokenButton";
            this.deleteAccessTokenButton.Size = new System.Drawing.Size(57, 26);
            this.deleteAccessTokenButton.TabIndex = 20;
            this.deleteAccessTokenButton.Text = "Delete";
            this.deleteAccessTokenButton.UseVisualStyleBackColor = false;
            this.deleteAccessTokenButton.Click += new System.EventHandler(this.deleteAccessTokenButton_Click);
            // 
            // expireAccessTokenButton
            // 
            this.expireAccessTokenButton.BackColor = System.Drawing.Color.LightSteelBlue;
            this.expireAccessTokenButton.Location = new System.Drawing.Point(427, 5);
            this.expireAccessTokenButton.Name = "expireAccessTokenButton";
            this.expireAccessTokenButton.Size = new System.Drawing.Size(64, 25);
            this.expireAccessTokenButton.TabIndex = 19;
            this.expireAccessTokenButton.Text = "Expire";
            this.expireAccessTokenButton.UseVisualStyleBackColor = false;
            this.expireAccessTokenButton.Click += new System.EventHandler(this.expireAccessTokenButton_Click);
            // 
            // scopesLabel
            // 
            this.scopesLabel.AutoSize = true;
            this.scopesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scopesLabel.Location = new System.Drawing.Point(3, 61);
            this.scopesLabel.Name = "scopesLabel";
            this.scopesLabel.Size = new System.Drawing.Size(49, 13);
            this.scopesLabel.TabIndex = 18;
            this.scopesLabel.Text = "Scopes";
            // 
            // expiresOnLabel
            // 
            this.expiresOnLabel.AutoSize = true;
            this.expiresOnLabel.Location = new System.Drawing.Point(173, 11);
            this.expiresOnLabel.Name = "expiresOnLabel";
            this.expiresOnLabel.Size = new System.Drawing.Size(117, 13);
            this.expiresOnLabel.TabIndex = 17;
            this.expiresOnLabel.Text = "Expires On Placeholder";
            // 
            // expiresOnAT1Label
            // 
            this.expiresOnAT1Label.AutoSize = true;
            this.expiresOnAT1Label.Location = new System.Drawing.Point(97, 11);
            this.expiresOnAT1Label.Name = "expiresOnAT1Label";
            this.expiresOnAT1Label.Size = new System.Drawing.Size(58, 13);
            this.expiresOnAT1Label.TabIndex = 16;
            this.expiresOnAT1Label.Text = "Expires On";
            // 
            // accessTokenOneLabel
            // 
            this.accessTokenOneLabel.AutoSize = true;
            this.accessTokenOneLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.accessTokenOneLabel.Location = new System.Drawing.Point(3, 11);
            this.accessTokenOneLabel.Name = "accessTokenOneLabel";
            this.accessTokenOneLabel.Size = new System.Drawing.Size(88, 13);
            this.accessTokenOneLabel.TabIndex = 15;
            this.accessTokenOneLabel.Text = "Access Token";
            // 
            // accessTokenScopesLabel
            // 
            this.accessTokenScopesLabel.AutoSize = true;
            this.accessTokenScopesLabel.Location = new System.Drawing.Point(81, 61);
            this.accessTokenScopesLabel.Name = "accessTokenScopesLabel";
            this.accessTokenScopesLabel.Size = new System.Drawing.Size(43, 13);
            this.accessTokenScopesLabel.TabIndex = 21;
            this.accessTokenScopesLabel.Text = "Scopes";
            // 
            // accessTokenAuthorityLabel
            // 
            this.accessTokenAuthorityLabel.AutoSize = true;
            this.accessTokenAuthorityLabel.Location = new System.Drawing.Point(81, 36);
            this.accessTokenAuthorityLabel.Name = "accessTokenAuthorityLabel";
            this.accessTokenAuthorityLabel.Size = new System.Drawing.Size(48, 13);
            this.accessTokenAuthorityLabel.TabIndex = 23;
            this.accessTokenAuthorityLabel.Text = "Authority";
            // 
            // authorityLabel
            // 
            this.authorityLabel.AutoSize = true;
            this.authorityLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.authorityLabel.Location = new System.Drawing.Point(3, 36);
            this.authorityLabel.Name = "authorityLabel";
            this.authorityLabel.Size = new System.Drawing.Size(57, 13);
            this.authorityLabel.TabIndex = 22;
            this.authorityLabel.Text = "Authority";
            // 
            // MsalUserAccessTokenControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.accessTokenAuthorityLabel);
            this.Controls.Add(this.authorityLabel);
            this.Controls.Add(this.accessTokenScopesLabel);
            this.Controls.Add(this.deleteAccessTokenButton);
            this.Controls.Add(this.expireAccessTokenButton);
            this.Controls.Add(this.scopesLabel);
            this.Controls.Add(this.expiresOnLabel);
            this.Controls.Add(this.expiresOnAT1Label);
            this.Controls.Add(this.accessTokenOneLabel);
            this.Name = "MsalUserAccessTokenControl";
            this.Size = new System.Drawing.Size(582, 88);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button deleteAccessTokenButton;
        private System.Windows.Forms.Button expireAccessTokenButton;
        private System.Windows.Forms.Label scopesLabel;
        private System.Windows.Forms.Label expiresOnLabel;
        private System.Windows.Forms.Label expiresOnAT1Label;
        private System.Windows.Forms.Label accessTokenOneLabel;
        private System.Windows.Forms.Label accessTokenScopesLabel;
        private System.Windows.Forms.Label accessTokenAuthorityLabel;
        private System.Windows.Forms.Label authorityLabel;
    }
}
