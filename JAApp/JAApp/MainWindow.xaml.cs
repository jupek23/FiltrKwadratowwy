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
using Microsoft.Win32;

namespace JAApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("/../../../../x64Debug/JADll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyASMFilter(int[,] image);
        [DllImport("/../../../../x64/Debug/CPPDll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyCFilter(int a, int b);
        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine(ApplyCFilter(2,2));
        }
        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz obraz",
                Filter = "Pliki graficzne (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp|Wszystkie pliki (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                // Załaduj obraz i wyświetl w GUI
                LoadToGui(selectedFilePath);
            }
        }

        private void LoadToGui(string path)
        {
                DisplayImage.Source = new BitmapImage(new Uri(path)); 
        }

        private void pngToBitMap()
        {

        }


        private void cButton(object sender, RoutedEventArgs e)
        {
            ApplyCFilter(2, 2);
        }


        private void asmButton(object sender, RoutedEventArgs e)
        {
            int[,] image = new int[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            ApplyASMFilter(image);

        }
    }
}