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
using HtmlAgilityPack;

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

                foreach (string filename in diag.SafeFileNames)
                {
                    string fullPath = System.IO.Path.Combine(dirPath, filename);
                    importVideo(filename, fullPath);
                }

            }

        }

        class VideoInfo
        {
            public VideoInfo()
            {
                actors = new List<string>();
                genres = new List<string>();
            }

            public string title { get; set; }
            public List<string> actors { get; set; }
            public List<string> genres { get; set; }
            public string url { get; set; }
        }

        private void importVideo(string filename, string fullPath)
        {
            string code = getCode(filename);
            if (code == null)
            {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. Can't find code", filename));
                return;
            }

            SQLiteCommand cmd = new SQLiteCommand(String.Format("SELECT id FROM video WHERE code = '{0}'", code), conn_);
            object ret = cmd.ExecuteScalar();
            if (ret != null)
            {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. Duplicate code {1}", filename, code));
                return;
            }

            VideoInfo info = new VideoInfo();
            bool success = getVideoInfo(code, info);
            if (success == false)
            {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. Can't get info for {1}", filename, code));
                return;
            }

            System.Windows.MessageBox.Show(String.Format("{0}: ACCEPTED. Code: {1}, Path: {2}", filename, code, ""));
        }

        private bool getVideoInfo(string target_code, VideoInfo info)
        {
            string url = String.Format("http://www.javlibrary.com/en/vl_searchbyid.php?keyword={0}", target_code);
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);

            HtmlNode dnode = doc.DocumentNode;
            if(dnode.SelectSingleNode("//*[@id=\"rightcolumn\"]/div[1]/text()").InnerText.Contains("ID Search Result")) {
                bool found = false;
                foreach (HtmlNode node in dnode.SelectNodes("//*[@id=\"rightcolumn\"]/div[2]/div/div"))
                {
                    string code = node.SelectSingleNode("./a/div[1]/text()").InnerText;
                    if (code == target_code)
                    {
                        found = true;

                        url = "http://www.javlibrary.com/en" + dnode.SelectSingleNode("./a[1]").Attributes["href"].Value.Substring(1);
                        doc = web.Load(url);
                        dnode = doc.DocumentNode;
                        break;
                    }
                }

                if (false == false) return false;
            }

            info.url = url;
            info.title = dnode.SelectSingleNode("//*[@id=\"video_title\"]/h3/a/text()").InnerText;
            
            dnode.SelectNodes("//span[@class=\"cast\"]/span/a/text()");
            HtmlNodeCollection nodes = dnode.SelectNodes("//span[@class=\"cast\"]/span/a/text()");
            if(nodes != null)
                foreach(HtmlNode node in nodes)
                    info.actors.Add(node.InnerText);

            dnode.SelectNodes("//span[@class=\"genre\"]/a/text()");
            nodes = dnode.SelectNodes("//span[@class=\"cast\"]/span/a/text()");
            if(nodes != null)
                foreach(HtmlNode node in nodes)
                    info.genres.Add(node.InnerText);

            return true;
        }

        private string getCode(string filename)
        {
            Regex reg = new Regex(@"([a-zA-Z]{2,5})\-?([0-9]{3,4})", RegexOptions.IgnoreCase);

            Match match = reg.Match(filename);
            if (match.Groups.Count < 3) return null;

            return String.Format("{0}-{1}", match.Groups[1], match.Groups[2]).ToUpper();
        }

    }
}
