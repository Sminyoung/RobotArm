using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using AForge.Video;
using AForge.Video.DirectShow;
using Intel.RealSense;
using System.Windows.Media;
using System.Threading.Tasks;
using Python.Runtime;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using UI1;

namespace RobotArmUI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<FilterInfo> VideoDevices { get; set; }

        private IVideoSource _videoSource;

        private Pipeline pipeline;
        private Colorizer colorizer;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        static Action<VideoFrame> UpdateImage(System.Windows.Controls.Image img)
        {
            var wbmp = img.Source as WriteableBitmap;
            return new Action<VideoFrame>(frame =>
            {
                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                wbmp.WritePixels(rect, frame.Data, frame.Stride * frame.Height, frame.Stride);
            });
        }

        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; this.OnPropertyChanged("CurrentDevice"); }
        }
        private FilterInfo _currentDevice;


        #region 필터 정의

        public int Threshold
        {
            get { return _threshold; }
            set { _threshold = value; this.OnPropertyChanged("Threshold"); }
        }
        private int _threshold;

        public int Red
        {
            get { return _red; }
            set { _red = value; this.OnPropertyChanged("Red"); }
        }
        private int _red;

        public int Blue
        {
            get { return _blue; }
            set { _blue = value; this.OnPropertyChanged("Blue"); }
        }
        private int _blue;

        public int Green
        {
            get { return _green; }
            set { _green = value; this.OnPropertyChanged("Green"); }
        }
        private int _green;

        public bool ColorFiltered
        {
            get { return _colorFiltered; }
            set { _colorFiltered = value; this.OnPropertyChanged("ColorFiltered"); }
        }
        private bool _colorFiltered;

        public short Radius
        {
            get { return _radius; }
            set { _radius = value; this.OnPropertyChanged("Radius"); }
        }
        private short _radius;

        #endregion


        #region 화면 상 x, y, z 좌표 정의

        public int clickX
        {
            get { return _clickX; }
            set { _clickX = value; this.OnPropertyChanged("clickX"); }
        }
        private int _clickX;

        public int clickY
        {
            get { return _clickY; }
            set { _clickY = value; this.OnPropertyChanged("clickY"); }
        }
        private int _clickY;

        public float clickZ
        {
            get { return _clickZ; }
            set { _clickZ = value; this.OnPropertyChanged("clickZ"); }
        }
        private float _clickZ;

        #endregion


        #region 형상 감지 getter, setter

        public bool ShapeDetection
        {
            get { return _shapeDetection; }
            set { _shapeDetection = value; this.OnPropertyChanged("ShapeDetection"); }
        }
        private bool _shapeDetection;

        public int Circle
        {
            get { return _circle; }
            set { _circle = value; this.OnPropertyChanged("Circle"); }
        }
        private int _circle;

        public int Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; this.OnPropertyChanged("Rectangle"); }
        }
        private int _rectangle;

        public int Triangle
        {
            get { return _triangle; }
            set { _triangle = value; this.OnPropertyChanged("Triangle"); }
        }
        private int _triangle;

        #endregion


        #region 메인

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            GetVideoDevices();
            Threshold = 127;
            Radius = 60;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            StopCamera();
        }

        #endregion


        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartCamera();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }


        #region 마우스 이벤트

        private void videoPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Point clickPos = e.GetPosition((IInputElement)sender);

                clickX = (int)clickPos.X;
                clickY = (int)clickPos.Y;

                using (var frame = pipeline.WaitForFrames())
                using (var depth = frame.DepthFrame)
                    clickZ = ((float)(depth.GetDistance(clickX, clickY) + 0.01) * 100);
            }

            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void videoPlayer_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Cross;
        }

        private void videoPlayer_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        #endregion


        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    if (ColorFiltered)
                    {
                        new EuclideanColorFiltering(new RGB((byte)Red, (byte)Green, (byte)Blue), Radius).ApplyInPlace(bitmap);
                    }

                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        var shape = BlobDetection(bitmap);
                    }));

                    bi = bitmap.ToBitmapImage();
                }
                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate { videoPlayer.Source = bi; }));
            }

            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCamera();
            }
        }


        #region 형상 감지와 그리기

        private Bitmap BlobDetection(Bitmap bitmap)
        {
            var clone = bitmap.Clone() as Bitmap;
            var grayscaledBitmap = Grayscale.CommonAlgorithms.BT709.Apply(clone);
            new Threshold(Threshold).ApplyInPlace(grayscaledBitmap);

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinWidth = 70;
            blobCounter.MinHeight = 70;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
            blobCounter.ProcessImage(grayscaledBitmap);

            Blob[] blobs = blobCounter.GetObjectsInformation();
            Bitmap tmp = new Bitmap(grayscaledBitmap.Width, grayscaledBitmap.Height);
            Graphics g = Graphics.FromImage(tmp);
            g.DrawImage(grayscaledBitmap, new Rectangle(0, 0, tmp.Width, tmp.Height), 0, 0, tmp.Width, tmp.Height, GraphicsUnit.Pixel);
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                int ipenWidth = 5;
                List<IntPoint> c = null;
                float radius;
                AForge.Point center;

                if (shapeChecker.IsQuadrilateral(edgePoints, out c))
                {
                    System.Drawing.Point[] coordinates = ToPointsArray(c);
                    Pen p = new Pen(Color.Blue, ipenWidth);

                    if (coordinates.Length == 4)
                    {
                        if(ShapeDetection)
                            Rectangle += 1;

                        PaintPolygon(c, bitmap, p);
                    }
                }

                if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                {
                    Pen p = new Pen(Color.Red, ipenWidth);

                    if(ShapeDetection)
                        Circle += 1;

                    PaintEllipse(bitmap, p, (float)(center.X - radius), (float)(center.Y - radius), (float)(radius * 2), (float)(radius * 2));
                }

                if (shapeChecker.IsTriangle(edgePoints, out c))
                {
                    System.Drawing.Point[] coordinates = ToPointsArray(c);
                    Pen p = new Pen(Color.Green, ipenWidth);

                    if (coordinates.Length == 3)
                    {
                        if(ShapeDetection)
                            Triangle += 1;

                        PaintPolygon(c, bitmap, p);
                    }
                }
            }
            ShapeDetection = false;

            return bitmap;
        }

        private void PaintPolygon(List<IntPoint>corners, Bitmap bitmap, Pen p)
        {
                Graphics g = Graphics.FromImage(bitmap);
                g.DrawPolygon(p, ToPointsArray(corners));
        }

        private void PaintEllipse(Bitmap bitmap, Pen p, float _x, float _y, float _w, float _h)
        {
            Graphics g = Graphics.FromImage(bitmap);
            g.DrawEllipse(p, _x, _y, _w, _h); ;
        }

        #endregion


        #region Status 출력

        private void statusBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
             클릭하면 메시지박스에 로봇팔로 옮겨진 도형의 현황을 출력
            ex) Circle : 1  Rectangle : 2   Triangle : 1
             */
        }

        #endregion


        #region 카메라 함수

        private void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();

            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
                VideoDevices.Add(filterInfo);

            if (VideoDevices.Any())
                CurrentDevice = VideoDevices[0];

            else
                MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void StartCamera()
        {
            if (CurrentDevice != null)
            {
                _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                _videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                _videoSource.Start();

                Action<VideoFrame> updateDepth;

                colorizer = new Colorizer();
                pipeline = new Pipeline();

                var cfg = new Config();
                cfg.EnableStream(Stream.Depth, 640, 480);

                var pp = pipeline.Start(cfg);

                SetupWindow(pp, out updateDepth);

                Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames())
                        {
                            var dFrame = frames.DepthFrame.DisposeWith(frames);
                            var colorizeDepth = colorizer.Process<VideoFrame>(dFrame).DisposeWith(frames);

                            Dispatcher.Invoke(DispatcherPriority.Render, updateDepth, colorizeDepth);

                            Dispatcher.Invoke(new Action(() =>
                            {
                                String depth_dev_sn = dFrame.Sensor.Info[CameraInfo.SerialNumber];
                            }));
                        }
                    }
                }, tokenSource.Token);
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);
            }
        }

        #endregion


        #region Depth 화면 setup

        private void SetupWindow(PipelineProfile pipelineProfile, out Action<VideoFrame> depth)
        {
            using (var p = pipelineProfile.GetStream(Stream.Depth).As<VideoStreamProfile>())
                depthFrame.Source = new WriteableBitmap(p.Width, p.Height, 96d, 96d, PixelFormats.Rgb24, null);

            depth = UpdateImage(depthFrame);
        }

        #endregion


        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);

            return array;
        }


        #region INotifyPropertyChanged members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion

    }
}