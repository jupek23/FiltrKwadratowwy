// Temat projektu: Przetwarzanie obrazów w aplikacji WPF z wykorzystaniem algorytmów w C++ i ASM
// Opis algorytmu: Interfejs aplikacji, który pozwala na wybór obrazu, zastosowanie filtra 5x5 oraz zapisanie wynikowego obrazu.
// Data wykonania projektu: Semestr Zimowy 2024/2025
// Autor: Mateusz Skrzypiec

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Shapes;

namespace JAApp
{
    public partial class MainWindow : Window
    {
        // Importowanie funkcji DLL z użyciem IntPtr
        [DllImport("JADll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyASMFilter(IntPtr pixelData, int width, int startY, int endY, int imageHeight);

        [DllImport("CPPDll.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int ApplyCFilter(IntPtr pixelData, int width, int startY, int endY, int imageHeight);

        private string selectedFilePath;
        private byte[] imagePixels;
        private int imageWidth;
        private int imageHeight;
        private int selectedThreads;
        private bool isAlgorithmApplied = false;


        public MainWindow()
        {
            InitializeComponent();
            InitializeThreadSelection();
        }

        private void InitializeThreadSelection()
        {
            int realThreads = Environment.ProcessorCount;
            selectedThreads = realThreads;

            ThreadsComboBox.ItemsSource = Enumerable.Range(1, 64).ToList();
            ThreadsComboBox.SelectedItem = realThreads;
            ThreadsComboBox.SelectionChanged += (s, e) =>
            {
                if (ThreadsComboBox.SelectedItem is int selected)
                {
                    selectedThreads = selected;
                    Debug.WriteLine($"Selected Threads: {selectedThreads}");
                }
            };
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz obraz",
                Filter = "Pliki graficzne (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;

                try
                {
                    LoadToGui(selectedFilePath);
                    ConvertToBitMap(selectedFilePath);
                    DrawHistogram(HistogramCanvas1, CalculateHistogram(imagePixels));
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
                DrawHistogram(HistogramCanvas2, CalculateHistogram(imagePixels));
                Debug.WriteLine("Processed image displayed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas konwersji obrazu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilterWithThreads(Func<IntPtr, int, int, int, int, int> filterFunction)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Najpierw wybierz i załaduj obraz!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Zawsze ładuj dane obrazu z `selectedFilePath`
                ConvertToBitMap(selectedFilePath);

                // Obliczanie optymalnego podziału w pionie
                int baseSegmentHeight = imageHeight / selectedThreads;
                int extraRows = imageHeight % selectedThreads;

                int[] startYs = new int[selectedThreads];
                int[] endYs = new int[selectedThreads];

                // Obliczanie startY i endY dla każdego wątku
                int currentStartY = 0;
                for (int i = 0; i < selectedThreads; i++)
                {
                    int segmentHeight = baseSegmentHeight + (i < extraRows ? 1 : 0);
                    startYs[i] = currentStartY;
                    endYs[i] = currentStartY + segmentHeight;
                    currentStartY += segmentHeight;
                }

                // Przypięcie tablicy imagePixels do pamięci, by uniknąć problemów z GC
                GCHandle handle = GCHandle.Alloc(imagePixels, GCHandleType.Pinned);
                IntPtr pixelDataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(imagePixels, 0);

                Stopwatch stopwatch = Stopwatch.StartNew();
                Parallel.For(0, selectedThreads, threadIndex =>
                {
                    int startY = startYs[threadIndex];
                    int endY = endYs[threadIndex];

                    // Przetwarzanie wybranego zakresu
                    filterFunction(pixelDataPtr, imageWidth, startY, endY, imageHeight);
                });

                stopwatch.Stop();
                handle.Free();

                ConvertToImage();
                isAlgorithmApplied = true; // Ustaw flagę na true po poprawnym przetworzeniu
                MessageBox.Show($"Filtr został zastosowany w {stopwatch.ElapsedMilliseconds} ms", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wywołania filtru: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void cButton(object sender, RoutedEventArgs e)
        {
            ApplyFilterWithThreads(ApplyCFilter);
        }

        private void asmButton(object sender, RoutedEventArgs e)
        {
            ApplyFilterWithThreads(ApplyASMFilter);
        }

        private int[][] CalculateHistogram(byte[] pixels)
        {
            int[][] histogram = new int[3][];
            histogram[0] = new int[256]; // Blue
            histogram[1] = new int[256]; // Green
            histogram[2] = new int[256]; // Red

            for (int i = 0; i < pixels.Length; i += 3)
            {
                histogram[0][pixels[i]]++;       // Blue
                histogram[1][pixels[i + 1]]++;  // Green
                histogram[2][pixels[i + 2]]++;  // Red
            }

            return histogram;
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isAlgorithmApplied)
            {
                MessageBox.Show("Najpierw zastosuj algorytm przed zapisaniem obrazu!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (imagePixels == null || string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Najpierw przetwórz obraz przed zapisem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Zapisz obraz",
                Filter = "Pliki BMP (*.bmp)|*.bmp|Pliki JPEG (*.jpg)|*.jpg|Pliki PNG (*.png)|*.png",
                FileName = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath) + "_processed"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string savePath = saveFileDialog.FileName;

                // Sprawdzenie, czy użytkownik próbuje nadpisać plik źródłowy
                if (string.Equals(savePath, selectedFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Nie można nadpisać oryginalnego obrazu w użyciu!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string extension = System.IO.Path.GetExtension(savePath).ToLower();

                // Sprawdzenie, czy wybrano poprawny format
                if (extension != ".bmp" && extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                {
                    MessageBox.Show("Nieobsługiwany format pliku!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Zapis obrazu z odpowiednim formatem
                    SaveImage(savePath, extension.TrimStart('.'));
                    MessageBox.Show($"Obraz zapisany pomyślnie: {savePath}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania obrazu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }





        private void SaveImage(string filePath, string format)
        {
            if (imagePixels == null)
            {
                throw new InvalidOperationException("Brak danych pikseli do zapisania.");
            }

            // Tworzenie WriteableBitmap z przetworzonych pikseli
            WriteableBitmap bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
            bitmap.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), imagePixels, imageWidth * 3, 0);

            // Wybór odpowiedniego enkodera na podstawie formatu
            BitmapEncoder encoder;
            switch (format.ToLower())
            {
                case "bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case "jpg":
                case "jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case "png":
                    encoder = new PngBitmapEncoder();
                    break;
                default:
                    throw new ArgumentException("Nieobsługiwany format zapisu: " + format);
            }

            // Zapis obrazu do pliku
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }



        private void DrawHistogram(Canvas canvas, int[][] histogram)
        {
            canvas.Children.Clear();

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0)
                return;

            // Obliczanie maksymalnej wartości w histogramie
            int maxCount = Math.Max(histogram[0].Max(), Math.Max(histogram[1].Max(), histogram[2].Max()));

            // Obliczanie szerokości pojedynczego słupka
            double barWidth = Math.Max(canvasWidth / (256 * 3), 1); // Podział na RGB (3 słupki per poziom jasności)
            double scale = canvasHeight / maxCount;

            // Tworzenie słupków histogramu
            for (int i = 0; i < 256; i++)
            {
                // Blue bar
                Rectangle blueRect = new Rectangle
                {
                    Width = barWidth,
                    Height = histogram[0][i] * scale,
                    Fill = Brushes.Blue
                };
                Canvas.SetLeft(blueRect, i * 3 * barWidth);
                Canvas.SetBottom(blueRect, 0);
                canvas.Children.Add(blueRect);

                // Green bar
                Rectangle greenRect = new Rectangle
                {
                    Width = barWidth,
                    Height = histogram[1][i] * scale,
                    Fill = Brushes.Green
                };
                Canvas.SetLeft(greenRect, i * 3 * barWidth + barWidth);
                Canvas.SetBottom(greenRect, 0);
                canvas.Children.Add(greenRect);

                // Red bar
                Rectangle redRect = new Rectangle
                {
                    Width = barWidth,
                    Height = histogram[2][i] * scale,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(redRect, i * 3 * barWidth + 2 * barWidth);
                Canvas.SetBottom(redRect, 0);
                canvas.Children.Add(redRect);
            }
        }


    }
}
