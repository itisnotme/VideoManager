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

                updateVideoList();
            }
        }

        private void updateVideoList()
        {
            SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT code, filename FROM video", conn_);
            System.Data.DataSet ds = new System.Data.DataSet();
            da.Fill(ds);
            lvVidoes.DataContext = ds.Tables[0].DefaultView;
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

            // Check info and page to user
            VideoInfoWindow infoWindow = new VideoInfoWindow();
            infoWindow.lbCode.Content = code;
            infoWindow.lbFileName.Content = filename;
            infoWindow.wbBrowser.Navigate(info.url);
            foreach (string actor in info.actors)
                infoWindow.lbCast.Content += actor + ", ";
            foreach (string genre in info.genres)
                infoWindow.lbGenre.Content += genre + ", ";

            Nullable<bool> diagRet = infoWindow.ShowDialog();
            if (diagRet == false)
            {
                System.Windows.MessageBox.Show(String.Format("{0}: REJECTED. User reject", filename));
                return;
            }

            // Update DB
            SQLiteTransaction tr = conn_.BeginTransaction();
            cmd.Transaction = tr;

            cmd.CommandText = String.Format("INSERT INTO video (code, title, filename, url) VALUES ('{0}', '{1}', '{2}', '{3}')", code, info.title, filename, info.url);
            cmd.ExecuteNonQuery();

            cmd.CommandText = String.Format("SELECT id FROM video WHERE code = '{0}'", code);
            long id = (long)cmd.ExecuteScalar();

            foreach (string actor in info.actors)
            {
                cmd.CommandText = string.Format("INSERT INTO actor VALUES ({0}, '{1}')", id, actor);
                cmd.ExecuteNonQuery();
            }

            foreach (string genre in info.genres)
            {
                cmd.CommandText = string.Format("INSERT INTO tag VALUES ({0}, '{1}')", id, genre);
                cmd.ExecuteNonQuery();
            }

            tr.Commit();

            // Move file
            string repoPath = System.IO.Path.Combine("E:\\Data2\\Thumbs\\.temp\\repository", filename);
            System.IO.File.Move(fullPath, repoPath);

            System.Windows.MessageBox.Show(String.Format("{0}: ACCEPTED. Code: {1}, Path: {2}", filename, code, repoPath));
        }

        private bool getVideoInfo(string target_code, VideoInfo info)
        {
            string url = String.Format("http://www.javlibrary.com/en/vl_searchbyid.php?keyword={0}", target_code);
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(url);

            HtmlNode dnode = doc.DocumentNode;
            if(dnode.SelectSingleNode("//*[@id=\"rightcolumn\"]/div[1]/text()").InnerText.Contains("ID Search Result")) {
                HtmlNodeCollection videos = dnode.SelectNodes("//*[@id=\"rightcolumn\"]/div[2]/div/div");
                if (videos == null) return false;

                bool found = false;
                foreach (HtmlNode node in videos)
                {
                    string code = node.SelectSingleNode("./a/div[1]/text()").InnerText;
                    if (code == target_code)
                    {
                        found = true;

                        url = "http://www.javlibrary.com/en" + node.SelectSingleNode("./a[1]").Attributes["href"].Value.Substring(1);
                        doc = web.Load(url);
                        dnode = doc.DocumentNode;
                        break;
                    }
                }

                if (found == false) return false;
            }

            info.url = url;
            info.title = dnode.SelectSingleNode("//*[@id=\"video_title\"]/h3/a/text()").InnerText;
            
            HtmlNodeCollection nodes = dnode.SelectNodes("//span[@class=\"cast\"]/span/a/text()");
            if(nodes != null)
                foreach(HtmlNode node in nodes)
                    info.actors.Add(node.InnerText);

            nodes = dnode.SelectNodes("//span[@class=\"genre\"]/a/text()");
            if(nodes != null)
                foreach(HtmlNode node in nodes)
                    info.genres.Add(node.InnerText);

            return true;
        }

        private string getCode(string filename)
        {
            Regex reg = new Regex(@"([a-zA-Z]{2,5})\-?([0-9]{3,5})", RegexOptions.IgnoreCase);

            Match match = reg.Match(filename);
            if (match.Groups.Count < 3) return null;

            return String.Format("{0}-{1}", match.Groups[1], match.Groups[2]).ToUpper();
        }

        private void DoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.ListView lv = sender as System.Windows.Controls.ListView;
            System.Data.DataRowView data = lv.SelectedItem as System.Data.DataRowView;
            string filename = (string)data.Row.ItemArray[1];

            string fullPath = System.IO.Path.Combine("E:\\Data2\\Thumbs\\.temp\\repository", filename);
            System.Diagnostics.Process.Start(fullPath);
        }

        private void lvVidoes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OpenDBFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Select DB Files";
            diag.Filter = "DB File|*.db";
            diag.FilterIndex = 1;
            
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string dbFile = diag.FileName;
                string strConn = String.Format("Data Source={0}", dbFile);
                conn_ = new SQLiteConnection(strConn);
                conn_.Open();

                SQLiteCommand cmd = conn_.CreateCommand();

                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' and name = 'video'";
                object ret = cmd.ExecuteScalar();
                if (ret == null)
                {
                    cmd.CommandText = "CREATE TABLE video (id INTEGER PRIMARY KEY AUTOINCREMENT, code CHAR(10), title VARCHAR(256), filename VARCHAR(256), url VARCHAR(1024))";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' and name = 'actor'";
                ret = cmd.ExecuteScalar();
                if (ret == null)
                {
                    cmd.CommandText = "CREATE TABLE actor (video_id INTEGER, actor VARCHAR(128), PRIMARY KEY(video_id, actor))";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' and name = 'tag'";
                ret = cmd.ExecuteScalar();
                if (ret == null)
                {
                    cmd.CommandText = "CREATE TABLE tag (video_id INTEGER, tag VARCHAR(128), PRIMARY KEY(video_id, tag))";
                    cmd.ExecuteNonQuery();
                }

                btnOpenVideo.IsEnabled = true;

                updateVideoList();
            }
        }

    }
}
