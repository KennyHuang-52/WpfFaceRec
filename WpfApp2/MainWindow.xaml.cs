// MainWindow.xaml.cs
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace WpfFaceRec
{
    public partial class MainWindow : Window
    {
        private VideoCapture _capture;
        private CascadeClassifier _faceDetector;
        private Thread _cameraThread;
        private bool _running;

        public MainWindow()
        {
            InitializeComponent();

            _capture = new VideoCapture(0);
            _capture.Set(CapProp.Fps, 30);

            string haarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");
            if (!File.Exists(haarPath))
            {
                MessageBox.Show($"找不到分類器檔案: {haarPath}");
                Close();
                return;
            }

            _faceDetector = new CascadeClassifier(haarPath);

            _running = true;
            _cameraThread = new Thread(CameraLoop);
            _cameraThread.Start();
        }

        private void CameraLoop()
        {
            while (_running)
            {
                using (Mat frame = _capture.QueryFrame())
                {
                    if (frame == null) continue;

                    using (Image<Bgr, byte> image = frame.ToImage<Bgr, byte>())
                    {
                        // 自動白平衡：計算 R/G/B 通道平均值
                        MCvScalar mean = CvInvoke.Mean(image);
                        double avgB = mean.V0;
                        double avgG = mean.V1;
                        double avgR = mean.V2;

                        double gainB = avgG / avgB;
                        double gainG = 1.0;
                        double gainR = avgG / avgR;

                        // 根據滑桿強度調整
                        double strength = 1.0;
                        Dispatcher.Invoke(() => strength = AWBGainSlider.Value);

                        gainB = 1.0 + (gainB - 1.0) * strength;
                        gainG = 1.0;
                        gainR = 1.0 + (gainR - 1.0) * strength;

                        // 分別對通道乘上增益
                        using (var channelB = image[0].Mul(gainB))
                        using (var channelG = image[1].Mul(gainG))
                        using (var channelR = image[2].Mul(gainR))
                        using (var merged = new VectorOfMat())
                        {
                            merged.Push(channelB.Mat);
                            merged.Push(channelG.Mat);
                            merged.Push(channelR.Mat);
                            CvInvoke.Merge(merged, image);
                        }

                        // 人臉偵測
                        using (Image<Gray, byte> gray = image.Convert<Gray, byte>())
                        {
                            Rectangle[] faces = _faceDetector.DetectMultiScale(
                                gray, 1.1, 5, Size.Empty);

                            foreach (var face in faces)
                            {
                                image.Draw(face, new Bgr(System.Drawing.Color.Red), 2);
                            }
                        }

                        Dispatcher.Invoke(() =>
                        {
                            CameraImage.Source = BitmapSourceConvert.ToBitmapSource(image);
                        });
                    }
                }

                Thread.Sleep(30);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _running = false;
            _cameraThread?.Join();
            _capture?.Dispose();
            _faceDetector?.Dispose();
        }
    }

    public static class BitmapSourceConvert
    {
        public static BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            using (Bitmap bitmap = image.ToBitmap())
            {
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    bitmap.PixelFormat);

                BitmapSource bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgr24, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                bitmap.UnlockBits(bitmapData);
                return bitmapSource;
            }
        }
    }
}
