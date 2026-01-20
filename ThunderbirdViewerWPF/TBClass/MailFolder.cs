namespace ThunderbirdViewerWPF
{
    using System.Collections.ObjectModel;

    public class MailFolder
    {
        public string Name { get; set; }
        public string FilePath { get; set; }   // mbox Datei
        public ObservableCollection<MailFolder> Children { get; set; } = new();
    }
}