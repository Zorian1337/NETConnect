namespace NETConnectGUI.UserControls
{
    partial class TabSwitcher
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
            PanelSwitch = new Panel();
            mainPanel = new Panel();
            btnNetwork = new Button();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            button5 = new Button();
            button6 = new Button();
            label1 = new Label();
            button7 = new Button();
            panelNetwork = new Panel();
            label2 = new Label();
            dataGridView1 = new DataGridView();
            PeerId = new DataGridViewTextBoxColumn();
            IP = new DataGridViewTextBoxColumn();
            ServerPort = new DataGridViewTextBoxColumn();
            PanelSwitch.SuspendLayout();
            mainPanel.SuspendLayout();
            panelNetwork.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // PanelSwitch
            // 
            PanelSwitch.BackColor = Color.FromArgb(40, 40, 58);
            PanelSwitch.Controls.Add(button7);
            PanelSwitch.Controls.Add(label1);
            PanelSwitch.Controls.Add(button6);
            PanelSwitch.Controls.Add(button5);
            PanelSwitch.Controls.Add(button4);
            PanelSwitch.Controls.Add(button3);
            PanelSwitch.Controls.Add(button2);
            PanelSwitch.Controls.Add(btnNetwork);
            PanelSwitch.Dock = DockStyle.Left;
            PanelSwitch.Location = new Point(0, 0);
            PanelSwitch.Name = "PanelSwitch";
            PanelSwitch.Size = new Size(299, 748);
            PanelSwitch.TabIndex = 0;
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.FromArgb(30, 30, 46);
            mainPanel.Controls.Add(panelNetwork);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(299, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(1634, 748);
            mainPanel.TabIndex = 1;
            // 
            // btnNetwork
            // 
            btnNetwork.FlatStyle = FlatStyle.Flat;
            btnNetwork.Font = new Font("Segoe UI", 24F);
            btnNetwork.ForeColor = Color.FromArgb(205, 214, 244);
            btnNetwork.ImageAlign = ContentAlignment.MiddleRight;
            btnNetwork.Location = new Point(17, 109);
            btnNetwork.Name = "btnNetwork";
            btnNetwork.Size = new Size(258, 59);
            btnNetwork.TabIndex = 0;
            btnNetwork.Text = " Network";
            btnNetwork.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnNetwork.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 24F);
            button2.ForeColor = Color.FromArgb(205, 214, 244);
            button2.Location = new Point(17, 200);
            button2.Name = "button2";
            button2.Size = new Size(258, 59);
            button2.TabIndex = 1;
            button2.Text = "Peers";
            button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Segoe UI", 24F);
            button3.ForeColor = Color.FromArgb(205, 214, 244);
            button3.Location = new Point(17, 291);
            button3.Name = "button3";
            button3.Size = new Size(258, 59);
            button3.TabIndex = 2;
            button3.Text = "Chat";
            button3.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            button4.FlatStyle = FlatStyle.Flat;
            button4.Font = new Font("Segoe UI", 24F);
            button4.ForeColor = Color.FromArgb(205, 214, 244);
            button4.Location = new Point(17, 382);
            button4.Name = "button4";
            button4.Size = new Size(258, 59);
            button4.TabIndex = 3;
            button4.Text = "Voice";
            button4.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            button5.FlatStyle = FlatStyle.Flat;
            button5.Font = new Font("Segoe UI", 24F);
            button5.ForeColor = Color.FromArgb(205, 214, 244);
            button5.Location = new Point(17, 473);
            button5.Name = "button5";
            button5.Size = new Size(258, 59);
            button5.TabIndex = 4;
            button5.Text = "Files";
            button5.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            button6.FlatStyle = FlatStyle.Flat;
            button6.Font = new Font("Segoe UI", 24F);
            button6.ForeColor = Color.FromArgb(205, 214, 244);
            button6.Location = new Point(17, 564);
            button6.Name = "button6";
            button6.Size = new Size(258, 59);
            button6.TabIndex = 5;
            button6.Text = "Settings";
            button6.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 26.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(205, 214, 244);
            label1.Location = new Point(66, 37);
            label1.Name = "label1";
            label1.Size = new Size(173, 47);
            label1.TabIndex = 6;
            label1.Text = "P2P Mesh";
            // 
            // button7
            // 
            button7.FlatStyle = FlatStyle.Flat;
            button7.Font = new Font("Segoe UI", 24F);
            button7.ForeColor = Color.FromArgb(205, 214, 244);
            button7.Location = new Point(17, 655);
            button7.Name = "button7";
            button7.Size = new Size(258, 59);
            button7.TabIndex = 7;
            button7.Text = "button7";
            button7.UseVisualStyleBackColor = true;
            // 
            // panelNetwork
            // 
            panelNetwork.Controls.Add(dataGridView1);
            panelNetwork.Controls.Add(label2);
            panelNetwork.Dock = DockStyle.Fill;
            panelNetwork.ForeColor = Color.FromArgb(205, 214, 244);
            panelNetwork.Location = new Point(0, 0);
            panelNetwork.Name = "panelNetwork";
            panelNetwork.Size = new Size(1634, 748);
            panelNetwork.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(24, 27);
            label2.Name = "label2";
            label2.Size = new Size(74, 25);
            label2.TabIndex = 0;
            label2.Text = "PeerId: ";
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { PeerId, IP, ServerPort });
            dataGridView1.Location = new Point(24, 93);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(1548, 194);
            dataGridView1.TabIndex = 1;
            // 
            // PeerId
            // 
            PeerId.HeaderText = "PeerId";
            PeerId.Name = "PeerId";
            PeerId.ReadOnly = true;
            // 
            // IP
            // 
            IP.HeaderText = "ServerIP";
            IP.Name = "IP";
            IP.ReadOnly = true;
            // 
            // ServerPort
            // 
            ServerPort.HeaderText = "ServerPort";
            ServerPort.Name = "ServerPort";
            ServerPort.ReadOnly = true;
            // 
            // TabSwitcher
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(mainPanel);
            Controls.Add(PanelSwitch);
            Name = "TabSwitcher";
            Size = new Size(1933, 748);
            PanelSwitch.ResumeLayout(false);
            PanelSwitch.PerformLayout();
            mainPanel.ResumeLayout(false);
            panelNetwork.ResumeLayout(false);
            panelNetwork.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel PanelSwitch;
        private Button button2;
        private Button btnNetwork;
        private Panel mainPanel;
        private Label label1;
        private Button button6;
        private Button button5;
        private Button button4;
        private Button button3;
        private Button button7;
        private Panel panelNetwork;
        private DataGridView dataGridView1;
        private Label label2;
        private DataGridViewTextBoxColumn PeerId;
        private DataGridViewTextBoxColumn IP;
        private DataGridViewTextBoxColumn ServerPort;
    }
}
