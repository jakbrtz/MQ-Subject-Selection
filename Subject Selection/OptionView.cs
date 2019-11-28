using System;
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
                    label1.Text = subject.ID + Environment.NewLine + subject.Name;
                    break;
                case Decision decision:
                    label1.Text = decision.ToString();
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
