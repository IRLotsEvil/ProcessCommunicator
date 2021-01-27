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

namespace communication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var PS = new ThinServerClient("127.0.0.1", 8383);
            PS.ImageSent += (sender, e)=> 
            {
                e.Image.Save("File_lol.jpg");
                e.RespondString("This is a response string");
            };
            PS.FileSent += (sender, e)=> 
            {
                
            };
        }
    }
}
