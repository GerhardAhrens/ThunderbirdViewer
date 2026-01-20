namespace ThunderbirdViewerWPF
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;

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

        // Backing fields
        private ThunderbirdProfile _selectedProfile;
        private ThunderbirdAccount _selectedAccount;
        private MailFolder _selectedFolder;
        private string _searchText;
        private List<MailMessageModel> _allMessages = new();

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

        public ObservableCollection<MailFolder> ImapFolders { get; }
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

                    this.LoadMessagesFromFolder();
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            WeakEventManager<Button, RoutedEventArgs>.AddHandler(BtnCloseApplication, "Click", OnCloseApplication);
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

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainWindow @this)
            {
                @this.SelectedFolder = e.NewValue as MailFolder;
            }
        }
    }
}