//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Lifeprojects.de">
//     Class: Program
//     Copyright © Lifeprojects.de 2026
// </copyright>
// <Template>
// 	Version 3.0.2026.1, 08.1.2026
// </Template>
//
// <author>Gerhard Ahrens - Lifeprojects.de</author>
// <email>developer@lifeprojects.de</email>
// <date>17.01.2026 10:07:40</date>
//
// <summary>
// Konsolen Applikation mit Menü
// </summary>
//-----------------------------------------------------------------------

/* Imports from NET Framework */
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

using MimeKit;

namespace ThunderbirdViewer
{
    public class Program
    {
        private static void Main(string[] args)
        {
            ConsoleMenu.Add("1", "Viewer (Mails empfangen)", () => MenuPoint1());
            ConsoleMenu.Add("2", "Viewer (Mails versendet)", () => MenuPoint2());
            ConsoleMenu.Add("3", "Viewer mit MimeKit", () => MenuPoint3());
            ConsoleMenu.Add("X", "Beenden", () => ApplicationExit());

            do
            {
                _ = ConsoleMenu.SelectKey(2, 2);
            }
            while (true);
        }

        private static void ApplicationExit()
        {
            Environment.Exit(0);
        }

        private static void MenuPoint1()
        {
            Console.Clear();

            string userName = Environment.ExpandEnvironmentVariables("%username%");
            string profilePath = $@"c:\Users\{userName}\AppData\Roaming\Thunderbird\Profiles\1mq6blei.default-release\Mail\Local Folders\Sent";
            var mails = ThunderbirdMboxReader.Read(profilePath);

            ConsoleMenu.Wait();
        }

        private static void MenuPoint2()
        {
            Console.Clear();

            string userName = Environment.ExpandEnvironmentVariables("%username%");
            string profilePath = $@"c:\Users\{userName}\AppData\Roaming\Thunderbird\Profiles\1mq6blei.default-release\Mail\Local Folders\Inbox";
            var mails = ThunderbirdMboxReader.Read(profilePath);

            ConsoleMenu.Wait();
        }

        private static void MenuPoint3()
        {
            Console.Clear();

            string userName = Environment.ExpandEnvironmentVariables("%username%");
            string profilePath = $@"c:\Users\{userName}\AppData\Roaming\Thunderbird\Profiles\1mq6blei.default-release\Mail\Local Folders\Inbox";

            using var stream = File.OpenRead(profilePath);
            var parser = new MimeParser(stream, MimeFormat.Mbox);

            while (!parser.IsEndOfStream)
            {
                var msg = parser.ParseMessage();
                Console.WriteLine(msg.Subject);
            }
            ConsoleMenu.Wait();
        }
    }

    public class ThunderbirdMboxReader
    {
        private static readonly Regex HeaderRegex =
        new Regex(@"^(.*?):\s*(.*)$", RegexOptions.Multiline);

        public static List<ThunderbirdMail> Read(string mboxPath)
        {
            var mails = new List<ThunderbirdMail>();
            var text = File.ReadAllText(mboxPath);

            var entries = Regex.Split(text, @"(?m)^From .*$");

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                int headerEnd = entry.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0)
                {
                    headerEnd = entry.IndexOf("\n\n", StringComparison.Ordinal);
                    if (headerEnd < 0)
                    {
                        continue;
                    }
                }

                var headerText = entry.Substring(0, headerEnd);
                var body = entry.Substring(headerEnd).Trim();

                var headers = ParseHeaders(headerText);

                var mail = new ThunderbirdMail
                {
                    From = headers.GetValueOrDefault("From"),
                    To = headers.GetValueOrDefault("To"),
                    Subject = headers.GetValueOrDefault("Subject"),
                    Body = body
                };

                if (headers.TryGetValue("Date", out var date) && DateTime.TryParse(date, out var dt))
                {
                    mail.Date = dt;
                }

                mails.Add(mail);
            }

            return mails;
        }

        private static Dictionary<string, string> ParseHeaders(string headerText)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string currentHeader = null;

            foreach (var line in headerText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Header-Folding (RFC 5322)
                if ((line.StartsWith(" ") || line.StartsWith("\t")) && currentHeader != null)
                {
                    headers[currentHeader] += " " + line.Trim();
                    continue;
                }

                var idx = line.IndexOf(':');
                if (idx < 0) continue;

                currentHeader = line.Substring(0, idx);
                headers[currentHeader] = line.Substring(idx + 1).Trim();
            }

            return headers;
        }
    }

    [DebuggerDisplay("From:{this.From}; To:{this.To}; Date:{this.Date}")]
    public class ThunderbirdMail
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public DateTime? Date { get; set; }
        public string Body { get; set; }
        public bool IsSent { get; set; }
    }
}
