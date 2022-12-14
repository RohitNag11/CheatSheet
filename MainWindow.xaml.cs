using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;

namespace CheatSheet
{

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

        
        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        BackdropType m_currentBackdrop;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController m_acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;
        AppSummary m_activeApp;
        List<ShortcutGroup> m_shortcutGroups;
        private enum BackdropType
        {
            DesktopAcrylic,
            DefaultColor,
        }

        public MainWindow()
        {
            this.InitializeComponent();

            //// TODO: test code is here to be reused elsewhere
            //var appName = GetActiveWindowProcessName();
            //Trace.WriteLine(appName);
            //PrintCommands(appName);

            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            this.SetBackdrop(BackdropType.DesktopAcrylic);
            Title = "CheatSheet";
            // Hide default title bar.
            ExtendsContentIntoTitleBar = true;
            //SetTitleBar(AppTitleBar);

            this.InitializeActiveAppData();
            var appFriendlyName = m_activeApp.FriendlyName;
            ForegroundAppTextBlock.Text = GetActiveAppTitle();
            ForegroundAppImage.Source = GetActiveAppIcon();
            //ShortcutGroupRepeater.ItemsSource = m_shortcutGroups;

        }

        private void SetBackdrop(BackdropType type)
        {
            // Reset to default color. If the requested type is supported, we'll update to that.
            m_currentBackdrop = BackdropType.DefaultColor;
            tbChangeStatus.Text = "";
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= Window_Activated;
            this.Closed -= Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged -= Window_ThemeChanged;
            m_configurationSource = null;

            if (type == BackdropType.DesktopAcrylic)
            {
                if (TrySetAcrylicBackdrop())
                {
                    m_currentBackdrop = type;
                }
                else
                {
                    // Acrylic isn't supported, so take the next option, which is DefaultColor, which is already set.
                    tbChangeStatus.Text += "  Acrylic isn't supported. Switching to default color.";
                }
            }
        }

        bool TrySetAcrylicBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }
            else
            {
                btnChangeBackdrop.IsEnabled = false; // Disable backgdrop button
            }

            return false; // Acrylic is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        void ChangeBackdropButton_Click(object sender, RoutedEventArgs e)
        {
            BackdropType newType;
            switch (m_currentBackdrop)
            {
                case BackdropType.DesktopAcrylic: 
                    newType = BackdropType.DefaultColor;
                    break;
                default:
                case BackdropType.DefaultColor: newType = BackdropType.DesktopAcrylic; break;
            }
            SetBackdrop(newType);
        }

        private string GetActiveWindowProcessName()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();
            uint lpdwProcessId;
            GetWindowThreadProcessId(handle, out lpdwProcessId);
            IntPtr hProcess = OpenProcess(0x0410, false, lpdwProcessId);
            StringBuilder text = new StringBuilder(1000);
            GetModuleFileNameEx(hProcess, IntPtr.Zero, text, text.Capacity);
            CloseHandle(hProcess);

            var filename = text.ToString().Split('\\')?.Last();

            return filename;
        }

        private void PrintCommands(string appName)
        {
            string json = ShortcutsData.Shortcuts;
            var root = JsonConvert.DeserializeObject<ShortcutsJsonRoot>(json);

            StringBuilder outputBuilder = new StringBuilder();
            var appsArray = root.Apps;
            foreach (var app in appsArray)
            {
                if (app.Name.ToLower() == appName.ToLower())
                {
                    outputBuilder.AppendLine(app.FriendlyName + ":");
                    foreach (var shortcutGroup in app.ShortcutGroups)
                    {
                        outputBuilder.AppendLine("  " + shortcutGroup.Name);
                        foreach (var shortcut in shortcutGroup.Shortcuts)
                        {
                            outputBuilder.Append("    ");
                            foreach (var keyCombination in shortcut.Keys)
                            {
                                foreach (var key in keyCombination.SkipLast(1))
                                {
                                    outputBuilder.Append(key + " + ");
                                }
                                outputBuilder.Append(keyCombination.Last() + " ");
                            }

                            outputBuilder.AppendLine(": " + shortcut.Description);
                        }
                    }
                    break;
                }
            }

            var output = outputBuilder.ToString();
            Trace.WriteLine(output);
        }

        private void InitializeActiveAppData()
        {
            var appName = GetActiveWindowProcessName();
            string json = ShortcutsData.Shortcuts;
            var root = JsonConvert.DeserializeObject<ShortcutsJsonRoot>(json);
            var appsArray = root.Apps;
            var system = new AppSummary();
            foreach (var app in appsArray)
            {
                if (app.Name.ToLower() == appName.ToLower())
                {
                    m_activeApp = app;
                    m_shortcutGroups = app.ShortcutGroups;
                    return;
                }
                if (app.Name.ToLower() == "system")
                {
                    system = app;
                }
            }
            m_activeApp = system;
            m_shortcutGroups = system.ShortcutGroups;
            return;
        }

        private string GetActiveAppTitle()
        {
            var appFriendlyName = m_activeApp.FriendlyName;
            return appFriendlyName + " Shortcuts";
        }

        private BitmapImage GetActiveAppIcon()
        {
            var appFriendlyName = m_activeApp.FriendlyName;
            var iconPath = appFriendlyName.ToLower();
            char[] whitespace = new char[] { ' ', '\t', '\r', '\n' };
            iconPath = String.Join("-", iconPath.Split(whitespace, StringSplitOptions.RemoveEmptyEntries));
            var uri = new Uri("ms-appx:///Assets/AppIcons/" + iconPath + "-icon.png");
            return new BitmapImage(uri);
        }
    }
}