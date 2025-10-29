using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace CountdownApp
{
    public partial class SetDateWindow : Window
    {
        public DateTime SelectedDateTime { get; private set; }
        public string? SelectedMediaPath { get; private set; }
        public string? SelectedCaption { get; private set; }
        public string? SelectedIcon { get; private set; }

        public SetDateWindow(DateTime initialDateTime, string? initialMediaPath, string? initialCaption, string? initialIcon)
        {
            InitializeComponent();

            SelectedDateTime = initialDateTime;
            SelectedMediaPath = string.IsNullOrWhiteSpace(initialMediaPath) ? null : initialMediaPath;
            SelectedCaption = string.IsNullOrWhiteSpace(initialCaption) ? null : initialCaption;
            SelectedIcon = string.IsNullOrWhiteSpace(initialIcon) ? null : initialIcon;

            DateSelector.SelectedDate = initialDateTime.Date;

            var hours = Enumerable.Range(0, 24).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();
            var minutes = Enumerable.Range(0, 60).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();

            HourList.ItemsSource = hours;
            MinuteList.ItemsSource = minutes;

            HourList.SelectedItem = initialDateTime.Hour.ToString("D2", CultureInfo.InvariantCulture);
            MinuteList.SelectedItem = initialDateTime.Minute.ToString("D2", CultureInfo.InvariantCulture);

            HourList.ScrollIntoView(HourList.SelectedItem);
            MinuteList.ScrollIntoView(MinuteList.SelectedItem);

            MediaPathTextBox.Text = SelectedMediaPath ?? string.Empty;
            CaptionTextBox.Text = SelectedCaption ?? string.Empty;
            IconTextBox.Text = SelectedIcon ?? string.Empty;
        }

        private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DateSelector.SelectedDate is not DateTime selectedDate)
            {
                MessageBox.Show(this, "Wybierz datę wydarzenia.", "Brak daty",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HourList.SelectedItem is not string hourText || MinuteList.SelectedItem is not string minuteText)
            {
                MessageBox.Show(this, "Wybierz godzinę wydarzenia.", "Brak godziny",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hour = int.Parse(hourText, CultureInfo.InvariantCulture);
            var minute = int.Parse(minuteText, CultureInfo.InvariantCulture);

            SelectedDateTime = selectedDate.Date.AddHours(hour).AddMinutes(minute);
            SelectedMediaPath = string.IsNullOrWhiteSpace(MediaPathTextBox.Text) ? null : MediaPathTextBox.Text.Trim();
            SelectedCaption = string.IsNullOrWhiteSpace(CaptionTextBox.Text) ? null : CaptionTextBox.Text.Trim();
            SelectedIcon = string.IsNullOrWhiteSpace(IconTextBox.Text) ? null : IconTextBox.Text.Trim();
            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ChooseMediaButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Obrazy i animacje|*.gif;*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.tiff;*.tif|Wszystkie pliki|*.*",
                Title = "Wybierz obraz lub animację"
            };

            if (dialog.ShowDialog(this) == true)
            {
                MediaPathTextBox.Text = dialog.FileName;
            }
        }

        private void ClearMediaButton_OnClick(object sender, RoutedEventArgs e)
        {
            MediaPathTextBox.Text = string.Empty;
        }
    }
}