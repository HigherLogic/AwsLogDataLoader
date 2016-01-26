using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AwsLogDataLoader
{
    public static class WindowsAccountInformation
    {
        public static string CurrentAccountName => WindowsIdentity.GetCurrent().Name;
    }

    public class AwsLogDataLoaderViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Location> _files;
        private bool _isWindowsAuthentication = true, _isBusy = false;
        private string _authenticationAccount, _accountPassword, _sqlServerInstance, _sqlServerDatabase, _sqlTableName, _statusText;
        private int _progress, _maximumItems;

        public AwsLogDataLoaderViewModel()
        {
            AddFileCommand = new RelayCommand(OnAddFileCommand) { CanExecuteValue = true };
            AddDirectoryCommand = new RelayCommand(OnAddDirectoryCommand) { CanExecuteValue = true };
            RemoveCommand = new RelayCommand(OnRemoveCommand) { CanExecuteValue = true };
            LoadDataCommand = new RelayCommand(OnLoadDataCommand);
            _files = new ObservableCollection<Location>();
            _files.CollectionChanged += delegate {
                UpdateConnectButton();
            };
            PropertyChanged += delegate {
                UpdateConnectButton();
            };
            SqlServerInstance = "(local)";
        }

        public ObservableCollection<Location> Files
        {
            get
            {
                return _files;
            }
            set
            {
                _files = value;
                OnPropertyChanged();
            }
        }

        public bool IsWindowsAuthentication
        {
            get
            {
                return _isWindowsAuthentication;
            }
            set
            {
                _isWindowsAuthentication = value;
                OnPropertyChanged();

            }
        }

        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string AuthenticationAccount
        {
            get
            {
                return _authenticationAccount;
            }
            set
            {
                _authenticationAccount = value;
                OnPropertyChanged();
            }
        }

        public string SqlServerInstance
        {
            get
            {
                return _sqlServerInstance;
            }
            set
            {
                _sqlServerInstance = value;
                OnPropertyChanged();
            }
        }

        public string SqlServerDatabase
        {
            get
            {
                return _sqlServerDatabase;
            }
            set
            {
                _sqlServerDatabase = value;
                OnPropertyChanged();
            }
        }

        public string AccountPassword
        {
            get
            {
                return _accountPassword;
            }
            set
            {
                _accountPassword = value;
                OnPropertyChanged();
            }
        }

        public string SqlTableName
        {
            get
            {
                return _sqlTableName;
            }
            set
            {
                _sqlTableName = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public int Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public int MaximumItems
        {
            get
            {
                return _maximumItems;
            }
            set
            {
                _maximumItems = value;
                OnPropertyChanged();
            }
        }

        public void UpdateConnectButton()
        {
            var hasCredentials = IsWindowsAuthentication ? true : !String.IsNullOrEmpty(AccountPassword) && !String.IsNullOrEmpty(AuthenticationAccount);
            var validConnection = !String.IsNullOrEmpty(SqlServerInstance) && !String.IsNullOrEmpty(SqlServerDatabase) && hasCredentials;
            LoadDataCommand.CanExecuteValue = hasCredentials && validConnection && Files.Any() && !String.IsNullOrEmpty(SqlTableName);
            LoadDataCommand.OnCanExecuteChanged();
        }


        public RelayCommand AddFileCommand { get; }
        public RelayCommand AddDirectoryCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand LoadDataCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnAddFileCommand(object args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = "Select Log File(s)";
            dialog.Filter = "Log File|*.log|All Files|*.*";
            if (dialog.ShowDialog() == true)
            {
                foreach(var file in dialog.FileNames)
                {
                    var normalized = System.IO.Path.GetFullPath(file);
                    if (!_files.Any(f => f.Type == LocationType.File && string.Equals(normalized, f.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        _files.Add(new Location { Path = file, Type = LocationType.File });
                    }
                }
            }
        }

        public void OnAddDirectoryCommand(object args)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Log File Directory";
                dialog.ShowNewFolderButton = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var normalized = System.IO.Path.GetFullPath(dialog.SelectedPath);
                    if (!_files.Any(f => f.Type == LocationType.Folder && string.Equals(normalized, f.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        _files.Add(new Location { Path = normalized, Type = LocationType.Folder });
                    }
                }
            }
        }

        private void OnRemoveCommand(object args)
        {
            var itemsToRemove = Files.Where(f => f.IsSelected).ToList();
            foreach(var itemToRemove in itemsToRemove)
            {
                Files.Remove(itemToRemove);
            }
        }

        private async void OnLoadDataCommand(object args)
        {
            IsBusy = true;
            var dataLoader = IsWindowsAuthentication
                ? (DataLoaderBase)new WindowsAuthenticatedLoader(SqlServerInstance, SqlServerDatabase)
                : new SqlAuthenticatedLoader(SqlServerInstance, SqlServerDatabase, AuthenticationAccount, AccountPassword);
            var valid = await dataLoader.ValidateCredentials();
            if (valid)
            {
                var formatFile = await dataLoader.GetPathToFormatFile();
                await dataLoader.PrepareTable(SqlTableName);
                var expandToFiles = _files
                    .Where(f => f.Type == LocationType.File).Select(location => location.Path)
                    .Union(_files.Where(f => f.Type == LocationType.Folder).SelectMany(
                            location => System.IO.Directory.GetFiles(location.Path, "*.log", System.IO.SearchOption.AllDirectories)
                        )).ToList();
                int counter = 0;
                MaximumItems = expandToFiles.Count;
                foreach(var file in expandToFiles)
                {
                    Progress = ++counter;
                    StatusText = $"Importing file {counter} of {expandToFiles.Count}.";
                    await dataLoader.LoadFile(SqlTableName, formatFile, file);
                }
                StatusText = "Load complete";
            }
            else
            {
                StatusText = "Could not connect with credentials.";
            }
            IsBusy = false;
        }

        public void OnPropertyChanged([CallerMemberName]string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
