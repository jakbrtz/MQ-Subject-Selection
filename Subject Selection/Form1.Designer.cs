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
            this.LBXdecisions = new System.Windows.Forms.ListBox();
            this.DGVplanTable = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.LBXtime = new System.Windows.Forms.ListBox();
            this.FLPchoose = new System.Windows.Forms.FlowLayoutPanel();
            this.LBLcourse = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.DGVplanTable)).BeginInit();
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
            this.SuspendLayout();
            // 
            // LBXdecisions
            // 
            this.LBXdecisions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LBXdecisions.FormattingEnabled = true;
            this.LBXdecisions.IntegralHeight = false;
            this.LBXdecisions.Location = new System.Drawing.Point(0, 0);
            this.LBXdecisions.Name = "LBXdecisions";
            this.LBXdecisions.Size = new System.Drawing.Size(365, 287);
            this.LBXdecisions.TabIndex = 4;
            this.LBXdecisions.SelectedIndexChanged += new System.EventHandler(this.LBXdecisions_SelectedIndexChanged);
            // 
            // DGVplanTable
            // 
            this.DGVplanTable.AllowUserToAddRows = false;
            this.DGVplanTable.AllowUserToDeleteRows = false;
            this.DGVplanTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DGVplanTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4});
            this.DGVplanTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DGVplanTable.Location = new System.Drawing.Point(0, 0);
            this.DGVplanTable.Name = "DGVplanTable";
            this.DGVplanTable.ReadOnly = true;
            this.DGVplanTable.Size = new System.Drawing.Size(380, 287);
            this.DGVplanTable.TabIndex = 9;
            this.DGVplanTable.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DGVplanTable_CellClick);
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column1.HeaderText = "Subject 1";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column2.HeaderText = "Subject 2";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column3.HeaderText = "Subject 3";
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // Column4
            // 
            this.Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column4.HeaderText = "Subject 4";
            this.Column4.Name = "Column4";
            this.Column4.ReadOnly = true;
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
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.FLPchoose);
            this.splitContainer1.Size = new System.Drawing.Size(799, 522);
            this.splitContainer1.SplitterDistance = 287;
            this.splitContainer1.TabIndex = 10;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.LBXdecisions);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer2.Size = new System.Drawing.Size(799, 287);
            this.splitContainer2.SplitterDistance = 365;
            this.splitContainer2.TabIndex = 0;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.LBLcourse);
            this.splitContainer3.Panel1.Controls.Add(this.DGVplanTable);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.LBXtime);
            this.splitContainer3.Size = new System.Drawing.Size(430, 287);
            this.splitContainer3.SplitterDistance = 380;
            this.splitContainer3.TabIndex = 0;
            // 
            // LBXtime
            // 
            this.LBXtime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LBXtime.FormattingEnabled = true;
            this.LBXtime.IntegralHeight = false;
            this.LBXtime.Location = new System.Drawing.Point(0, 0);
            this.LBXtime.Name = "LBXtime";
            this.LBXtime.Size = new System.Drawing.Size(46, 287);
            this.LBXtime.TabIndex = 0;
            this.LBXtime.SelectedIndexChanged += new System.EventHandler(this.LBXtime_SelectedIndexChanged);
            // 
            // FLPchoose
            // 
            this.FLPchoose.AutoScroll = true;
            this.FLPchoose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FLPchoose.Location = new System.Drawing.Point(0, 0);
            this.FLPchoose.Name = "FLPchoose";
            this.FLPchoose.Size = new System.Drawing.Size(799, 231);
            this.FLPchoose.TabIndex = 8;
            // 
            // LBLcourse
            // 
            this.LBLcourse.BackColor = System.Drawing.SystemColors.Control;
            this.LBLcourse.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LBLcourse.Dock = System.Windows.Forms.DockStyle.Top;
            this.LBLcourse.Location = new System.Drawing.Point(0, 0);
            this.LBLcourse.Name = "LBLcourse";
            this.LBLcourse.Size = new System.Drawing.Size(380, 22);
            this.LBLcourse.TabIndex = 10;
            this.LBLcourse.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(823, 546);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DGVplanTable)).EndInit();
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
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListBox LBXdecisions;
        private System.Windows.Forms.DataGridView DGVplanTable;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ListBox LBXtime;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.FlowLayoutPanel FLPchoose;
        private System.Windows.Forms.Label LBLcourse;
    }
}

