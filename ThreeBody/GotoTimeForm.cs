using System;
using System.Windows.Forms;

namespace ThreeBody
{
    public partial class GotoTimeForm : Form
    {
        private readonly TimeSpan _initialValue;

        public GotoTimeForm(TimeSpan elapsed)
        {
            InitializeComponent();
            _initialValue = elapsed;
            Reset();
        }

        public TimeSpan CurrentValue
        {
            get
            {
                TimeSpan val;
                if (TimeSpan.TryParse(TimeText.Text, out val))
                {
                    return val;
                }
                return TimeSpan.Zero;
            }
        }

        public bool Changed
        {
            get { return CurrentValue != _initialValue; }
        }

        private void TimeText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Close();
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            Reset();
        }

        private void Reset()
        {
            TimeText.Text = _initialValue.ToString();
        }

        private void BtnZero_Click(object sender, EventArgs e)
        {
            TimeText.Text = TimeSpan.Zero.ToString();
        }
    }
}
