using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using Parker.Properties;
using System.Diagnostics;
using TweetSharp;
using GSms;

namespace Parker
{
    public partial class frmMain : Form
    {
        string fpath, sendTo, conKey, conSec;
        int secs = 0, tlimit = 3;
        bool tAuth = true, tGetMen = true;

        TwitterService tServ;
        FileSystemWatcher watcher;
        List<string> tmp;
        Timer timer, menTime;
        SmsSender sender;

        public frmMain()
        {
            InitializeComponent();

            watcher = new FileSystemWatcher();
            tmp = new List<string>();
            timer = new Timer();
            menTime = new Timer();
            sender = new SmsSender(Settings.Default.SmsAccounts[0].Split(':')[0], Settings.Default.SmsAccounts[0].Split(':')[1]);
            
            nicMain.ShowBalloonTip(1000, "Parker", "I'm Up!", ToolTipIcon.Info);

            listenToolStripMenuItem.Checked = Settings.Default.Listen;
            fpath = Settings.Default.Path;
            sendTo = Settings.Default.sendTo;
            conKey = Settings.Default.conKey;
            conSec = Settings.Default.conSec;
            tGetMen = Settings.Default.tGetMen;

            if (!fpath.Equals(""))
                watcher.Path = fpath + @"\";

            watcher.Filter = "*.txt";
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = listenToolStripMenuItem.Checked;

            timer.Enabled = false;
            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);

            menTime.Enabled = false;
            menTime.Interval = 60000;
            menTime.Tick += new EventHandler(menTime_Tick);

            tAuthenticate();
        }

        private void menTime_Tick(object sender, EventArgs e)
        {
            try
            {
                getNewMentions();
            }
            catch (Exception ex)
            {
                nicMain.ShowBalloonTip(1000, "Parker", ex.Message, ToolTipIcon.Error);
            }
        }

        private void sendSMS(string number, string message)
        {
            foreach (string acc in Settings.Default.SmsAccounts)
            {
                string uname = acc.Split(':')[0];
                string upin = acc.Split(':')[1];

                sender.uName = uname;
                sender.uPin = upin;

                string resp = sender.SendSms(number, message);

                if (resp.Contains("302"))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
        }

        private void tAuthenticate()
        {
            tServ = new TwitterService(conKey, conSec);
            tServ.AuthenticateWith(Settings.Default.acTok, Settings.Default.acSec);
            tServ.VerifyCredentials();
            if (tServ.Response.StatusCode.ToString() != "OK")
            {
                OAuthRequestToken requestToken = tServ.GetRequestToken();
                Uri uri = tServ.GetAuthorizationUri(requestToken);
                Process.Start(uri.ToString());

                frmPrompt pr = new frmPrompt("Please enter the 6 digit PIN", "Twitter");
                pr.ShowDialog(this);

                OAuthAccessToken access = tServ.GetAccessToken(requestToken, pr.toRet);
                tServ.AuthenticateWith(access.Token, access.TokenSecret);

                if (tServ.Response.StatusCode.ToString() == "OK")
                {
                    Settings.Default.acTok = access.Token;
                    Settings.Default.acSec = access.TokenSecret;
                    Settings.Default.Save();
                }
                else
                {
                    nicMain.ShowBalloonTip(2000, "Parker", "Twitter Authentication Error!", ToolTipIcon.Error);
                    tAuth = false;
                }
            }

            menTime.Enabled = tAuth;
        }

        private void getNewMentions()
        {
            if (tAuth && tGetMen)
            {
                List<TwitterStatus> mentions = new List<TwitterStatus>(tServ.ListTweetsMentioningMe());
                List<TwitterStatus> limMen = new List<TwitterStatus>(mentions.Take(tlimit));
                foreach (TwitterStatus mention in limMen) 
                {
                    if (!Settings.Default.readMentions.Contains(mention.Id.ToString()))
                    {
                        Settings.Default.readMentions.Add(mention.Id.ToString());
                        sendSMS(Settings.Default.sendTo, "@" + mention.User.ScreenName + ": " + mention.Text);
                    }
                }
                Settings.Default.Save();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "Exit?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void listenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listenToolStripMenuItem.Checked && fpath.Equals(""))
            {
                setDirectoryToolStripMenuItem_Click(null, null);
            }

            Settings.Default.Listen = listenToolStripMenuItem.Checked;
            watcher.EnableRaisingEvents = listenToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        private void setDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fbdMain.ShowDialog();
            fpath = fbdMain.SelectedPath;
            Settings.Default.Path = fpath;
            watcher.Path = fpath + @"\";
            Settings.Default.Save();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (timer.Enabled)
            {
                tmp.Add(e.FullPath);
            }
            else
            {
                tmp = new List<string>();
                this.Invoke(new MethodInvoker(delegate
                {
                    timer.Enabled = true;
                    timer.Start();
                }));
                tmp.Add(e.FullPath);
            }            
        }

        private string readContent(string path)
        {
            StreamReader reader = new StreamReader(path);
            string whole = reader.ReadToEnd().Trim();
            reader.Close();
            return whole;
        }

        private void procList(List<string> lst)
        {
            List<string> data = new List<string>();
            string xdata = "";
            bool app = true;
            int prev = -1;
            foreach (string fp in lst)
            {
                if (Regex.IsMatch(fp, @"_(d{2})\.txt$"))
                {
                    Match m = Regex.Match(fp, @"_(d{2})\.txt$");
                    int x = Int32.Parse(m.Groups[1].Value);
                    if (x - prev != 1)
                    {
                        app = false;
                        prev = -1;
                    }
                    else
                    {
                        prev = x;
                    }
                }
                string datum = readContent(fp);
                if (app)
                {
                    xdata += datum;
                }
                else
                {
                    data.Add(xdata);
                    xdata = datum;
                }
            }
            data.Add(xdata);
            foreach (string d in data)
            {
                processData(d);
            }
            
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            secs++;
            if (secs >= 2)
            {
                timer.Enabled = false;
                secs = 0;
                procList(tmp);
            }
        }

        private void processData(string data)
        {
            Command cmd = parseString(data);

            switch (cmd.id)
            {
                case "tweet":
                    if (tAuth)
                        tServ.SendTweet(cmd.arg);
                    break;
                case "tmention":
                    toggleMentionService(cmd.arg);
                    break;
                case "google":
                    // Google processing here
                    break;
                case "acro":
                    break;
            }
        }

        private void toggleMentionService(string code)
        {
            switch (code)
            {
                case "1":
                    Settings.Default.tGetMen = true;
                    break;
                case "0":
                    Settings.Default.tGetMen = false;
                    break;
            }
            Settings.Default.Save();
            tGetMen = Settings.Default.tGetMen;
        }

        private Command parseString(string str)
        {
            Command cmd = new Command();
            if (str.Contains(' '))
            {
                cmd.id = str.Split(' ').FirstOrDefault();
                cmd.arg = str.Remove(0, cmd.id.Length + 1);
            }
            return cmd;
        }
    }

    class Command
    {
        public string id;
        public string arg;
    }
}
