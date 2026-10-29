using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CountdownApp
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private DateTime _target = DateTime.Today.AddDays(30).AddHours(9);
        private string? _mediaPath;
        private string? _mediaCaption;
        private string? _eventIcon;
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CountdownApp",
            "settings.json");

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunName = "CountdownApp";
        private const string DefaultMediaPath = "pack://application:,,,/Assets/freedom.gif";
        private const string DefaultMediaCaption = "Najlepiej oddycham między gwiazdami, gdy nikt nie zagłusza nieba.";
        private const string DefaultEventIcon = "☀️";

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => UpdateCountdown();
            _timer.Start();

            UpdateCountdown();
            UpdateEventDateText();
            UpdateMediaContent();
            UpdateMediaCaption();
            UpdateEventIconDisplay();
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

                var dialog = new SetDateWindow(_target, _mediaPath, _mediaCaption, _eventIcon)
                {
                    Owner = this
                };

                var result = dialog.ShowDialog();
                if (result != true)
                {
                    return;
                }

                _target = dialog.SelectedDateTime;
                _mediaPath = dialog.SelectedMediaPath;
                _mediaCaption = dialog.SelectedCaption;
                _eventIcon = dialog.SelectedIcon;
                UpdateCountdown();
                UpdateEventDateText();
                UpdateMediaContent();
                UpdateMediaCaption();
                UpdateEventIconDisplay();
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
                if (settings != null)
                {
                    if (settings.TargetDate is DateTime savedTarget)
                    {
                        _target = savedTarget;
                    }

                    _mediaPath = NormalizeString(settings.MediaPath);
                    _mediaCaption = NormalizeString(settings.MediaCaption);
                    _eventIcon = NormalizeString(settings.EventIcon);
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
                    TargetDate = _target,
                    MediaPath = _mediaPath,
                    MediaCaption = _mediaCaption,
                    EventIcon = _eventIcon
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

        private void UpdateMediaContent()
        {
            if (!TrySetMediaContent(_mediaPath))
            {
                TrySetMediaContent(DefaultMediaPath);
            }
        }

        private bool TrySetMediaContent(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            if (!Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var uri))
            {
                return false;
            }

            FrameworkElement mediaElement;
            if (IsGifSource(source))
            {
                mediaElement = new AnimatedGifImage
                {
                    Width = 220,
                    Height = 220,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    GifSource = uri
                };
            }
            else
            {
                var bitmap = LoadBitmap(uri);
                if (bitmap == null)
                {
                    return false;
                }

                mediaElement = new Image
                {
                    Width = 220,
                    Height = 220,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Source = bitmap
                };
            }

            MediaPresenter.Content = mediaElement;
            return true;
        }

        private static bool IsGifSource(string source)
        {
            return source.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        }

        private static ImageSource? LoadBitmap(Uri uri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateMediaCaption()
        {
            var caption = string.IsNullOrWhiteSpace(_mediaCaption)
                ? DefaultMediaCaption
                : _mediaCaption;
            MediaCaptionText.Text = caption;
        }

        private void UpdateEventIconDisplay()
        {
            var icon = string.IsNullOrWhiteSpace(_eventIcon)
                ? DefaultEventIcon
                : _eventIcon;
            EventIconDisplay.Text = icon;
        }

        private static string? NormalizeString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
