using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using MahApps.Metro.Controls;

namespace Toxy
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : MetroWindow
    {
        public VideoWindow()
        {
            InitializeComponent();
        }

        public void PushVideoFrame(Bitmap frame)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                CurrentVideoFrame.Source = BitmapToImageSource(frame, ImageFormat.Bmp);
                frame.Dispose();
            }));
        }

        private BitmapImage BitmapToImageSource(Bitmap frame, ImageFormat format)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                frame.Save(stream, format);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                return bitmap;
            }
        }

        private void CurrentVideoFrame_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            InfoFlyout.IsOpen = !InfoFlyout.IsOpen;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
