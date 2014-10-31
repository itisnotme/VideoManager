using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace VideoManager
{
    /// <summary>
    /// Interaction logic for VideoInfo.xaml
    /// </summary>
    public partial class VideoInfoWindow : Window
    {
        public VideoInfoWindow()
        {
            InitializeComponent();
        }

        private void ClickOK(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ClickNo(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
