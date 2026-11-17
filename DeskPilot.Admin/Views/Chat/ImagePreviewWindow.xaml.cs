using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DeskPilot.Admin.Views.Chat
{
    public partial class ImagePreviewWindow : Window
    {
        public ImagePreviewWindow(string imageUrl)
        {
            InitializeComponent();
            LoadImage(imageUrl);
        }

        private void LoadImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                bitmap.DownloadCompleted += (s, e) => TxtLoading.Visibility = Visibility.Collapsed;
                PreviewImage.Source = bitmap;

                if (!bitmap.IsDownloading)
                {
                    TxtLoading.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                TxtLoading.Text = "Không thể tải hình ảnh: " + ex.Message;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void ImageCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Bấm vào ảnh không làm đóng modal
        }
    }
}
