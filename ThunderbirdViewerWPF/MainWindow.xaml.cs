namespace ThunderbirdViewerWPF
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Mail;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using MimeKit;

    using Windows.Networking.Vpn;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Services
        private readonly ThunderbirdProfileService _profileService = new();
        private readonly ThunderbirdAccountService _accountService = new();
        private readonly ImapFolderService _imapFolderService = new();
        private readonly MboxParserService _mboxParser = new();
        private readonly MboxParserServiceAsync _mboxParserAsync = new();

        // Backing fields
        private ThunderbirdProfile _selectedProfile;
        private ThunderbirdAccount _selectedAccount;
        private MailFolder _selectedFolder;
        private MailMessageModel _selectedMessage;

        private string _searchText;
        private List<MailMessageModel> _allMessages = new();

        private CancellationTokenSource _loadCts;
        private bool _isLoading;
        private int _progressValue;
        private int _progressMaximum = 100;

        public MainWindow()
        {
            this.InitializeComponent();

            this.Profiles = new ObservableCollection<ThunderbirdProfile>(this._profileService.GetProfiles());
            this.Accounts = new ObservableCollection<ThunderbirdAccount>();
            this.Messages = new ObservableCollection<MailMessageModel>();

            WeakEventManager<Window, RoutedEventArgs>.AddHandler(this, "Loaded", OnLoaded);
            WeakEventManager<Window, CancelEventArgs>.AddHandler(this, "Closing", OnWindowClosing);

            this.LoadProfiles();

            this.WindowTitel = "Thunderbird Email Viewer";
            DataContext = this;
        }

        public ObservableCollection<MailFolder> ImapFolders { get; } = new();
        public ObservableCollection<ThunderbirdProfile> Profiles { get; }
        public ObservableCollection<ThunderbirdAccount> Accounts { get; }
        public ObservableCollection<MailMessageModel> Messages { get; }

        private string _WindowTitel;

        public string WindowTitel
        {
            get { return _WindowTitel; }
            set
            {
                if (_WindowTitel != value)
                {
                    _WindowTitel = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => this._searchText;
            set
            {
                this._searchText = value;
                this.OnPropertyChanged();
                this.ApplySearch();
            }
        }

        public ThunderbirdProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();

                    LoadAccounts();
                }
            }
        }

        public ThunderbirdAccount SelectedAccount
        {
            get => this._selectedAccount;
            set
            {
                if (this._selectedAccount != value)
                {
                    this._selectedAccount = value;
                    this.OnPropertyChanged();

                    this.LoadImapFolders();
                }
            }
        }

        public MailFolder SelectedFolder
        {
            get => this._selectedFolder;
            set
            {
                if (this._selectedFolder != value)
                {
                    this._selectedFolder = value;
                    this.OnPropertyChanged();

                    //this.LoadMessagesFromFolder();
                    _ = LoadMessagesFromFolderAsync();
                }
            }
        }

        public MailMessageModel SelectedMessage
        {
            get => this._selectedMessage;
            set
            {
                this._selectedMessage = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => this._isLoading;
            set
            {
                this._isLoading = value;
                this.OnPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get => this._progressValue;
            set
            {
                this._progressValue = value;
                this.OnPropertyChanged();
            }
        }

        public int ProgressMaximum
        {
            get => this._progressMaximum;
            set
            {
                this._progressMaximum = value;
                this.OnPropertyChanged();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            WeakEventManager<Button, RoutedEventArgs>.AddHandler(BtnCloseApplication, "Click", OnCloseApplication);
            WeakEventManager<Button, RoutedEventArgs>.AddHandler(BtnAttachmentOpen, "Click", OnAttachmentOpen);
        }

        private void OnCloseApplication(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            MessageBoxResult msgYN = MessageBox.Show("Wollen Sie die Anwendung beenden?", "Beenden", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msgYN == MessageBoxResult.Yes)
            {
                App.ApplicationExit();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void LoadProfiles()
        {
            this.Profiles.Clear();
            foreach (var profile in this._profileService.GetProfiles())
            {
                this.Profiles.Add(profile);
            }
        }

        private void LoadAccounts()
        {
            this.Accounts.Clear();
            this.ImapFolders.Clear();
            this.Messages.Clear();

            if (this.SelectedProfile == null)
            {
                return;
            }

            foreach (var acc in this._accountService.GetAccounts(SelectedProfile.Path))
            {
                Accounts.Add(acc);
            }
        }

        private void LoadImapFolders()
        {
            this.ImapFolders.Clear();
            this.Messages.Clear();

            if (this.SelectedAccount == null)
            {
                return;
            }

            foreach (var folder in this._imapFolderService.LoadFolders(SelectedAccount.Path))
            {
                this.ImapFolders.Add(folder);
            }
        }

        private void LoadMessagesFromFolder()
        {
            this.Messages.Clear();
            this._allMessages.Clear();

            if (this.SelectedFolder == null || string.IsNullOrEmpty(this.SelectedFolder.FilePath))
            {
                return;
            }

            bool isSentFolder =
                this.SelectedFolder.Name.Contains("sent", StringComparison.OrdinalIgnoreCase) ||
                this.SelectedFolder.Name.Contains("gesendet", StringComparison.OrdinalIgnoreCase);

            this._allMessages = this._mboxParser.Parse(this.SelectedFolder.FilePath)
                .Select(m => 
                {m.IsSent = isSentFolder;
                    return m;
                }).OrderByDescending(m => m.Date).ToList();

            this.ApplySearch();
        }

        private void ApplySearch()
        {
            this.Messages.Clear();

            foreach (var mail in _allMessages.Where(MatchesSearch))
            {
                this.Messages.Add(mail);
            }
        }

        private bool MatchesSearch(MailMessageModel mail)
        {
            if (string.IsNullOrWhiteSpace(this.SearchText))
            {
                return true;
            }

            return
                (mail.Subject?.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (mail.From?.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (mail.Body?.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase) == true);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainWindow @this)
            {
                @this.SelectedFolder = e.NewValue as MailFolder;
            }
        }

        private void OnAttachmentOpen(object? sender, RoutedEventArgs e)
        {
            if (this.SelectedMessage == null || this.SelectedMessage.HasAttachments == false)
            {
                return;
            }

            if (this.SelectedMessage.Attachments.Count == 0)
            {
                return;
            }

            string fileName = this.SelectedMessage.Attachments[0].FileName;
            byte[] content = this.SelectedMessage.Attachments[0].Content;

            var tempFile = Path.Combine(Path.GetTempPath(), fileName);

            File.WriteAllBytes(tempFile, content);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }

        private async Task LoadMessagesFromFolderAsync()
        {
            // vorherigen Ladevorgang abbrechen
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();

            Messages.Clear();
            _allMessages.Clear();

            if (SelectedFolder == null || string.IsNullOrEmpty(SelectedFolder.FilePath))
                return;

            bool isSentFolder =
                SelectedFolder.Name.Contains("sent", StringComparison.OrdinalIgnoreCase) ||
                SelectedFolder.Name.Contains("gesendet", StringComparison.OrdinalIgnoreCase);

            try
            {
                IsLoading = true;

                var mails = await _mboxParserAsync.ParseAsync(
                    SelectedFolder.FilePath,
                    isSentFolder,
                    _loadCts.Token);

                _allMessages = mails
                    .OrderByDescending(m => m.Date)
                    .ToList();

                ApplySearch();
            }
            catch (OperationCanceledException)
            {
                // bewusst ignorieren (Ordnerwechsel)
            }
            finally
            {
                IsLoading = false;
            }
        }


        #region INotifyPropertyChanged implementierung
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null)
            {
                return;
            }

            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }
        #endregion INotifyPropertyChanged implementierung

    }

    public class MboxParserServiceAsync
    {
        public Task<List<MailMessageModel>> ParseAsync(string mboxFile, bool isSentFolder, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var result = new List<MailMessageModel>();

                using var stream = File.OpenRead(mboxFile);
                var parser = new MimeParser(stream, MimeFormat.Mbox);

                while (!parser.IsEndOfStream)
                {
                    token.ThrowIfCancellationRequested();

                    var message = parser.ParseMessage();

                    var model = ConvertMessage(message, isSentFolder);
                    result.Add(model);
                }

                return result;
            }, token);
        }

        private MailMessageModel ConvertMessage(MimeMessage message, bool isSent)
        {
            var model = new MailMessageModel
            {
                From = message.From.ToString(),
                To = message.To.ToString(),
                Subject = message.Subject,
                Date = message.Date.LocalDateTime,
                Body = message.TextBody ?? message.HtmlBody,
                IsSent = isSent
            };

            foreach (var attachment in message.Attachments.OfType<MimePart>())
            {
                using var ms = new MemoryStream();
                attachment.Content.DecodeTo(ms);

                model.Attachments.Add(new MailAttachment
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType.MimeType,
                    Size = ms.Length,
                    Content = ms.ToArray()
                });
            }

            return model;
        }
    }
}