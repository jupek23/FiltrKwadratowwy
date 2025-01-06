using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace JAApp
{
    public partial class MainWindow : Window
    {
        // Importowanie funkcji DLL z użyciem IntPtr
        [DllImport("JADll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyASMFilter(IntPtr pixelData, int width, int startY, int endY, int imageHeight);

        [DllImport("CPPDll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern ulong ApplyCFilter(IntPtr pixelData, int width, int startY, int endY, int imageHeight);

        private string selectedFilePath;
        private byte[] imagePixels;
        private int imageWidth;
        private int imageHeight;
        private int selectedThreads = 1;

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
            DisplayImage1.Source = new BitmapImage(new Uri(path));
        }

        private void ConvertToBitMap(string path)
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri(path));

            imageWidth = bitmapImage.PixelWidth;
            imageHeight = bitmapImage.PixelHeight;

            // Calculate stride and allocate pixel buffer
            int stride = imageWidth * 3; // BGR (3 bajty na piksel)
            imagePixels = new byte[imageHeight * stride];

            // Extract pixel data
            FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Rgb24, null, 0);
            formattedBitmap.CopyPixels(imagePixels, stride, 0);

            Debug.WriteLine($"Image loaded: {imageWidth}x{imageHeight}, Pixels extracted: {imagePixels.Length}");
        }

        private void ConvertToImage()
        {
            if (imagePixels == null)
            {
                MessageBox.Show("Brak danych pikseli do konwersji!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
                bitmap.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), imagePixels, imageWidth * 3, 0);
                DisplayImage2.Source = bitmap;
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
                // Start pomiaru czasu
                Stopwatch stopwatch = Stopwatch.StartNew();

                // Podziel obraz na części i przetwarzaj równolegle
                int numberOfThreads = selectedThreads; // Liczba wątków
                int rowsPerThread = imageHeight / numberOfThreads;
                Parallel.For(0, numberOfThreads, threadIndex =>
                {
                    int startY = threadIndex * rowsPerThread;
                    int endY = (threadIndex == numberOfThreads - 1) ? imageHeight : startY + rowsPerThread;

                    // Przygotuj dane do wysłania do DLL
                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(imagePixels.Length);
                    Marshal.Copy(imagePixels, 0, unmanagedPointer, imagePixels.Length);

                    // Wywołanie funkcji w DLL
                    ApplyCFilter(unmanagedPointer, imageWidth, startY, endY, imageHeight);

                    // Skopiowanie danych z powrotem do tablicy zarządzanej
                    Marshal.Copy(unmanagedPointer, imagePixels, 0, imagePixels.Length);

                    // Zwolnienie pamięci
                    Marshal.FreeHGlobal(unmanagedPointer);
                });

                // Koniec pomiaru czasu
                stopwatch.Stop();

                // Wyświetlenie przetworzonego obrazu
                ConvertToImage();

                // Wyświetlenie czasu wykonania
                MessageBox.Show($"Filtr C został zastosowany w {stopwatch.ElapsedMilliseconds} ms", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // Start pomiaru czasu
                Stopwatch stopwatch = Stopwatch.StartNew();

                int numberOfThreads = selectedThreads;
                int rowsPerThread = imageHeight / numberOfThreads;

                Parallel.For(0, numberOfThreads, threadIndex =>
                {
                    int startY = threadIndex * rowsPerThread;
                    int endY = (threadIndex == numberOfThreads - 1) ? imageHeight : startY + rowsPerThread;

                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(imagePixels.Length);
                    Marshal.Copy(imagePixels, 0, unmanagedPointer, imagePixels.Length);

                    ApplyASMFilter(unmanagedPointer, imageWidth, startY, endY, imageHeight);

                    Marshal.Copy(unmanagedPointer, imagePixels, 0, imagePixels.Length);
                    Marshal.FreeHGlobal(unmanagedPointer);
                });                
                // Koniec pomiaru czasu
                stopwatch.Stop();

                ConvertToImage();
                MessageBox.Show($"Filtr ASM został zastosowany w {stopwatch.ElapsedMilliseconds} ms", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wywołania filtru ASM: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
