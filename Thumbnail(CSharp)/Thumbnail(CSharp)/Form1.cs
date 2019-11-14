using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Thumbnail_CSharp_
{
    public partial class Form1 : Form
    {
        //\! =================================== win32 api 函数导入 ======================================
        // 注册
        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        // 解除注册
        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        // 获取窗口大小
        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);

        // 更新缩略图
        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        // 枚举所有窗口
        [DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsCallback lpEnumFunc, int lParam);

        delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        // 获取窗口标题
        [DllImport("user32.dll")]
        public static extern void GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // 获取窗口
        [DllImport("user32.dll")]
        static extern ulong GetWindowLongA(IntPtr hWnd, int nIndex);
        //\! =================================================================================================

        //\! ========================================= 类型定义 ===============================================
        // PSIZE 定义
        [StructLayout(LayoutKind.Sequential)]
        internal struct PSIZE
        {
            public int x;
            public int y;
        }

        // DWM_THUMBNAIL_PROPERTIES 定义
        [StructLayout(LayoutKind.Sequential)]
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
        [StructLayout(LayoutKind.Sequential)]
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
        
        //\! =========================================== 辅助功能 ==============================================
        // 获取所有窗口
        private void GetWindows()
        {
            windows = new List<Window>();

            EnumWindows(Callback, 0);

            comboBox1.Items.Clear();
            foreach (Window w in windows)
                comboBox1.Items.Add(w);
        }

        // 回调
        private bool Callback(IntPtr hwnd, int lParam)
        {
            if (this.Handle != hwnd && (GetWindowLongA(hwnd, GWL_STYLE) & TARGETWINDOW) == TARGETWINDOW)
            {
                StringBuilder sb = new StringBuilder(200);
                GetWindowText(hwnd, sb, sb.Capacity);
                Window t = new Window();
                t.Handle = hwnd;
                t.Title = sb.ToString();
                windows.Add(t);
            }

            return true; //continue enumeration
        }

        // 更新缩略图
        private void UpdateThumb()
        {
            if (thumb != IntPtr.Zero)
            {
                PSIZE size;
                DwmQueryThumbnailSourceSize(thumb, out size);

                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY;
                props.fVisible = true;
                props.opacity = (byte)trackBar1.Value;
                props.rcDestination = new Rect(pictureBox1.Left, pictureBox1.Top, pictureBox1.Right, pictureBox1.Bottom);
                if (size.x < pictureBox1.Width)
                    props.rcDestination.Right = props.rcDestination.Left + size.x;
                if (size.y < pictureBox1.Height)
                    props.rcDestination.Bottom = props.rcDestination.Top + size.y;

                DwmUpdateThumbnailProperties(thumb, ref props);
            }
        }

        private List<Window>    windows;        // 保存窗口
        private IntPtr          thumb;          // 缩略图
        //\! =================================================================================================

        public Form1()
        {
            InitializeComponent();
        }

        //\! ======================================= 界面控件事件处理 ==========================================
        // 窗体加载事件
        private void Form1_Load(object sender, EventArgs e)
        {
            GetWindows();
        }

        // 下拉框选择事件
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Window w = (Window)comboBox1.SelectedItem;
            if (thumb != IntPtr.Zero)
                DwmUnregisterThumbnail(thumb);

            int i = DwmRegisterThumbnail(this.Handle, w.Handle, out thumb);
            if (i == 0)
                UpdateThumb();
        }

        // 刷新按钮单击事件
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (thumb != IntPtr.Zero)
                DwmUnregisterThumbnail(thumb);

            GetWindows();
        }

        // 滑条滚动事件
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            UpdateThumb();
        }

        // 窗体改变大小事件
        private void Form1_Resize(object sender, EventArgs e)
        {
            UpdateThumb();
        }
        //\! ==================================================================================================
    }
}
