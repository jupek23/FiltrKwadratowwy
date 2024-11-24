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
        public static extern int ApplyASMFilter(int[] image);
        public MainWindow()
        {
            InitializeComponent();
        }

        private int[,] algorithm(int[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            int[,] newImage = new int[width, height];

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    int sum = 0;
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            sum += image[i + x, j + y];
                        }
                    }
                    newImage[i, j] = sum / 9;
                }
            }
            return newImage;
        }

        private void cButton(object sender, RoutedEventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int[,] image = new int[3, 3] { { 1, 4, 3 }, { 4, 120, 6 }, { 7, 8, 9 } };
            var result = algorithm(image);
            stopwatch.Stop();

            string resultString = ArrayToString(result);
            MessageBox.Show(resultString, "Filtered Image Data");
        }

        private string ArrayToString(int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(array[i, j] + " ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void asmButton(object sender, RoutedEventArgs e)
        {
            int[,] image = new int[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
            //ApplyASMFilter(image);

        }
    }
}