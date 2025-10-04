using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace CountdownApp
{
    public partial class SetDateWindow : Window
    {
        public DateTime SelectedDateTime { get; private set; }

        public SetDateWindow(DateTime initialDateTime)
        {
            InitializeComponent();

            SelectedDateTime = initialDateTime;

            DateSelector.SelectedDate = initialDateTime.Date;

            var hours = Enumerable.Range(0, 24).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();
            var minutes = Enumerable.Range(0, 60).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();

            HourList.ItemsSource = hours;
            MinuteList.ItemsSource = minutes;

            HourList.SelectedItem = initialDateTime.Hour.ToString("D2", CultureInfo.InvariantCulture);
            MinuteList.SelectedItem = initialDateTime.Minute.ToString("D2", CultureInfo.InvariantCulture);

            HourList.ScrollIntoView(HourList.SelectedItem);
            MinuteList.ScrollIntoView(MinuteList.SelectedItem);
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
            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}