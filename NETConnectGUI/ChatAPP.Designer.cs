namespace NETConnectGUI
{
    partial class ChatAPP
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
            lblPeerId = new Label();
            listPeers = new ListBox();
            lblAddress = new Label();
            panel1 = new Panel();
            btnSendMessage = new Button();
            txtMessage = new RichTextBox();
            rtbMessages = new RichTextBox();
            label1 = new Label();
            txtUsername = new TextBox();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lblPeerId
            // 
            lblPeerId.AutoSize = true;
            lblPeerId.Location = new Point(12, 9);
            lblPeerId.Name = "lblPeerId";
            lblPeerId.Size = new Size(46, 15);
            lblPeerId.TabIndex = 0;
            lblPeerId.Text = "PeerId: ";
            // 
            // listPeers
            // 
            listPeers.BackColor = SystemColors.ScrollBar;
            listPeers.Dock = DockStyle.Left;
            listPeers.FormattingEnabled = true;
            listPeers.Location = new Point(0, 0);
            listPeers.Name = "listPeers";
            listPeers.Size = new Size(387, 421);
            listPeers.TabIndex = 1;
            // 
            // lblAddress
            // 
            lblAddress.AutoSize = true;
            lblAddress.Location = new Point(12, 26);
            lblAddress.Name = "lblAddress";
            lblAddress.Size = new Size(55, 15);
            lblAddress.TabIndex = 7;
            lblAddress.Text = "Address: ";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnSendMessage);
            panel1.Controls.Add(txtMessage);
            panel1.Controls.Add(listPeers);
            panel1.Controls.Add(rtbMessages);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 51);
            panel1.Name = "panel1";
            panel1.Size = new Size(1285, 421);
            panel1.TabIndex = 8;
            // 
            // btnSendMessage
            // 
            btnSendMessage.Location = new Point(1187, 334);
            btnSendMessage.Name = "btnSendMessage";
            btnSendMessage.Size = new Size(97, 87);
            btnSendMessage.TabIndex = 9;
            btnSendMessage.Text = "Send";
            btnSendMessage.UseVisualStyleBackColor = true;
            btnSendMessage.Click += btnSendMessage_Click;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(386, 334);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(802, 87);
            txtMessage.TabIndex = 4;
            txtMessage.Text = "";
            // 
            // rtbMessages
            // 
            rtbMessages.BackColor = SystemColors.GradientInactiveCaption;
            rtbMessages.Location = new Point(386, -1);
            rtbMessages.Name = "rtbMessages";
            rtbMessages.Size = new Size(898, 336);
            rtbMessages.TabIndex = 3;
            rtbMessages.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(653, 3);
            label1.Name = "label1";
            label1.Size = new Size(60, 15);
            label1.TabIndex = 9;
            label1.Text = "Username";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(612, 20);
            txtUsername.MaxLength = 16;
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(151, 23);
            txtUsername.TabIndex = 10;
            // 
            // ChatAPP
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.RosyBrown;
            ClientSize = new Size(1285, 472);
            Controls.Add(txtUsername);
            Controls.Add(label1);
            Controls.Add(panel1);
            Controls.Add(lblAddress);
            Controls.Add(lblPeerId);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Name = "ChatAPP";
            Text = "P2P NETConnect GUI testing";
            Load += ChatAPP_Load;
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPeerId;
        private ListBox listPeers;
        private Label lblAddress;
        private Panel panel1;
        private RichTextBox rtbMessages;
        private Button btnSendMessage;
        private RichTextBox txtMessage;
        private Label label1;
        private TextBox txtUsername;
    }
}