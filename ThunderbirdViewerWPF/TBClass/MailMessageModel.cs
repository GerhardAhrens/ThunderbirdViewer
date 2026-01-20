namespace ThunderbirdViewerWPF
{
    public class MailMessageModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }

        public bool IsSent { get; set; }

        public string DisplayAddress =>
            IsSent ? To : From;
    }
}