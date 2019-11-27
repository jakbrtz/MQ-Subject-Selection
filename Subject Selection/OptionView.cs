using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Subject_Selection
{
    public partial class OptionView : UserControl
    {
        public Option Option { get; }

        public OptionView(Option option)
        {
            InitializeComponent();
            
            Option = option;

            switch (Option)
            {
                case Subject subject:
                    LBLcode.Text = subject.ID;
                    LBLname.Text = subject.Name;
                    break;
                case Decision decision:
                    LBLname.Text = decision.ToString();
                    break;
            }
        }

        public new event EventHandler Click
        {
            add
            {
                base.Click += value;
                foreach (Control control in Controls)
                {
                    control.Click += value;
                }
            }
            remove
            {
                base.Click -= value;
                foreach (Control control in Controls)
                {
                    control.Click -= value;
                }
            }
        }
    }
}
