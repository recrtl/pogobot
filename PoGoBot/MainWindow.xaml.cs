using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PoGoBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Width = Properties.Settings.Default.WindowSize.Width;
            this.Height = Properties.Settings.Default.WindowSize.Height;
            this.Left = Properties.Settings.Default.WindowPosition.X;
            this.Top = Properties.Settings.Default.WindowPosition.Y;
            this.WindowState = (WindowState)Properties.Settings.Default.WindowsState;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            Properties.Settings.Default.WindowPosition = new System.Drawing.Point((int)this.Left, (int)this.Top);
            Properties.Settings.Default.WindowsState = (int)this.WindowState;
            Properties.Settings.Default.Save();
        }
    }
}
