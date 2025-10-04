using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CountdownApp
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private DateTime _target = new DateTime(2026, 6, 1, 9, 0, 0); // <- podmień na swoją datę
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CountdownApp",
            "settings.json");

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunName = "CountdownApp";

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => UpdateCountdown();
            _timer.Start();

            UpdateCountdown();
            UpdateEventDateText();
            UpdateAutostartButtonText();

            Closing += (_, __) => SaveSettings();
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

        private void UpdateEventDateText()
        {
            var culture = new CultureInfo("pl-PL");
            var formatted = _target.ToString("dddd, dd MMMM yyyy HH:mm", culture);
            var titleCased = culture.TextInfo.ToTitleCase(formatted);
            EventDateText.Text = $"Docelowy termin: {titleCased}";
        }

        private void SetDateBtn_Click(object sender, RoutedEventArgs e)
        {
            var previousTopmost = Topmost;
            try
            {
                Topmost = false;

                var dialog = new SetDateWindow(_target)
                {
                    Owner = this
                };

                var result = dialog.ShowDialog();
                if (result != true)
                {
                    return;
                }

                _target = dialog.SelectedDateTime;
                UpdateCountdown();
                UpdateEventDateText();
                SaveSettings();

                MessageBox.Show(this, $"Ustawiono datę: {_target}", "OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                Topmost = previousTopmost;
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

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings?.TargetDate is DateTime savedTarget)
                {
                    _target = savedTarget;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Nie udało się wczytać ustawień: {ex}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    TargetDate = _target
                };

                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Nie udało się zapisać ustawień: {ex}");
            }
        }
    }
}
