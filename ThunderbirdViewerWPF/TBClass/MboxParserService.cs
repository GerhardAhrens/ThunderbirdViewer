namespace ThunderbirdViewerWPF
{
    using System.IO;
    using System.Text;

    public class MboxParserService
    {
        public IEnumerable<MailMessageModel> Parse(string mboxFile)
        {
            if (File.Exists(mboxFile) == false)
            {
                yield break;
            }

            var lines = File.ReadAllLines(mboxFile);
            MailMessageModel current = null;
            var body = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("From "))
                {
                    if (current != null)
                    {
                        current.Body = body.ToString();
                        yield return current;
                    }

                    current = new MailMessageModel();
                    body.Clear();
                }
                else if (line.StartsWith("Subject:"))
                    current.Subject = line.Substring(8).Trim();
                else if (line.StartsWith("From:"))
                    current.From = line.Substring(5).Trim();
                else if (line.StartsWith("To:"))
                    current.To = line.Substring(3).Trim();
                else if (line.StartsWith("Date:") &&
                         DateTime.TryParse(line.Substring(5), out var date))
                    current.Date = date;
                else
                    body.AppendLine(line);
            }

            if (current != null)
            {
                current.Body = body.ToString();
                yield return current;
            }
        }
    }
}