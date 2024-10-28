using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JAApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("/../../../../x64Debug/JADll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int MyProc1(int a, int b);
        private BitmapImage image;
        public MainWindow()
        {
            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(/*Tu sciezka*/"C:/Users/matis/Desktop/Kot1.jpg", UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();


            InitializeComponent();
            int a = 10, b = 10;
            int res = MyProc1(a, b);

            Debug.WriteLine("Result: " + res);
            //Console.WriteLine("Result: " + res);

            Kamil.Text = "Result: " + res;


        
            }
    }
}