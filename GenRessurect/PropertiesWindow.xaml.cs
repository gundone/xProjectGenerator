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
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GenRessurect.Properties;

namespace GenRessurect
{
    /// <summary>
    /// Логика взаимодействия для PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow()
        {
            InitializeComponent();
        }

        private void PropertiesWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadWaitingCheckBox.IsChecked = Settings.Default.NoWaitForThreads;
            NextProjectPauseTextBox.Text = Settings.Default.NextProjectPause.TotalSeconds.ToString();
        }
        private void NextProjectPause_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); 
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void PropertiesWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NextProjectPause_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            Settings.Default.NextProjectPause = TimeSpan.FromSeconds(int.Parse(text));
            Settings.Default.Save();
        }

        private void ThreadWaitingCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.NoWaitForThreads = ThreadWaitingCheckBox.IsChecked ?? false;
            Settings.Default.Save();
        }
    }
}
