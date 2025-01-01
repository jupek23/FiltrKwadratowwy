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
        public static extern int ApplyASMFilter(int[] image, int width, int height);

        [DllImport("/../../../../x64/Debug/CPPDll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyCFilter(int[] image, int width, int height);

        private string selectedFilePath;
        private int[] imagePixels;
        private int imageWidth;
        private int imageHeight;

        public MainWindow()
        {
            InitializeComponent();
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
                selectedFilePath = openFileDialog.FileName;

                try
                {
                    LoadToGui(selectedFilePath);
                    ConvertToBitMap(selectedFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas ładowania obrazu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadToGui(string path)
        {
            DisplayImage.Source = new BitmapImage(new Uri(path));
        }

        private void ConvertToBitMap(string path)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri(path));

            imageWidth = bitmapImage.PixelWidth;
            imageHeight = bitmapImage.PixelHeight;

            // Calculate stride and allocate pixel buffer
            int stride = imageWidth * ((bitmapImage.Format.BitsPerPixel + 7) / 8);
            byte[] pixelData = new byte[imageHeight * stride];

            // Extract pixel data
            bitmapImage.CopyPixels(pixelData, stride, 0);

            // Convert byte array (BGRA format) to int array (ARGB format)
            imagePixels = new int[imageWidth * imageHeight];
            for (int i = 0, j = 0; i < pixelData.Length; i += 4, j++)
            {
                int blue = pixelData[i];
                int green = pixelData[i + 1];
                int red = pixelData[i + 2];
                int alpha = pixelData[i + 3];
                imagePixels[j] = (alpha << 24) | (red << 16) | (green << 8) | blue;
            }

            Debug.WriteLine($"Image loaded: {imageWidth}x{imageHeight}, Pixels extracted: {imagePixels.Length}");
        }

        private void convertToImage() {

            if (imagePixels == null)
            {
                MessageBox.Show("Brak danych pikseli do konwersji!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                byte[] pixelData = new byte[imageWidth * imageHeight * 4];
                for (int i = 0, j = 0; i < imagePixels.Length; i++, j += 4)
                {
                    int pixel = imagePixels[i];
                    pixelData[j] = (byte)(pixel & 0xFF);         // Blue
                    pixelData[j + 1] = (byte)((pixel >> 8) & 0xFF); // Green
                    pixelData[j + 2] = (byte)((pixel >> 16) & 0xFF); // Red
                    pixelData[j + 3] = (byte)((pixel >> 24) & 0xFF); // Alpha
                }

                WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgra32, null);
                bitmap.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), pixelData, imageWidth * 4, 0);
                DisplayImage.Source = bitmap;
                Debug.WriteLine("Processed image displayed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas konwersji obrazu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void cButton(object sender, RoutedEventArgs e)
        {
            if (imagePixels == null)
            {
                MessageBox.Show("Najpierw wybierz i załaduj obraz!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int result = ApplyCFilter(imagePixels, imageWidth, imageHeight);
                Debug.WriteLine($"ApplyCFilter result: {imagePixels}");
                Debug.WriteLine($"ApplyCFilter result: {result}");
                convertToImage();
                MessageBox.Show("Filtr C zastosowany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wywołania filtru C: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void asmButton(object sender, RoutedEventArgs e)
        {
            if (imagePixels == null)
            {
                MessageBox.Show("Najpierw wybierz i załaduj obraz!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int result = ApplyASMFilter(imagePixels, imageWidth, imageHeight);
                Debug.WriteLine($"ApplyASMFilter result: {result}");
                MessageBox.Show("Filtr ASM zastosowany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wywołania filtru ASM: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}