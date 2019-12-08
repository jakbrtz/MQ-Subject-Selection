namespace Subject_Selection
{
    partial class Form1
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
            this.BTNremove = new System.Windows.Forms.Button();
            this.LBXtime = new System.Windows.Forms.ListBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.FLPchoose = new System.Windows.Forms.FlowLayoutPanel();
            this.LBXdecisions = new System.Windows.Forms.ListBox();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.DGVplanTable = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DGVplanTable)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // BTNremove
            // 
            this.BTNremove.Location = new System.Drawing.Point(61, 3);
            this.BTNremove.Name = "BTNremove";
            this.BTNremove.Size = new System.Drawing.Size(55, 23);
            this.BTNremove.TabIndex = 1;
            this.BTNremove.Text = "remove";
            this.BTNremove.UseVisualStyleBackColor = true;
            this.BTNremove.Click += new System.EventHandler(this.BTNremove_Click);
            // 
            // LBXtime
            // 
            this.LBXtime.Dock = System.Windows.Forms.DockStyle.Left;
            this.LBXtime.FormattingEnabled = true;
            this.LBXtime.IntegralHeight = false;
            this.LBXtime.Location = new System.Drawing.Point(0, 0);
            this.LBXtime.Name = "LBXtime";
            this.LBXtime.Size = new System.Drawing.Size(55, 109);
            this.LBXtime.TabIndex = 0;
            this.LBXtime.SelectedIndexChanged += new System.EventHandler(this.LBXtime_SelectedIndexChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(991, 522);
            this.splitContainer1.SplitterDistance = 261;
            this.splitContainer1.TabIndex = 12;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 16);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.FLPchoose);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.LBXdecisions);
            this.splitContainer2.Size = new System.Drawing.Size(985, 242);
            this.splitContainer2.SplitterDistance = 797;
            this.splitContainer2.TabIndex = 13;
            // 
            // FLPchoose
            // 
            this.FLPchoose.AutoScroll = true;
            this.FLPchoose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FLPchoose.Location = new System.Drawing.Point(0, 0);
            this.FLPchoose.Name = "FLPchoose";
            this.FLPchoose.Size = new System.Drawing.Size(797, 242);
            this.FLPchoose.TabIndex = 8;
            // 
            // LBXdecisions
            // 
            this.LBXdecisions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LBXdecisions.FormattingEnabled = true;
            this.LBXdecisions.IntegralHeight = false;
            this.LBXdecisions.Location = new System.Drawing.Point(0, 0);
            this.LBXdecisions.Name = "LBXdecisions";
            this.LBXdecisions.Size = new System.Drawing.Size(184, 242);
            this.LBXdecisions.TabIndex = 4;
            this.LBXdecisions.SelectedIndexChanged += new System.EventHandler(this.LBXdecisions_SelectedIndexChanged);
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.BTNremove);
            this.splitContainer3.Panel2.Controls.Add(this.LBXtime);
            this.splitContainer3.Size = new System.Drawing.Size(991, 257);
            this.splitContainer3.SplitterDistance = 144;
            this.splitContainer3.TabIndex = 13;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.DGVplanTable);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(991, 144);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Schedule";
            // 
            // DGVplanTable
            // 
            this.DGVplanTable.AllowUserToAddRows = false;
            this.DGVplanTable.AllowUserToDeleteRows = false;
            this.DGVplanTable.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.DGVplanTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DGVplanTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DGVplanTable.Location = new System.Drawing.Point(3, 16);
            this.DGVplanTable.Name = "DGVplanTable";
            this.DGVplanTable.ReadOnly = true;
            this.DGVplanTable.Size = new System.Drawing.Size(985, 125);
            this.DGVplanTable.TabIndex = 11;
            this.DGVplanTable.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DGVplanTable_CellClick);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.splitContainer2);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(991, 261);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Make a Decision";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 546);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DGVplanTable)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox LBXtime;
        private System.Windows.Forms.Button BTNremove;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ListBox LBXdecisions;
        private System.Windows.Forms.FlowLayoutPanel FLPchoose;
        private System.Windows.Forms.DataGridView DGVplanTable;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

