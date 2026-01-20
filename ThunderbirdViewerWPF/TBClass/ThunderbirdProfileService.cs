namespace ThunderbirdViewerWPF
{
    using System.IO;

    public class ThunderbirdProfileService
    {
        public IEnumerable<ThunderbirdProfile> GetProfiles()
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Thunderbird", "Profiles");

            if (Directory.Exists(basePath) == false)
            {
                yield break;
            }

            foreach (var dir in Directory.GetDirectories(basePath))
            {
                yield return new ThunderbirdProfile
                {
                    Name = Path.GetFileName(dir),
                    Path = dir
                };
            }
        }
    }
}