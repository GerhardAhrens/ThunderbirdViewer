namespace ThunderbirdViewerWPF
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class FolderHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string folderName)
                return "Kontakt";

            var name = folderName.ToLowerInvariant();

            // Gesendet-Ordner
            if (name.Contains("sent") ||
                name.Contains("gesendet") ||
                name.Contains("sent items"))
            {
                return "An";
            }

            // Posteingang
            if (name.Contains("inbox") ||
                name.Contains("posteingang"))
            {
                return "Von";
            }

            // Neutral (Archive, Custom Folder)
            return "Kontakt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
