using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;


namespace FastClipper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder);
            SavePathTextBox.Text = saveFolder;
            SavePathTextBox.ToolTip = saveFolder;
        }
        private const int HOTKEY_ID = 9001;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private HwndSource _source;

        // Change this to your desired save folder
        private string saveFolder = GetScreenshotsFolder();
 

private static string GetScreenshotsFolder()
    {
        // Get the user's "Pictures" folder
        string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // Define the "Screenshots" folder inside "Pictures"
        string screenshotsFolder = Path.Combine(picturesFolder, "Screenshots");

        // Ensure the folder exists
        if (!Directory.Exists(screenshotsFolder))
        {
            Directory.CreateDirectory(screenshotsFolder);
        }

        return screenshotsFolder;
    }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            // Register PrintScreen key (0x2C)
            RegisterHotKey(helper.Handle, HOTKEY_ID, 0, 0x2C);
        }
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            bool held = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            const int WM_HOTKEY = 0x0312;
       
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                if (held) MessageBox.Show("Ctrl Key Held");
                CaptureScreenshot(saveFolder, held);
                if (ChckMessage.IsChecked == true)   ScreenNotification.DrawNotificationOnScreen("Screenshot Captured", Colors.LimeGreen, 1000, 24);
             
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void DisplayOverLayMessage(string v1, Color green, int v2)
        {
            throw new NotImplementedException();
        }

        [DllImport("user32.dll")]
 
   
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
        public static void CaptureScreenshot(string saveFolder,  bool OnlyWindow = false)
        {
            Bitmap bmp;

            if (OnlyWindow)
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return; // No window

                GetWindowRect(hWnd, out RECT rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                }
            }
            else
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                bmp = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }
            }
            string filename = Path.Combine(saveFolder, $"{GetActiveWindowTitle()}_{DateTime.Now:dd_MMM_yyyy_HH'h'_mm'm'_ss's'}.png"); 
      
            bmp.Save(filename, ImageFormat.Png);
            bmp.Dispose();
        }

        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();
            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            return null;
        }

        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        protected override void OnClosed(EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
            _source.RemoveHook(HwndHook);
            base.OnClosed(e);
        }

        private void open(object sender, RoutedEventArgs e)
        {
            string folder = SavePathTextBox.Text;
            if (Directory.Exists(folder)){
                                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            string selected = SelectFolderFunc();
            if (Directory.Exists(selected))
            {
                saveFolder = selected;
                SavePathTextBox.Text = selected;
                SavePathTextBox.ToolTip = selected;
            }

        }

        private string SelectFolderFunc()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to save screenshots";
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return string.Empty;
        }
    }
    public static class ScreenNotification
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        public static async void DrawNotificationOnScreen(string message, Color color, int durationMs, int fontSize)
        {
            Window notificationWindow = new Window
            {
                Width = 400,
                Height = 100,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowActivated = false // prevents focus stealing
            };

            // Add message
            TextBlock textBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(color),
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            notificationWindow.Content = textBlock;

            notificationWindow.SourceInitialized += (s, e) =>
            {
                // Apply extended styles for no activation
                var hwnd = new WindowInteropHelper(notificationWindow).Handle;
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            };

            notificationWindow.Show();

            await System.Threading.Tasks.Task.Delay(durationMs);

            notificationWindow.Close();
        }
    }
}