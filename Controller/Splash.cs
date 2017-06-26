using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Controller
{
    public partial class Splash : Form
    {
        public Splash()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        //给Form1返回进度条控件
        public ProgressBar getProgressBar()
        {
            return this.progressBar1;
        }

        //通过代理更新Label
        public delegate void updateLableCallback(int index, int count);
        public void updateLabel(int index, int count)
        {
            if (this.label2.InvokeRequired)
            {
                updateLableCallback call = new updateLableCallback(updateLabel);
                this.Invoke(call, new object[] { index, count });
            }
            else
            {
                this.label2.Text = index + "/" + count;
            }
        }
    }
}
