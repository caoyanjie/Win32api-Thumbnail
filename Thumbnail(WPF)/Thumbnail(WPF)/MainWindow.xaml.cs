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

namespace Thumbnail_WPF_
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Width = 1242;
            this.Height = 744;
            this.Background = new SolidColorBrush(Color.FromArgb(255, 24, 15, 56));

            wrapPanel.Margin = new System.Windows.Thickness(98, 103, 98, 175);
            //wrapPanel.Padding = new System.Windows.Thickness(40, 103, 40, 175);

            GetWindows();

            foreach (Window window in windows)
            {
                Button btn = new Button();
                btn.Name = "btn1";
                btn.Width = 240;
                btn.Height = 180;
                btn.Margin = new System.Windows.Thickness(5);
                btn.Content = window.Title;
                btn.Background = new SolidColorBrush(Color.FromArgb(255, 41, 30, 89));
                btn.Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 217, 217));
                btn.VerticalContentAlignment = VerticalAlignment.Bottom;
                btn.Click += new RoutedEventHandler(btn_click);

                // 保存按钮
                btns.Add(btn);

                // 添加到布局
                wrapPanel.Children.Add(btn);
            }
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

        //\! =========================================== 辅助功能 =============================================
        // 获取所有窗口
        private void GetWindows()
        {
            windows = new List<Window>();
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
            for (var i = 0; i < btns.Count; ++i)
            {
                IntPtr thumb;
                DwmRegisterThumbnail(new System.Windows.Interop.WindowInteropHelper(this).Handle, windows[i].Handle, out thumb);

                if (thumb != IntPtr.Zero)
                {
                    //PSIZE size;
                    //DwmQueryThumbnailSourceSize(thumb, out size);

                    DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                    props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY;
                    props.fVisible = true;
                    props.opacity = 255;

                    Point point = btns[i].TranslatePoint(new Point(0, 0), this);
                    int w = 230;
                    int h = 130;
                    int margin = 5;

                    props.rcDestination = new Rect((int)point.X+margin, (int)point.Y+margin, (int)point.X+margin+w, (int)point.Y+margin+h);
                    //if (size.y < pictureBox1.Height)
                    {
                        //props.rcDestination.Bottom = props.rcDestination.Top + 20;// size.y;
                    }

                    DwmUpdateThumbnailProperties(thumb, ref props);
                }

            }
        }

        private List<Window>    windows;    // 保存窗口
        private List<Button>    btns = new List<Button>();
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
        //\! =================================================================================================
    }
}
