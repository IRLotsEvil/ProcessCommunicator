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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using Microsoft.Win32;

namespace communication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ThinServerClient THS { get; set; } = new ThinServerClient();
        public MainWindow()
        {
            InitializeComponent();
            THS.ImageSent += (sender, e)=> 
            {
                e.Image.Save("File_lol.jpg");
                e.RespondString("This is a response string");
            };
            DataContext = this;
            THS.SerializedSent += (sender ,e) => 
            {
                var de = e.DeserializedObject;
            };

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            THS.Stop();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            THS.Start();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var shw = new OpenFileDialog();
            shw.ShowDialog();
            var tsk = ThinServerClient.SendFileAsync(shw.FileName);
            
            tsk.ContinueWith(x =>
            {
                var s = Encoding.Default.GetString(x.Result);
                App.Current.Dispatcher.Invoke(() =>
                {
                    returnmessage.Content = s;
                });
            });
            tsk.Start();
        }
    }
}
