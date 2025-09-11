using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using RSLBot.Core.Interop;

namespace RSLBot.Core.Services
{
    public class WindowPicker : Window
    {
        private readonly string[] _ignoreProcesses = { "applicationframehost", "shellexperiencehost", "systemsettings", "winstore.app", "searchui" };
        private ListBox _windowsListBox;

        public WindowPicker()
        {
            Title = "Please select the window to capture";
            Width = 800;
            Height = 450;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Content = grid;

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(5, 0, 5, 0),
                Orientation = Orientation.Horizontal
            };
            Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            var textBlock = new TextBlock
            {
                FontSize = 20,
                Text = "Double-Click to select the window you want to capture"
            };
            stackPanel.Children.Add(textBlock);

            _windowsListBox = new ListBox
            {
                Padding = new Thickness(0, 10, 0, 10),
                BorderThickness = new Thickness(0)
            };
            Grid.SetRow(_windowsListBox, 1);
            grid.Children.Add(_windowsListBox);

            var itemTemplate = new DataTemplate();
            var contentControlFactory = new FrameworkElementFactory(typeof(ContentControl));
            contentControlFactory.AddHandler(MouseDoubleClickEvent, new MouseButtonEventHandler(WindowsOnMouseDoubleClick));
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
            textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            contentControlFactory.AppendChild(textBlockFactory);
            itemTemplate.VisualTree = contentControlFactory;
            _windowsListBox.ItemTemplate = itemTemplate;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FindWindows();
        }

        public IntPtr PickCaptureTarget(IntPtr hWnd)
        {
            new WindowInteropHelper(this).Owner = hWnd;
            ShowDialog();

            return ((CapturableWindow?)_windowsListBox.SelectedItem)?.Handle ?? IntPtr.Zero;
        }

        private void FindWindows()
        {
            var wih = new WindowInteropHelper(this);
            User32.EnumWindows((hWnd, lParam) =>
            {
                if (!User32.IsWindowVisible(hWnd))
                    return true;

                var title = new StringBuilder(1024);
                User32.GetWindowText(hWnd, title, title.Capacity);
                if (string.IsNullOrWhiteSpace(title.ToString()))
                    return true;

                if (wih.Handle == hWnd)
                    return true;

                User32.GetWindowThreadProcessId(hWnd, out var processId);

                var process = Process.GetProcessById((int)processId);
                if (_ignoreProcesses.Contains(process.ProcessName.ToLower()))
                    return true;

                _windowsListBox.Items.Add(new CapturableWindow
                {
                    Handle = hWnd,
                    Name = $"{title} ({process.ProcessName}.exe)"
                });

                return true;
            }, IntPtr.Zero);
        }

        private void WindowsOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }

    public struct CapturableWindow
    {
        public string Name { get; set; }
        public IntPtr Handle { get; set; }
    }
}
