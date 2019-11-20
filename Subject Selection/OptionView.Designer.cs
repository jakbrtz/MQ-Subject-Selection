namespace Subject_Selection
{
    partial class OptionView
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
            this.LBLcode = new System.Windows.Forms.Label();
            this.LBLname = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // LBLcode
            // 
            this.LBLcode.AutoSize = true;
            this.LBLcode.Location = new System.Drawing.Point(3, 0);
            this.LBLcode.Name = "LBLcode";
            this.LBLcode.Size = new System.Drawing.Size(50, 13);
            this.LBLcode.TabIndex = 0;
            this.LBLcode.Text = "C000000";
            // 
            // LBLname
            // 
            this.LBLname.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LBLname.Location = new System.Drawing.Point(3, 13);
            this.LBLname.Name = "LBLname";
            this.LBLname.Size = new System.Drawing.Size(144, 56);
            this.LBLname.TabIndex = 1;
            this.LBLname.Text = "Name which can get pretty long";
            // 
            // OptionView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.LBLname);
            this.Controls.Add(this.LBLcode);
            this.Name = "OptionView";
            this.Size = new System.Drawing.Size(150, 69);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LBLcode;
        private System.Windows.Forms.Label LBLname;
    }
}
