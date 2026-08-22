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
            lblPeerId = new Label();
            lblServerIP = new Label();
            rtbConsole = new RichTextBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            statusStrip1 = new StatusStrip();
            StripServerStatus = new ToolStripStatusLabel();
            StripPeerCount = new ToolStripStatusLabel();
            listPeerView = new ListBox();
            listDiscoveredPeers = new ListBox();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lblPeerId
            // 
            lblPeerId.AutoSize = true;
            lblPeerId.Location = new Point(341, 149);
            lblPeerId.Name = "lblPeerId";
            lblPeerId.Size = new Size(46, 15);
            lblPeerId.TabIndex = 1;
            lblPeerId.Text = "PeerId: ";
            // 
            // lblServerIP
            // 
            lblServerIP.AutoSize = true;
            lblServerIP.Location = new Point(341, 125);
            lblServerIP.Name = "lblServerIP";
            lblServerIP.Size = new Size(35, 15);
            lblServerIP.TabIndex = 2;
            lblServerIP.Text = "IPv4: ";
            // 
            // rtbConsole
            // 
            rtbConsole.Location = new Point(341, 193);
            rtbConsole.Name = "rtbConsole";
            rtbConsole.Size = new Size(622, 537);
            rtbConsole.TabIndex = 3;
            rtbConsole.Text = "";
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(366, 33);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(276, 23);
            txtMessage.TabIndex = 4;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(368, 65);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(274, 23);
            btnSend.TabIndex = 5;
            btnSend.Text = "SendMessage";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { StripServerStatus, StripPeerCount });
            statusStrip1.Location = new Point(0, 733);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1317, 22);
            statusStrip1.TabIndex = 6;
            statusStrip1.Text = "statusStrip1";
            // 
            // StripServerStatus
            // 
            StripServerStatus.Name = "StripServerStatus";
            StripServerStatus.Size = new Size(113, 17);
            StripServerStatus.Text = "ServerStatus: Offline";
            // 
            // StripPeerCount
            // 
            StripPeerCount.Name = "StripPeerCount";
            StripPeerCount.Size = new Size(47, 17);
            StripPeerCount.Text = "Peers: 0";
            // 
            // listPeerView
            // 
            listPeerView.Dock = DockStyle.Left;
            listPeerView.FormattingEnabled = true;
            listPeerView.Location = new Point(0, 0);
            listPeerView.Name = "listPeerView";
            listPeerView.Size = new Size(335, 733);
            listPeerView.TabIndex = 7;
            // 
            // listDiscoveredPeers
            // 
            listDiscoveredPeers.Dock = DockStyle.Right;
            listDiscoveredPeers.FormattingEnabled = true;
            listDiscoveredPeers.Location = new Point(982, 0);
            listDiscoveredPeers.Name = "listDiscoveredPeers";
            listDiscoveredPeers.Size = new Size(335, 733);
            listDiscoveredPeers.TabIndex = 8;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1317, 755);
            Controls.Add(listDiscoveredPeers);
            Controls.Add(listPeerView);
            Controls.Add(statusStrip1);
            Controls.Add(btnSend);
            Controls.Add(txtMessage);
            Controls.Add(rtbConsole);
            Controls.Add(lblServerIP);
            Controls.Add(lblPeerId);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label lblPeerId;
        private Label lblServerIP;
        private RichTextBox rtbConsole;
        private TextBox txtMessage;
        private Button btnSend;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel StripServerStatus;
        private ToolStripStatusLabel StripPeerCount;
        private ListBox listPeerView;
        private ListBox listDiscoveredPeers;
    }
}
