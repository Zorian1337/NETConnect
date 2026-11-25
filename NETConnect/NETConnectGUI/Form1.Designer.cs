namespace NETConnectGUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtConnection = new TextBox();
            btnConnect = new Button();
            gbServer = new GroupBox();
            lblPeerId = new Label();
            gbServer.SuspendLayout();
            SuspendLayout();
            // 
            // txtConnection
            // 
            txtConnection.Location = new Point(23, 29);
            txtConnection.Name = "txtConnection";
            txtConnection.Size = new Size(239, 23);
            txtConnection.TabIndex = 0;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(28, 64);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(82, 23);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Connect";
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // gbServer
            // 
            gbServer.BackColor = SystemColors.WindowFrame;
            gbServer.Controls.Add(lblPeerId);
            gbServer.Location = new Point(23, 141);
            gbServer.Name = "gbServer";
            gbServer.Size = new Size(593, 443);
            gbServer.TabIndex = 2;
            gbServer.TabStop = false;
            gbServer.Text = "Server";
            // 
            // lblPeerId
            // 
            lblPeerId.AutoSize = true;
            lblPeerId.Location = new Point(26, 43);
            lblPeerId.Name = "lblPeerId";
            lblPeerId.Size = new Size(43, 15);
            lblPeerId.TabIndex = 0;
            lblPeerId.Text = "PeerId:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDarkDark;
            ClientSize = new Size(1434, 619);
            Controls.Add(gbServer);
            Controls.Add(btnConnect);
            Controls.Add(txtConnection);
            Name = "Form1";
            Text = "Form1";
            gbServer.ResumeLayout(false);
            gbServer.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtConnection;
        private Button btnConnect;
        private GroupBox gbServer;
        private Label lblPeerId;
    }
}
