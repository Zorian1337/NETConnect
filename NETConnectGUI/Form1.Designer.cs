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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            cmbPeerList = new ComboBox();
            btnSendMessage = new Button();
            txtMessage = new TextBox();
            rtbMessageHistory = new RichTextBox();
            lblPeerId = new Label();
            tabPage2 = new TabPage();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(946, 581);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(cmbPeerList);
            tabPage1.Controls.Add(btnSendMessage);
            tabPage1.Controls.Add(txtMessage);
            tabPage1.Controls.Add(rtbMessageHistory);
            tabPage1.Controls.Add(lblPeerId);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(938, 553);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tab1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // cmbPeerList
            // 
            cmbPeerList.FormattingEnabled = true;
            cmbPeerList.Location = new Point(321, 260);
            cmbPeerList.Name = "cmbPeerList";
            cmbPeerList.Size = new Size(312, 23);
            cmbPeerList.TabIndex = 4;
            cmbPeerList.Click += cmbPeerList_Click;
            // 
            // btnSendMessage
            // 
            btnSendMessage.Location = new Point(321, 318);
            btnSendMessage.Name = "btnSendMessage";
            btnSendMessage.Size = new Size(312, 23);
            btnSendMessage.TabIndex = 3;
            btnSendMessage.Text = "Send Message";
            btnSendMessage.UseVisualStyleBackColor = true;
            btnSendMessage.Click += btnSendMessage_Click;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(321, 289);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(312, 23);
            txtMessage.TabIndex = 2;
            // 
            // rtbMessageHistory
            // 
            rtbMessageHistory.Location = new Point(321, 46);
            rtbMessageHistory.Name = "rtbMessageHistory";
            rtbMessageHistory.Size = new Size(312, 208);
            rtbMessageHistory.TabIndex = 1;
            rtbMessageHistory.Text = "";
            // 
            // lblPeerId
            // 
            lblPeerId.AutoSize = true;
            lblPeerId.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPeerId.Location = new Point(8, 19);
            lblPeerId.Name = "lblPeerId";
            lblPeerId.Size = new Size(82, 30);
            lblPeerId.TabIndex = 0;
            lblPeerId.Text = "PeerId: ";
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(938, 553);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tab2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 559);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(946, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(41, 17);
            toolStripStatusLabel1.Text = "Peers: ";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(946, 581);
            Controls.Add(statusStrip1);
            Controls.Add(tabControl1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            Shown += Form1_Shown;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private StatusStrip statusStrip1;
        private Label lblPeerId;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private Button btnSendMessage;
        private TextBox txtMessage;
        private RichTextBox rtbMessageHistory;
        private ComboBox cmbPeerList;
    }
}
