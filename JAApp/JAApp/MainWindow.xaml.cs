﻿using System;
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
                Filter = "Pliki graficzne (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp|Wszystkie pliki (*.*)|*.*"
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
            if (imagePixels == null)
            {
                MessageBox.Show("Najpierw wybierz i załaduj obraz!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                int rowsPerThread = imageHeight / selectedThreads;

                Parallel.For(0, selectedThreads, threadIndex =>
                {
                    // Obliczanie zakresów z uwzględnieniem buforów
                    int startY = threadIndex * rowsPerThread;
                    int endY = (threadIndex == selectedThreads - 1) ? imageHeight : (threadIndex + 1) * rowsPerThread;

                    // Dodaj bufor tylko jeśli zakres nie przekracza obrazu
                    int processStartY = Math.Max(0, startY - 2);
                    int processEndY = Math.Min(imageHeight, endY + 2);

                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(imagePixels.Length);
                    Marshal.Copy(imagePixels, 0, unmanagedPointer, imagePixels.Length);

                    // Przetwarzanie wybranego zakresu
                    filterFunction(unmanagedPointer, imageWidth, processStartY, processEndY, imageHeight);

                    // Skopiowanie tylko właściwego zakresu (bez buforów)
                    int validBytes = (endY - startY) * imageWidth * 3;
                    Marshal.Copy(unmanagedPointer + (startY * imageWidth * 3), imagePixels, startY * imageWidth * 3, validBytes);

                    Marshal.FreeHGlobal(unmanagedPointer);
                });

                stopwatch.Stop();
                ConvertToImage();
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

        private void DrawHistogram(Canvas canvas, int[][] histogram)
        {
            canvas.Children.Clear();

            int maxCount = Math.Max(histogram[0].Max(), Math.Max(histogram[1].Max(), histogram[2].Max()));
            double scale = canvas.Height / maxCount;

            for (int i = 0; i < 256; i++)
            {
                Rectangle blueRect = new Rectangle
                {
                    Width = 1,
                    Height = histogram[0][i] * scale,
                    Fill = Brushes.Blue
                };
                Canvas.SetLeft(blueRect, i * 2);
                Canvas.SetBottom(blueRect, 0);
                canvas.Children.Add(blueRect);

                Rectangle greenRect = new Rectangle
                {
                    Width = 1,
                    Height = histogram[1][i] * scale,
                    Fill = Brushes.Green
                };
                Canvas.SetLeft(greenRect, i * 2);
                Canvas.SetBottom(greenRect, 0);
                canvas.Children.Add(greenRect);

                Rectangle redRect = new Rectangle
                {
                    Width = 1,
                    Height = histogram[2][i] * scale,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(redRect, i * 2);
                Canvas.SetBottom(redRect, 0);
                canvas.Children.Add(redRect);
            }
        }
    }
}
