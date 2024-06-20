using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace KeyLockCheckApp
{
    public partial class KeyLockCheckWindow : Form
    {



        public KeyLockCheckWindow()
        {
            InitializeComponent();
            ContextMenuStrip menuStrip = new ContextMenuStrip();
            menuStrip.Items.Add("Exit", null, Exit_Click);
            notifyIcon1.ContextMenuStrip = menuStrip;
            // create instance of HotKeys
            HotKeys.Init();
        }
        private void Exit_Click(object sender, EventArgs e)
        {
            // Clean up and exit application
            notifyIcon1.Visible = false;
            Application.Exit();
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            //if the form is minimized
            //hide it from the task bar
            //and show the system tray icon (represented by the NotifyIcon control)
            //if (this.WindowState == FormWindowState.Minimized)
            //{
            //    Hide();
            //    notifyIcon1.Visible = true;
                
            //}
        }
         private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.WindowState = FormWindowState.Minimized;
            //Form1_Resize(sender, e);

            //Hide();

            Form form = (Form)sender;
            form.ShowInTaskbar = false;
            form.Opacity = 0;
        }
    }
}
