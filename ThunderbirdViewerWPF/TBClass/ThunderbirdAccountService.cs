namespace ThunderbirdViewerWPF
{
    using System.IO;

    public class ThunderbirdAccountService
    {
        public IEnumerable<ThunderbirdAccount> GetAccounts(string profilePath)
        {
            var mailPath = Path.Combine(profilePath, "Mail");
            if (Directory.Exists(mailPath) == false)
            {
                yield break;
            }

            foreach (var dir in Directory.GetDirectories(mailPath))
            {
                yield return new ThunderbirdAccount
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                };
            }
        }
    }
}