namespace ThunderbirdViewerWPF
{
    using System.IO;
    using System.Text;

    using MimeKit;

    public class MboxParserService
    {
        public IEnumerable<MailMessageModel> Parse(string mboxFile)
        {
            using var stream = File.OpenRead(mboxFile);
            var parser = new MimeParser(stream, MimeFormat.Mbox);

            while (!parser.IsEndOfStream)
            {
                var message = parser.ParseMessage();
                yield return ConvertMessage(message);
            }
        }

        private MailMessageModel ConvertMessage(MimeMessage message)
        {
            var model = new MailMessageModel
            {
                From = message.From.ToString(),
                To = message.To.ToString(),
                Subject = message.Subject,
                Date = message.Date.LocalDateTime,
                Body = message.TextBody ?? message.HtmlBody
            };

            // Anhänge extrahieren
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart part)
                {
                    using var ms = new MemoryStream();
                    part.Content.DecodeTo(ms);

                    model.Attachments.Add(new MailAttachment
                    {
                        FileName = part.FileName,
                        ContentType = part.ContentType.MimeType,
                        Size = ms.Length,
                        Content = ms.ToArray()
                    });
                }
            }

            return model;
        }
    }
}