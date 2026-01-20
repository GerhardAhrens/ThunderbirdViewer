namespace ThunderbirdViewerWPF
{
    using System.Collections.ObjectModel;
    using System.IO;

    public class ImapFolderService
    {
        public ObservableCollection<MailFolder> LoadFolders(string imapAccountPath)
        {
            var root = new ObservableCollection<MailFolder>();
            LoadFolderLevel(imapAccountPath, root);
            return root;
        }

        private void LoadFolderLevel(string path, ObservableCollection<MailFolder> target)
        {
            // mbox Dateien
            foreach (var file in Directory.GetFiles(path))
            {
                if (file.EndsWith(".msf"))
                {
                    continue;
                }

                target.Add(new MailFolder
                {
                    Name = Path.GetFileName(file),
                    FilePath = file
                });
            }

            // Unterordner (.sbd)
            foreach (var dir in Directory.GetDirectories(path, "*.sbd"))
            {
                var folder = new MailFolder
                {
                    Name = Path.GetFileNameWithoutExtension(dir)
                };

                LoadFolderLevel(dir, folder.Children);
                target.Add(folder);
            }
        }
    }
}