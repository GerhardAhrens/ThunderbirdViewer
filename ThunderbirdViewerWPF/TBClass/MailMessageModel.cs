namespace ThunderbirdViewerWPF
{
    using System.Collections.ObjectModel;

    public class MailMessageModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }

        public bool IsSent { get; set; }

        public string DisplayAddress =>
            IsSent == true ? $"An: {To}" : $"Von: {From}";

        public ObservableCollection<MailAttachment> Attachments { get; set; } = new();

        public bool HasAttachments => Attachments?.Count > 0;
    }
}