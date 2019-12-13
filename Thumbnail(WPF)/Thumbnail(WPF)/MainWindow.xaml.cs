using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace Thumbnail_WPF_
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private DispatcherTimer updateWindowsTimer;
        public MainWindow()
        {
            InitializeComponent();

            this.Width = 1242;
            this.Height = 744;
            this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 24, 15, 56));

            wrapPanel.Margin = new System.Windows.Thickness(58, 30, 58, 0);
            //wrapPanel.Padding = new System.Windows.Thickness(40, 103, 40, 175);

            // 添加一个占位符，让第一个预览框绘制桌面内容，因为用win32 api得不到只能得到桌面壁纸缩略图，得不到桌面内容
            windows.Add(new Window());

            // 获取所有窗口
            GetWindows();

            // 为每个按钮生成一个button
            foreach (Window window in windows)
            {
                Button btn = new Button();
                btn.Name = "btn1";
                btn.Width = 240;
                btn.Height = 180;
                btn.Margin = new System.Windows.Thickness(5);
                btn.Content = window.Title;
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 41, 30, 89));
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 217, 217, 217));
                btn.VerticalContentAlignment = VerticalAlignment.Bottom;
                btn.Click += new RoutedEventHandler(btn_click);

                // 保存按钮
                btns.Add(btn);

                // 添加到布局
                wrapPanel.Children.Add(btn);
            }

            // 定时器更新缩略图
            updateWindowsTimer = new DispatcherTimer();
            updateWindowsTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            updateWindowsTimer.Tick += new EventHandler(updateThumbnailTimeout);
            updateWindowsTimer.Start();
        }

        //\! =================================== win32 api 函数导入 ======================================
        // 注册
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        // 解除注册
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        // 获取窗口大小
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        // 更新缩略图
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        // 枚举所有窗口
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

        delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        // 获取窗口标题
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void GetWindowTextA(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // 获取类名
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void GetClassNameA(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        // 获取窗口
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);

        // 最大化窗口，最小化窗口，正常大小窗口；
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        //\! =================================================================================================

        //\! ========================================= 类型定义 ==============================================
        // PSIZE 定义
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct PSIZE
        {
            public int x;
            public int y;
        }

        // DWM_THUMBNAIL_PROPERTIES 定义
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        // Rect 定义
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct Rect
        {
            internal Rect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Window 定义
        internal class Window
        {
            public string Title;
            public IntPtr Handle;

            public override string ToString()
            {
                return Title;
            }
        }

        // 枚举
        static readonly ulong WS_VISIBLE = 0x10000000L;
        static readonly ulong WS_BORDER = 0x00800000L;
        static readonly ulong TARGETWINDOW = WS_BORDER | WS_VISIBLE;

        static readonly int GWL_STYLE = -16;

        static readonly int DWM_TNP_VISIBLE = 0x8;
        static readonly int DWM_TNP_OPACITY = 0x4;
        static readonly int DWM_TNP_RECTDESTINATION = 0x1;
        //\! ==================================================================================================

        //\! =========================================== 窗口缩略图 =============================================
        // 获取所有窗口
        private void GetWindows()
        {
            EnumWindows(Callback, 0);
        }

        // 获取窗口回调
        private bool Callback(IntPtr hwnd, int lParam)
        {
            if (new System.Windows.Interop.WindowInteropHelper(this).Handle != hwnd && (GetWindowLongA(hwnd, GWL_STYLE) & TARGETWINDOW) == TARGETWINDOW)
            {
                StringBuilder windowStringBuilder = new StringBuilder(200);
                StringBuilder classStringBuilder = new StringBuilder(200);
                //GetWindowText(hwnd, sb, sb.Capacity);
                GetWindowTextA(hwnd, windowStringBuilder, windowStringBuilder.Capacity);
                GetClassNameA(hwnd, classStringBuilder, classStringBuilder.Capacity);
                string windowTitle = windowStringBuilder.ToString();
                string className = classStringBuilder.ToString();
                if (windowTitle != "\0" && className != "Windows.UI.Core.CoreWindow" && className != "ApplicationFrameWindow" && className != "Progman" && IsWindow(hwnd) && IsWindowEnabled(hwnd) && IsWindowVisible(hwnd))
                {
                    Window t = new Window();
                    t.Handle = hwnd;
                    t.Title = windowTitle;
                    windows.Add(t);
                }
            }

            return true; //continue enumeration
        }

        // 更新缩略图
        private void UpdateThumb()
        {
            // 解除注册缩略图
            foreach (var thumb in thumbs)
            {
                DwmUnregisterThumbnail(thumb);
            }
            thumbs.Clear();

            // 重新注册缩略图
            for (var i = 0; i < btns.Count; ++i)
            {
                IntPtr thumb;
                DwmRegisterThumbnail(new System.Windows.Interop.WindowInteropHelper(this).Handle, windows[i].Handle, out thumb);

                if (thumb != IntPtr.Zero)
                {
                    PSIZE size;
                    DwmQueryThumbnailSourceSize(thumb, out size);

                    DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                    props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY;
                    props.fVisible = true;
                    props.opacity = 255;

                    System.Windows.Point point = btns[i].TranslatePoint(new System.Windows.Point(0, 0), this);
                    int w = 230;
                    int h = 130;
                    int margin = 5;

                    props.rcDestination = new Rect((int)point.X+margin, (int)point.Y+margin, (int)point.X+margin+w, (int)point.Y+margin+h);

                    // 超出可视范围处理
                    int topLimit = 87;
                    int bottomLimit = (int)this.Height - 175 + 45;
                    if (point.Y < topLimit)
                    {
                        props.rcDestination.Top = topLimit;

                        props.rcSource.Top = 500;
                        props.rcSource.Bottom = size.y;
                        props.rcSource.Left = 0;
                        props.rcSource.Right = size.x;
                    }
                    else if (point.Y+h > bottomLimit)
                    {
                        props.rcDestination.Bottom = bottomLimit;
                    }

                    // 按照设置属性更新缩略图
                    DwmUpdateThumbnailProperties(thumb, ref props);

                    // 记录此缩略图，方便下次解除注册
                    thumbs.Add(thumb);
                }
            }
        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0; BitmapImage result = new BitmapImage();
                result.BeginInit(); result.CacheOption = BitmapCacheOption.OnLoad; result.StreamSource = stream; result.EndInit();
                result.Freeze();
                return result;
            }
        }

        // 把桌面内容截图贴到按钮上显示
        private void mapDesktopContentToButton()
        {
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            var bitmap = new Bitmap(screenWidth, screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics memoryGrahics = Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
            }

            BitmapImage bg = BitmapToBitmapImage(bitmap);
            btns[0].Background = new ImageBrush(bg);
        }

        private List<Window>    windows = new List<Window>();    // 保存窗口
        private List<Button>    btns = new List<Button>();
        private List<IntPtr>    thumbs = new List<IntPtr>();
        //\! =================================================================================================

        //\! ======================================== UI 事件 ================================================
        // 按钮单击事件
        private void btn_click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
        }

        // 窗体加载事件
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThumb();
        }

        // 视口滚动事件
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateThumb();
        }

        private void updateThumbnailTimeout(object sender, EventArgs e)
        {
            mapDesktopContentToButton();
        }
        //\! =================================================================================================

        //\! ========================================= 服务 ==================================================
        private void outputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            dispatcher.Invoke(() =>
            {
                string _result = e.Data;

            });
        }

        private void outputErrorReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            string _error = e.Data;
        }
        //\! =================================================================================================
    }
}
