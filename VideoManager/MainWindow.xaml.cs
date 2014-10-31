using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;

namespace VideoManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string strConn = @"Data Source=E:\\Data2\\Thumbs\\.temp\\test.db";
            conn_ = new SQLiteConnection(strConn);
            conn_.Open();
        }

        private SQLiteConnection conn_;

        private void OpenVideoFiles(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Select Video Files";
            diag.Filter = "Video Files|*.avi; *.mp4; *.wmv; *.mkv";
            diag.FilterIndex = 1;
            diag.Multiselect = true;

            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string dirPath = System.IO.Path.GetDirectoryName(diag.FileName);

                foreach (string filename in diag.FileNames)
                {
                    string fullPath = System.IO.Path.Combine(dirPath, filename);
                    importVideo(filename, fullPath);
                }

            }

        }

        private void importVideo(string filename, string fullPath)
        {
            string code = getCode(filename);
            if(code == null) {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. Can't find code", filename));
                return;
            }

            SQLiteCommand cmd = new SQLiteCommand(String.Format("SELECT id FROM video WHERE code = '{0}'", code), conn_);
            object ret = cmd.ExecuteScalar();
            if(ret != null) {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. Duplicate code {1}", filename, code));
                return;
            }

            System.Windows.MessageBox.Show(String.Format("{0}: ACCEPTED. Code: {1}, Path: {2}", filename, code, ""));
        }

        private string getCode(string filename)
        {
            Regex reg = new Regex(@"([a-zA-Z]{2,5})\-?([0-9]{3,4})", RegexOptions.IgnoreCase);

            Match match = reg.Match(filename);
            if(match.Groups.Count < 3) return null;

            return String.Format("{0}-{1}", match.Groups[1], match.Groups[2]).ToUpper();
        }

    }
}
