// MainWindow
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;


using Application = System.Windows.Application;
//using xProjectGenerator;
using GenRessurect.Properties;

namespace GenRessurect
{
    public partial class MainWindow : Window, IComponentConnector
    {
        private string _linksDir;

        private string _emailsPath;

        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        private string _linksPath;

        private static Random _rnd = new Random();

        private Stopwatch _sw = new Stopwatch();

        private List<string> _linksFilePaths = new List<string>();

        private int _totalRows = 0;

        private string _emailsFilePath;

        private string _xrumerPath;

        private string _outputPath;

        private int _threads;

        //internal System.Windows.Controls.TextBox OutputTextBox;

        //internal System.Windows.Controls.TextBox LinksTextBox;

        internal System.Windows.Controls.Label label;

        internal System.Windows.Controls.Label label_Copy;

        //internal System.Windows.Controls.TextBox EmailsTextBox;

        internal System.Windows.Controls.Label label_Copy1;

        //internal System.Windows.Controls.Button GenerateSchedule_Button;

        //internal System.Windows.Controls.TextBox XrumerPathTexBox;

        internal System.Windows.Controls.Label label_Copy2;

        //internal System.Windows.Controls.ProgressBar ProcessedLinesProgressBar;

        //internal System.Windows.Controls.Button GenerateProjectsButton;

        internal System.Windows.Controls.TextBox ThreadsBox;

        //private bool _contentLoaded;


        
        private string ProjectName
        {
            get;
            set;
        }

        public static object Links
        {
            get;
            set;
        }

        public MainWindow()
        {

            InitializeComponent();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            if (!string.IsNullOrWhiteSpace(Settings.Default.XrumerPath) && File.Exists(Path.Combine(Settings.Default.XrumerPath, "xpymep.exe")))
            {
                XrumerPathTexBox.Text = Settings.Default.XrumerPath;
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.LinksDir) && Directory.Exists(Settings.Default.LinksDir))
            {
                LinksTextBox.Text = Settings.Default.LinksDir;
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.EmailsPath) && File.Exists(Settings.Default.EmailsPath))
            {
                EmailsTextBox.Text = Settings.Default.EmailsPath;
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.OutputDir) && Directory.Exists(Settings.Default.OutputDir))
            {
                OutputTextBox.Text = Settings.Default.OutputDir;
            }
        }

        private void XrumerPathTexBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                switch (folderBrowserDialog.ShowDialog())
                {
                    case System.Windows.Forms.DialogResult.None:
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        break;
                    case System.Windows.Forms.DialogResult.Abort:
                        break;
                    case System.Windows.Forms.DialogResult.Retry:
                        break;
                    case System.Windows.Forms.DialogResult.Ignore:
                        break;
                    case System.Windows.Forms.DialogResult.Yes:
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    case System.Windows.Forms.DialogResult.OK:
                        XrumerPathTexBox.Text = folderBrowserDialog.SelectedPath;
                        Settings.Default.XrumerPath = folderBrowserDialog.SelectedPath;
                        _xrumerPath = folderBrowserDialog.SelectedPath;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void LinksTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _linksPath = LinksTextBox.Text;
        }

        private static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
                ? Application.Current.Windows.OfType<T>().Any()
                : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //Application
            //    .Current
            //    .Windows
            //    .OfType<PropertiesWindow>().ToList()
            //    .ForEach(w => w.Close());
            Settings.Default.Save();
        }

        private static string GeneratePasswd()
        {
            return Membership.GeneratePassword(11, 0);
        }

        private string GenerateNickName()
        {
            string[] array = File.ReadAllLines(Path.Combine(_xrumerPath, "ProjectFill", "male_names.txt"));
            string text = array[_rnd.Next(0, array.Length - 1)].Split(' ').First();
            return text + "#gennick[" + RandomString(1) + RandomString(13).ToLower() + RandomString(2) + ",2,5]";
        }

        public static string RandomString(int length)
        {
            return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ", length)
                               select s[_rnd.Next(s.Length)]).ToArray());
        }

        public static string RandomStringAlpha(int length)
        {
            return new string((from s in Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length)
                               select (_rnd.Next(10) % 2 == 0) ? char.ToLower(s[_rnd.Next(s.Length)], CultureInfo.CurrentCulture) : s[_rnd.Next(s.Length)]).ToArray());
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_xrumerPath))
            {
                System.Windows.MessageBox.Show("XrumerPath doesn't exists", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else if (!Directory.Exists(_linksPath))
            {
                System.Windows.MessageBox.Show("Links dir doesn't exists", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else if (!File.Exists(_emailsFilePath))
            {
                System.Windows.MessageBox.Show("Emails file doesn't exists", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                if (!Directory.Exists(_outputPath))
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Output Path doesn't exists. Create it?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (messageBoxResult != MessageBoxResult.Yes)
                    {
                        return;
                    }
                    Directory.CreateDirectory(_outputPath);
                }
                if (!backgroundWorker.IsBusy)
                {
                    GenerateProjectsButton.IsEnabled = false;
                    backgroundWorker.DoWork += Generating;
                    
                    backgroundWorker.RunWorkerAsync();
                }
            }
        }

        private void Generating(object sender, DoWorkEventArgs e)
        {
            _sw.Reset();
            _sw.Start();
            _totalRows = 0;
            base.Dispatcher.BeginInvoke((Action)delegate
            {
                GenerateProjectsButton.IsEnabled = false;
            });
            List<string> ext = new List<string>
        {
            ".txt"
        };
            IEnumerable<string> enumerable = from s in Directory.GetFiles(_linksPath, "*.*", SearchOption.TopDirectoryOnly)
                                             where ext.Contains(Path.GetExtension(s))
                                             select s;
            string[][] validMails = GetValidMails(_emailsFilePath);
            int num = 0;
            _linksFilePaths = ((enumerable as List<string>) ?? enumerable.ToList());
            if (_linksFilePaths.Count > validMails.Length)
            {
                System.Windows.MessageBox.Show($"Email list contains `{validMails.Length}` unused emails, you trying to generate `{_linksFilePaths.Count}`", "Warning!", MessageBoxButton.OK);
            }
            else
            {
                int linesTotal = _linksFilePaths.SelectMany(File.ReadAllLines).Count();
                ProcessedLinesProgressBar.Dispatcher.BeginInvoke((Action)delegate
                {
                    ProcessedLinesProgressBar.Maximum = (double)linesTotal;
                });
                int num2 = 0;
                foreach (string linksFilePath in _linksFilePaths)
                {
                    ProjectName = Path.GetFileNameWithoutExtension(linksFilePath).Replace('.', '_') + "_Generated";
                    string text = GenerateNickName();
                    string text2 = RandomStringAlpha(11);
                    string text3 = validMails[num % validMails.Length][0];
                    string text4 = validMails[num % validMails.Length][1];
                    string[] array = GenerateCountryCity();
                    string path = Path.Combine(_outputPath, ProjectName + ".xml");
                    string text5 = "C:\\Temp\\" + ProjectName + "_URLs.txt";
                    string[] array2 = File.ReadAllLines(linksFilePath);
                    string contents = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + 
                        "<XRumerProject>" + Environment.NewLine + 
                        "  <PrimarySection>" + Environment.NewLine + 
                        "    <ProjectName>" + ProjectName + "</ProjectName>" + Environment.NewLine + 
                        "    <ProjectFormat>ANSI</ProjectFormat>" + Environment.NewLine + 
                        "    <NickName>" + text + "</NickName>" + Environment.NewLine + 
                        "    <RealName>" + text + RandomString(2) + "</RealName>" + Environment.NewLine + 
                        "    <Password>" + text2 + "</Password>" + Environment.NewLine + 
                        "    <EmailAddress>" + text3 + "</EmailAddress>" + Environment.NewLine + 
                        "    <EmailPassword>" + text4 + "</EmailPassword>" + Environment.NewLine + 
                        "    <EmailLogin>" + text3 + "</EmailLogin>" + Environment.NewLine + 
                        "    <EmailPOP>" + SetEmailPop(text3) + "</EmailPOP>" + Environment.NewLine + 
                        "    <Homepage>#file_links_A[\"C:\\Temp\\" + ProjectName + "_URLs.txt\",1,N]</Homepage>" + Environment.NewLine +
                       $"    <ICQ>{_rnd.Next(111111111, 999999999)}</ICQ>" + Environment.NewLine + 
                        "    <City>" + array[1] + "</City>" + Environment.NewLine + 
                        "    <Country>" + array[0] + "</Country>" + Environment.NewLine + 
                        "    <Occupation>" + GenerateOccupation() + "</Occupation>" + Environment.NewLine + 
                        "    <Interests>" + GenerateInterests() + "</Interests>" + Environment.NewLine + 
                        "    <Signature>#file_links_A[\"" + text5 + "\",1,N]</Signature>" + Environment.NewLine + 
                        "    <Gender>0</Gender>" + Environment.NewLine + 
                        "    <UnknownFields></UnknownFields>" + Environment.NewLine + 
                        "    <PollTitle></PollTitle>" + Environment.NewLine + 
                        "    <PollOption1></PollOption1>" + Environment.NewLine + 
                        "    <PollOption2></PollOption2>" + Environment.NewLine + 
                        "    <PollOption3></PollOption3>" + Environment.NewLine + 
                        "    <PollOption4></PollOption4>" + Environment.NewLine + 
                        "    <PollOption5></PollOption5>" + Environment.NewLine + 
                        "  </PrimarySection>\r\n" +
                        "  <SecondarySection>\r\n" +
                        "    <Subject1>#file_links_A[\"C:\\Temp\\" + ProjectName + "_Titles.txt\",1,N]</Subject1>\r\n" +
                        "    <Subject2>#file_links_A[\"C:\\Temp\\" + ProjectName + "_Titles.txt\",1,N]</Subject2>\r\n" +
                        "    <PostText>#file_links_A[\"C:\\Temp\\" + ProjectName + "_Texts.txt\",1,N]</PostText>\r\n" +
                        "    <Prior>бизнес\r\n" +
                        "досуг\r\n" +
                        "объяв\r\n" +
                        "курилка\r\n" +
                        "флейм\r\n" +
                        "флэйм\r\n" +
                        "основн\r\n" +
                        "развлеч\r\n" +
                        "оффтопик\r\n" +
                        "офтопик\r\n" +
                        "офф-топик\r\n" +
                        "прочее\r\n" +
                        "разное\r\n" +
                        "обо всём\r\n" +
                        "flood\r\n" +
                        "flame\r\n" +
                        "stuff\r\n" +
                        "blah\r\n" +
                        "off-topic\r\n" +
                        "off topic\r\n" +
                        "offtopic\r\n" +
                        "oftopic\r\n" +
                        "general\r\n" +
                        "common\r\n" +
                        "business\r\n" +
                        "обща\r\n" +
                        "общий\r\n" +
                        "общее\r\n" +
                        "общие\r\n" +
                        "реклам\r\n" +
                        "adver</Prior>\r\n" +
                        "    <OnlyPriors>false</OnlyPriors>\r\n" +
                        "  </SecondarySection>\r\n" +
                        "</XRumerProject>";
                    if (Regex.IsMatch(array2.First(), "http://.+"))
                    {
                        Task<List<string[]>> task = WebParser.AccessTheWebAsync(array2.ToList(), num2, _threads, backgroundWorker);
                        task.Wait();
                        List<string[]> result = task.Result;
                        num2 = (_totalRows = num2 + array2.Length);
                        backgroundWorker.ReportProgress(num2);
                        if (result != null && result.Count > 0)
                        {
                            string contents2 = string.Join(Environment.NewLine, from l in result
                                                                                select l[0]);
                            string contents3 = string.Join(Environment.NewLine, from l in result
                                                                                select l[1]);
                            string contents4 = string.Join(Environment.NewLine, result?.Select(PerformText));
                            File.WriteAllText("C:\\Temp\\" + ProjectName + "_URLs.txt", contents2);
                            File.WriteAllText("C:\\Temp\\" + ProjectName + "_Texts.txt", contents4);
                            File.WriteAllText("C:\\Temp\\" + ProjectName + "_Titles.txt", contents3);
                            File.AppendAllText(GetUsedEmailsFilePath(_emailsFilePath), text3 + Environment.NewLine);
                            File.WriteAllText(path, contents);
                        }
                    }
                    else
                    {
                        try
                        {
                            JArray jArray;
                            if ((jArray = (JsonConvert.DeserializeObject(array2.First()) as JArray)) != null)
                            {
                                List<string> values = (from x in jArray.Children()
                                                       select ((string)x["link"]).Replace("\n", "").Replace("\r", "")).ToList();
                                List<string> values2 = (from x in jArray.Children()
                                                        select ((string)x["title"]).Replace("\n", "").Replace("\r", "")).ToList();
                                List<string> values3 = (from x in jArray.Children()
                                                        select PerformText(new string[2]
                                                        {
                                ((string)x["link"]).Replace("\n", "").Replace("\r", ""),
                                ((string)x["description"]).Replace("\n", "").Replace("\r", "")
                                                        })).ToList();
                                File.WriteAllText("C:\\Temp\\" + ProjectName + "_URLs.txt", string.Join(Environment.NewLine, values));
                                using (StreamWriter streamWriter = new StreamWriter(File.Open("C:\\Temp\\" + ProjectName + "_Texts.txt", FileMode.Create), Encoding.GetEncoding("Windows-1251")))
                                {
                                    streamWriter.Write(string.Join(Environment.NewLine, values3));
                                }
                                using (StreamWriter streamWriter2 = new StreamWriter(File.Open("C:\\Temp\\" + ProjectName + "_Titles.txt", FileMode.Create), Encoding.GetEncoding("Windows-1251")))
                                {
                                    streamWriter2.Write(string.Join(Environment.NewLine, values2));
                                }
                                File.AppendAllText(GetUsedEmailsFilePath(_emailsFilePath), text3 + Environment.NewLine);
                                File.WriteAllText(path, contents);
                                num2++;
                                _totalRows += jArray.Count;
                                backgroundWorker.ReportProgress(num2);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show("Fail to deserialize content in file `" + linksFilePath + "` as json " + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                    }
                    num++;
                }
            }
        }

        private static string[][] GetValidMails(string path)
        {
            string usedEmailsFilePath = GetUsedEmailsFilePath(path);
            if (!File.Exists(usedEmailsFilePath))
            {
                File.WriteAllText(usedEmailsFilePath, "");
            }
            string[] usedEmails = (from l in File.ReadAllLines(usedEmailsFilePath)
                                   select l.Split(';') into x
                                   select x[0]).ToArray();
            return (from l in File.ReadAllLines(path)
                    select l.Split(';') into mail
                    where !usedEmails.Contains(mail[0])
                    select mail).ToArray();
        }

        private static string GetUsedEmailsFilePath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_used.txt");
        }

        private static string PerformText(string[] input)
        {
            string text = input[0].Replace(Environment.NewLine, "");
            string text2 = input[1].Replace(Environment.NewLine, "");
            return text2 + "   [url=" + text + "]{" + text2 + "|Click here|More info|Show more}{!|...|>>>|!..}[/url]";
        }

        private string SetEmailPop(string email)
        {
            string text = email.Substring(email.IndexOf("@", StringComparison.Ordinal));
            switch (text)
            {
                case "mail.ru":
                    return "pop.mail.ru";
                case "yandex.ru":
                    return "pop.yandex.ru";
                case "gmail.com":
                    return "pop.gmail.com";
                default:
                    return "pop.mail.ru";
            }
        }

        private string[] GenerateCountryCity()
        {
            string[] array = File.ReadAllLines(Path.Combine(_xrumerPath, "ProjectFill", "cities.txt"));
            string text = array[_rnd.Next(0, array.Length - 1)];
            string text2 = text.Split('|').First();
            string[] array2 = text.Split('|').Skip(1).First()
                .Split(',');
            string text3 = array2[_rnd.Next(0, array2.Length - 1)];
            return new string[2]
            {
            text2,
            text3
            };
        }

        private string GenerateOccupation()
        {
            string[] array = File.ReadAllLines(Path.Combine(_xrumerPath, "ProjectFill", "occupation.rus.txt"), Encoding.GetEncoding("Windows-1251"));
            return array[_rnd.Next(0, array.Length - 1)];
        }

        private string GenerateInterests()
        {
            IEnumerable<string> values = (from x in File.ReadAllLines(Path.Combine(_xrumerPath, "ProjectFill", "interest.rus.txt"), Encoding.GetEncoding("Windows-1251")).ToList()
                                          orderby _rnd.Next()
                                          select x).Take(3);
            return string.Join(", ", values);
        }

        private void LinksTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                switch (folderBrowserDialog.ShowDialog())
                {
                    case System.Windows.Forms.DialogResult.None:
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        break;
                    case System.Windows.Forms.DialogResult.Abort:
                        break;
                    case System.Windows.Forms.DialogResult.Retry:
                        break;
                    case System.Windows.Forms.DialogResult.Ignore:
                        break;
                    case System.Windows.Forms.DialogResult.Yes:
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    case System.Windows.Forms.DialogResult.OK:
                        _linksDir = folderBrowserDialog.SelectedPath;
                        Settings.Default.LinksDir = _linksDir;
                        LinksTextBox.Text = folderBrowserDialog.SelectedPath;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void EmailsTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                switch (openFileDialog.ShowDialog())
                {
                    case System.Windows.Forms.DialogResult.None:
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        break;
                    case System.Windows.Forms.DialogResult.Abort:
                        break;
                    case System.Windows.Forms.DialogResult.Retry:
                        break;
                    case System.Windows.Forms.DialogResult.Ignore:
                        break;
                    case System.Windows.Forms.DialogResult.Yes:
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    case System.Windows.Forms.DialogResult.OK:
                        _emailsPath = openFileDialog.FileName;
                        Settings.Default.EmailsPath = _emailsPath;
                        EmailsTextBox.Text = openFileDialog.FileName;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OutputTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                switch (folderBrowserDialog.ShowDialog())
                {
                    case System.Windows.Forms.DialogResult.None:
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        break;
                    case System.Windows.Forms.DialogResult.Abort:
                        break;
                    case System.Windows.Forms.DialogResult.Retry:
                        break;
                    case System.Windows.Forms.DialogResult.Ignore:
                        break;
                    case System.Windows.Forms.DialogResult.Yes:
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    case System.Windows.Forms.DialogResult.OK:
                        Settings.Default.OutputDir = folderBrowserDialog.SelectedPath;
                        OutputTextBox.Text = folderBrowserDialog.SelectedPath;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void EmailsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _emailsFilePath = EmailsTextBox.Text;
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProcessedLinesProgressBar.Dispatcher.BeginInvoke((Action)delegate
            {
                ProcessedLinesProgressBar.Value = (double)e.ProgressPercentage;
            });
            GenerateProjectsButton.Dispatcher.BeginInvoke((Action)delegate
            {
                GenerateProjectsButton.Content = e.ProgressPercentage + "/" + ProcessedLinesProgressBar.Maximum + " Generated";
            });
        }

        private void XrumerPathTexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _xrumerPath = XrumerPathTexBox.Text;
            Settings.Default.XrumerPath = XrumerPathTexBox.Text;
        }

        private void OutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _outputPath = OutputTextBox.Text;
        }

        private void GenerateSchedule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sw.Reset();
                _sw.Start();
                string[] files = Directory.GetFiles(_outputPath, "*.xml", SearchOption.TopDirectoryOnly);
                string str = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<body>\r\n";
                int num = 0;
                string[] array = files;
                var pause = Settings.Default.NextProjectPause;
                var waitEvent = Settings.Default.NoWaitForThreads ? "6" : "5"; 
                var waitParam = Settings.Default.NoWaitForThreads ? "" : "0";
                foreach (string text in array)
                {
                    string text2 = Path.GetFileNameWithoutExtension(text)?.Replace('.', '_');
                    File.Copy(text, Path.Combine(_xrumerPath, "Projects", text2 + ".xml"), overwrite: true);
                    string str2 = $"  <Schedule{num}>\r\n" +
                        $"    <PerformedTime></PerformedTime>\r\n" +
                        $"    <EventNum>{waitEvent}</EventNum>\r\n" +
                        $"    <EventParameter>{waitParam}</EventParameter>\r\n" +
                        $"    <JobNum>4</JobNum>\r\n" +
                        $"    <JobParameter>{text2}</JobParameter>\r\n" +
                        $"  </Schedule{num++}>\r\n" +
                        $"  <Schedule{num}>\r\n" +
                        $"    <PerformedTime></PerformedTime>\r\n" +
                        $"    <EventNum>6</EventNum>\r\n" +
                        $"    <EventParameter></EventParameter>\r\n" +
                        $"    <JobNum>24</JobNum>\r\n" +
                        $"    <JobParameter>00:01:00</JobParameter>\r\n" +
                        $"  </Schedule{num++}>\r\n" +
                        $"  <Schedule{num}>\r\n" +
                        $"    <PerformedTime></PerformedTime>\r\n" +
                        $"    <EventNum>6</EventNum>\r\n" +
                        $"    <EventParameter></EventParameter>\r\n" +
                        $"    <JobNum>1</JobNum>\r\n" +
                        $"    <JobParameter></JobParameter>\r\n" +
                        $"  </Schedule{num++}>\r\n" +
                        $"  <Schedule{num}>\r\n" +
                        $"    <PerformedTime></PerformedTime>\r\n" +
                        $"    <EventNum>0</EventNum>\r\n" +
                        $"    <EventParameter></EventParameter>\r\n" +
                        $"    <JobNum>24</JobNum>\r\n" +
                       $@"    <JobParameter>{pause:hh\:mm\:ss}</JobParameter>\r\n" +
                        $"  </Schedule{num++}>";
                    str += str2;
                }
                _sw.Stop();
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($@"Job's done in {_sw.Elapsed:hh\:mm\:ss\.fff}" + Environment.NewLine + $"Projects scheduled: <{files.Count()}>" + Environment.NewLine + "Add loop from the beginnig?", "Finished", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    str += $"\r\n  <Schedule{num}>\r\n    <PerformedTime></PerformedTime>\r\n    <EventNum>5</EventNum>\r\n    <EventParameter>0</EventParameter>\r\n    <JobNum>23</JobNum>\r\n    <JobParameter>0</JobParameter>\r\n  </Schedule{num++}>";
                }
                str += "\r\n</body>";
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = "xml",
                    AddExtension = true,
                    CustomPlaces = new List<Microsoft.Win32.FileDialogCustomPlace>
                {
                    new Microsoft.Win32.FileDialogCustomPlace(Path.Combine(_xrumerPath, "Schedules"))
                }
                };
                if (saveFileDialog.ShowDialog() ?? false)
                {
                    File.WriteAllText(saveFileDialog.FileName, str);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void ThreadsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _threads = int.Parse(ThreadsBox.Text);
            }
            catch
            {
                ThreadsBox.Text = "100";
                _threads = 100;
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _sw.Stop();
            base.Dispatcher.BeginInvoke((Action)delegate
            {
                GenerateProjectsButton.IsEnabled = true;
                GenerateProjectsButton.Content = "Generate projects";
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $@"Job's done in {_sw.Elapsed:hh\:mm\:ss\.fff}" + Environment.NewLine + 
                    $"Files generated: <{_linksFilePaths.Count}>" + Environment.NewLine +
                    $"Total rows: <{_totalRows}>");
                if (messageBoxResult == MessageBoxResult.OK)
                {
                    ProcessedLinesProgressBar.Value = 0.0;
                }
            });
            backgroundWorker.DoWork -= Generating;
        }

        private void ExitMenuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!IsWindowOpen<PropertiesWindow>())
            {
                PropertiesWindow _pw = new PropertiesWindow();
                _pw.ShowDialog();
            }
           
        }




        /*
[DebuggerNonUserCode]
[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
public void InitializeComponent()
{
   if (!_contentLoaded)
   {
       _contentLoaded = true;
       Uri resourceLocator = new Uri("/xProjectGenerator;component/mainwindow.xaml", UriKind.Relative);
       System.Windows.Application.LoadComponent(this, resourceLocator);
   }
}

[DebuggerNonUserCode]
[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
[EditorBrowsable(EditorBrowsableState.Never)]
void IComponentConnector.Connect(int connectionId, object target)
{
   switch (connectionId)
   {
       case 1:
           ((MainWindow)target).Closing += Window_Closing;
           break;
       case 2:
           ((BackgroundWorker)target).DoWork += backgroundWorker_DoWork;
           ((BackgroundWorker)target).ProgressChanged += backgroundWorker_ProgressChanged;
           ((BackgroundWorker)target).RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
           break;
       case 3:
           OutputTextBox = (System.Windows.Controls.TextBox)target;
           OutputTextBox.MouseDoubleClick += OutputTextBox_MouseDoubleClick;
           OutputTextBox.TextChanged += OutputTextBox_TextChanged;
           break;
       case 4:
           LinksTextBox = (System.Windows.Controls.TextBox)target;
           LinksTextBox.TextChanged += LinksTextBox_TextChanged;
           LinksTextBox.MouseDoubleClick += LinksTextBox_MouseDoubleClick;
           break;
       case 5:
           label = (System.Windows.Controls.Label)target;
           break;
       case 6:
           label_Copy = (System.Windows.Controls.Label)target;
           break;
       case 7:
           EmailsTextBox = (System.Windows.Controls.TextBox)target;
           EmailsTextBox.TextChanged += EmailsTextBox_TextChanged;
           EmailsTextBox.MouseDoubleClick += EmailsTextBox_MouseDoubleClick;
           break;
       case 8:
           label_Copy1 = (System.Windows.Controls.Label)target;
           break;
       case 9:
           GenerateSchedule_Button = (System.Windows.Controls.Button)target;
           GenerateSchedule_Button.Click += GenerateSchedule_Click;
           break;
       case 10:
           XrumerPathTexBox = (System.Windows.Controls.TextBox)target;
           XrumerPathTexBox.MouseDoubleClick += XrumerPathTexBox_MouseDoubleClick;
           XrumerPathTexBox.TextChanged += XrumerPathTexBox_TextChanged;
           break;
       case 11:
           label_Copy2 = (System.Windows.Controls.Label)target;
           break;
       case 12:
           ProcessedLinesProgressBar = (System.Windows.Controls.ProgressBar)target;
           break;
       case 13:
           GenerateProjectsButton = (System.Windows.Controls.Button)target;
           GenerateProjectsButton.Click += GenerateButton_Click;
           break;
       case 14:
           ThreadsBox = (System.Windows.Controls.TextBox)target;
           ThreadsBox.TextChanged += ThreadsBox_TextChanged;
           break;
       case 15:
           ((System.Windows.Controls.MenuItem)target).Click += ExitMenuItemClick;
           break;
       default:
           _contentLoaded = true;
           break;
   }
}
*/
    }

}
