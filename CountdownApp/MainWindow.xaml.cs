using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CountdownApp
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private DateTime _target = new DateTime(2026, 6, 1, 9, 0, 0); // <- podmień na swoją datę

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunName = "CountdownApp";

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => UpdateCountdown();
            _timer.Start();

            UpdateCountdown();
            UpdateAutostartButtonText();
        }

        private void UpdateCountdown()
        {
            var now = DateTime.Now;
            var diff = _target - now;

            if (diff < TimeSpan.Zero)
            {
                CountdownText.Text = "Wydarzenie już minęło 🎉";
                return;
            }

            CountdownText.Text = $"{diff.Days} d {diff.Hours:D2} h {diff.Minutes:D2} m {diff.Seconds:D2} s";
        }

        private void SetDateBtn_Click(object sender, RoutedEventArgs e)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Podaj datę i godzinę (np. 2026-06-01 09:00):",
                "Ustaw wydarzenie",
                _target.ToString("yyyy-MM-dd HH:mm"));

            if (DateTime.TryParse(input, out var parsed))
            {
                _target = parsed;
                UpdateCountdown();
                MessageBox.Show($"Ustawiono datę: {_target}", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Nieprawidłowy format daty/godziny.", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutostartBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsAutostartEnabled())
                    DisableAutostart();
                else
                    EnableAutostart();

                UpdateAutostartButtonText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się zmienić autostartu:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAutostartButtonText()
        {
            AutostartBtn.Content = IsAutostartEnabled() ? "Autostart: wyłącz" : "Autostart: włącz";
        }

        private bool IsAutostartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(AppRunName) as string;
            return !string.IsNullOrEmpty(value);
        }

        private void EnableAutostart()
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName!;
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            key.SetValue(AppRunName, $"\"{exePath}\"");
        }

        private void DisableAutostart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(AppRunName, false);
        }
    }
}
