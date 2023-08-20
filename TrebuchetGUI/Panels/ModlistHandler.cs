﻿using SteamWorksWebAPI;
using SteamWorksWebAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace Trebuchet
{
    public class ModlistHandler : Panel
    {
        private const string FetchManifests = "FetchManifests";
        private const string FetchModlist = "FetchModlist";
        private TrulyObservableCollection<ModFile> _modlist = new TrulyObservableCollection<ModFile>();
        private string _modlistURL = string.Empty;
        private FileSystemWatcher? _modWatcher;
        private ModListProfile _profile;
        private ObservableCollection<string> _profiles = new ObservableCollection<string>();
        private WorkshopSearch? _searchWindow;
        private string _selectedModlist = string.Empty;
        private SteamSession _steam;
        private SteamWidget _steamWidget;

        public ModlistHandler(Config config, UIConfig uiConfig, SteamSession steam, SteamWidget steamWidget) : base(config, uiConfig)
        {
            _steamWidget = steamWidget;
            _steam = steam;
            LoadPanel();
        }

        public ICommand CreateModlistCommand => new SimpleCommand(OnModlistCreate);

        public ICommand DeleteModlistCommand => new SimpleCommand(OnModlistDelete);

        public ICommand DuplicateModlistCommand => new SimpleCommand(OnModlistDuplicate);

        public ICommand ExploreLocalCommand => new SimpleCommand(OnExploreLocal);

        public ICommand ExploreWorkshopCommand => new SimpleCommand(OnExploreWorkshop);

        public ICommand ExportToJsonCommand => new SimpleCommand(OnExportToJson);

        public ICommand ExportToTxtCommand => new SimpleCommand(OnExportToTxt);

        public ICommand FetchCommand => new TaskBlockedCommand(OnFetchClicked, true, FetchModlist);

        public ICommand ImportFromFileCommand => new SimpleCommand(OnImportFromFile);

        public ICommand ImportFromTextCommand => new SimpleCommand(OnImportFromText);

        public object ItemTemplate => Application.Current.Resources["ModlistItems"];

        public ICommand ModFilesDownloadCommand => new TaskBlockedCommand(OnModFilesDownload, true, SteamWidget.SteamTask, Dashboard.GameTask);

        public TrulyObservableCollection<ModFile> Modlist
        {
            get => _modlist;
            set
            {
                _modlist = value;
                OnModlistChanged();
            }
        }

        public string ModlistURL
        {
            get => _modlistURL;
            set
            {
                _modlistURL = value;
                OnModlistURLChanged();
            }
        }

        public ObservableCollection<string> Profiles { get => _profiles; set => _profiles = value; }

        public ICommand RefreshModlistCommand => new TaskBlockedCommand(OnModlistRefresh, true, FetchManifests);

        public string SelectedModlist
        {
            get => _selectedModlist;
            set
            {
                _selectedModlist = value;
                OnSelectionChanged();
            }
        }

        public override DataTemplate Template => (DataTemplate)Application.Current.Resources["ModlistEditor"];

        public override bool CanExecute(object? parameter)
        {
            return _config.IsInstallPathValid;
        }

        public override void RefreshPanel()
        {
            OnCanExecuteChanged();
            LoadPanel();
        }

        private void FetchJsonList(UriBuilder builder)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;
            var cts = App.TaskBlocker.Set(FetchModlist, 15 * 1000);
            Task.Run(async () =>
            {
                try
                {
                    var result = await Tools.DownloadModList(builder.ToString(), cts.Token);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _profile.Modlist = ModListProfile.ParseModList(result.Modlist).ToList();
                        LoadModlist();
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Failed", $"Could not download the file. ({ex.Message})").ShowDialog());
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => App.TaskBlocker.Release(FetchModlist));
                }
            }, cts.Token);
        }

        private void FetchSteamCollection(UriBuilder builder)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;

            var query = HttpUtility.ParseQueryString(builder.Query);
            var id = query.Get("id");
            if (id == null || !ulong.TryParse(id, out ulong collectionID))
            {
                new ErrorModal("Invalid URL", "The steam URL seems to be missing its ID to be valid.").ShowDialog();
                return;
            }
            var cts = App.TaskBlocker.Set(FetchModlist, 15 * 1000);

            Task.Run(async () =>
            {
                try
                {
                    var result = await SteamRemoteStorage.GetCollectionDetails(new GetCollectionDetailsQuery(collectionID), cts.Token);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        List<string> modlist = new List<string>();
                        foreach (var child in result.CollectionDetails.First().Children)
                            modlist.Add(child.PublishedFileId);
                        _profile.Modlist = modlist;
                        LoadModlist();
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Failed", $"Could not download the collection. ({ex.Message})").ShowDialog());
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => App.TaskBlocker.Release(FetchModlist));
                }
            }, cts.Token);
        }

        private void LoadManifests()
        {
            if (App.TaskBlocker.IsSet(FetchManifests)) return;
            if (_modlist.Count == 0) return;

            IEnumerable<ulong> list =
                from mod in _modlist
                where mod.IsPublished
                select mod.PublishedFileID;

            var cts = App.TaskBlocker.Set(FetchManifests, 15 * 1000);
            Task.Run(async () =>
            {
                try
                {
                    var result = await SteamRemoteStorage.GetPublishedFileDetails(new GetPublishedFileDetailsQuery(list), cts.Token);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var update = from file in _modlist
                                     where file.IsPublished
                                     join details in result.PublishedFileDetails on file.PublishedFileID equals details.PublishedFileID
                                     select new KeyValuePair<ModFile, PublishedFile>(file, details);

                        foreach (var u in update)
                            u.Key.SetManifest(u.Value);
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Modlist", $"Could not download mod details of your modlist. ({ex.Message})").ShowDialog());
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App.TaskBlocker.Release(FetchManifests);
                    });
                }
            }, cts.Token);
        }

        private void LoadModlist()
        {
            _modlist.CollectionChanged -= OnModlistCollectionChanged;
            _modlist.Clear();

            foreach (string mod in _profile.Modlist)
            {
                string path = mod;
                _profile.ResolveMod(ref path);

                if (ulong.TryParse(mod, out var publishedFileID))
                    _modlist.Add(new ModFile(publishedFileID, path));
                else
                    _modlist.Add(new ModFile(path));
            }
            _modlist.CollectionChanged += OnModlistCollectionChanged;
            OnPropertyChanged("Modlist");
            LoadManifests();
        }

        [MemberNotNull("_profile")]
        private void LoadPanel()
        {
            SetupFileWatcher();

            _selectedModlist = _uiConfig.CurrentModlistProfile;
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);

            OnPropertyChanged("SelectedModlist");
            LoadProfile();
            RefreshProfiles();
        }

        [MemberNotNull("_profile")]
        private void LoadProfile()
        {
            _profile = ModListProfile.LoadProfile(_config, ModListProfile.GetPath(_config, _selectedModlist));
            _modlistURL = _profile.SyncURL;
            OnPropertyChanged("ModlistURL");

            LoadModlist();
        }

        private void OnExploreLocal(object? obj)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.DefaultExt = FileType.Pak.extention;
            dialog.AddExtension = true;
            dialog.FileName = "ModArchive";
            dialog.Filter = FileType.Pak.Filter;
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(NativeWindow.GetIWin32Window(Application.Current.MainWindow));
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                _modlist.Add(new ModFile(path));
            }
        }

        private void OnExploreWorkshop(object? obj)
        {
            if (_searchWindow != null) return;
            _searchWindow = new WorkshopSearch(_config);
            _searchWindow.Closing += OnSearchClosing;
            _searchWindow.ModAdded += OnModAdded;
            _searchWindow.Show();
        }

        private void OnExportToJson(object? obj)
        {
            string json = JsonSerializer.Serialize(new ModlistExport { Modlist = _profile.Modlist });
            new ModlistTextImport(json, true, FileType.Json).ShowDialog();
        }

        private void OnExportToTxt(object? obj)
        {
            try
            {
                string content = string.Join("\r\n", _profile.GetResolvedModlist());
                new ModlistTextImport(content, true, FileType.Txt).ShowDialog();
            }
            catch
            {
                new ErrorModal("Error", "Some of the mods path cannot be resolved because the mod file was not found. " +
                    "In order to export your modlist, please unsure that all of the mods are not marked as missing.").ShowDialog();
            }
        }

        private void OnFetchClicked(object? obj)
        {
            if (App.TaskBlocker.IsSet(FetchModlist)) return;
            if (string.IsNullOrEmpty(_modlistURL)) return;

            QuestionModal question = new QuestionModal("Replacement", "This action will replace your modlist, do you wish to continue ?");
            question.ShowDialog();
            if (question.Result != System.Windows.Forms.DialogResult.Yes) return;

            UriBuilder builder = new UriBuilder(_modlistURL);
            if (SteamWorks.SteamCommunityHost == builder.Host)
                FetchSteamCollection(builder);
            else
                FetchJsonList(builder);
        }

        private void OnImportFromFile(object? obj)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.DefaultExt = FileType.Json.extention;
            dialog.AddExtension = true;
            dialog.FileName = "Modlist";
            dialog.Filter = string.Join("|", new string[] { FileType.Json.Filter, FileType.Txt.Filter });
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            System.Windows.Forms.DialogResult result = dialog.ShowDialog(NativeWindow.GetIWin32Window(Application.Current.MainWindow));
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                var ext = Path.GetExtension(dialog.FileName);
                if (ext == FileType.Json.extention)
                    OnImportFromJsonFile(File.ReadAllText(path));
                else if (ext == FileType.Txt.extention)
                    OnImportFromTxtFile(File.ReadAllText(path));
                else
                    new ErrorModal("Wrong Type", "The type of file provided is unsupported.").ShowDialog();
            }
        }

        private void OnImportFromJsonFile(string json)
        {
            ModlistExport? modlist = JsonSerializer.Deserialize<ModlistExport>(json);
            if (modlist == null)
            {
                new ErrorModal("Invalid Json", "Loaded json could not be parsed.");
                return;
            }

            _profile.Modlist = modlist.Modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnImportFromText(object? obj)
        {
            ModlistTextImport import = new ModlistTextImport(string.Empty, false, FileType.Json);
            import.ShowDialog();

            if (import.Canceled) return;

            string text = import.Text;
            List<string>? modlist;
            try
            {
                ModlistExport? export = JsonSerializer.Deserialize<ModlistExport>(text);
                if (export == null)
                    throw new Exception("This is not Json.");
                modlist = export.Modlist;
            }
            catch
            {
                var split = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                modlist = ModListProfile.ParseModList(split).ToList();
            }

            if (modlist == null)
            {
                new ErrorModal("Invalid Modlist", "The modlist could not be parsed.");
                return;
            }

            if (import.Append)
                _profile.Modlist.AddRange(modlist);
            else
                _profile.Modlist = modlist;
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnImportFromTxtFile(string text)
        {
            var split = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            _profile.Modlist = ModListProfile.ParseModList(split).ToList();
            _profile.SaveFile();
            LoadModlist();
        }

        private void OnModAdded(object? sender, WorkshopSearchResult mod)
        {
            if (_modlist.Where((x) => x.IsPublished && x.PublishedFileID == mod.PublishedFileID).Any()) return;
            string path = mod.PublishedFileID.ToString();
            _profile.ResolveMod(ref path);
            ModFile file = new ModFile(mod, path);
            _modlist.Add(file);
            LoadManifests();
        }

        private void OnModFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!ModListProfile.TryParseDirectory2ModID(e.FullPath, out ulong id)) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in _modlist.Where(file => file.PublishedFileID == id))
                {
                    string path = file.PublishedFileID.ToString();
                    _profile.ResolveMod(ref path);
                    file.RefreshFile(path);
                }
            });
        }

        private void OnModFilesDownload(object? obj)
        {
            if (!_steamWidget.CanExecute()) return;

            QuestionModal question = new QuestionModal("Download", "Do you wish to update your modlist ? This might take a while.");
            question.ShowDialog();
            if (question.Result != System.Windows.Forms.DialogResult.Yes) return;

            var cts = _steamWidget.SetTask($"Updating your modlist mods {_selectedModlist}...");

            Task.Run(async () =>
            {
                try
                {
                    await Setup.UpdateMods(_config, _steam, _profile.GetModIDList(), cts);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Write(ex);
                    Application.Current.Dispatcher.Invoke(() => new ErrorModal("Mod update failed", $"Mod update failed. Please check the log for more information. ({ex.Message})").ShowDialog());
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RefreshModFileStatus();
                        _steamWidget.ReleaseTask();
                    });
                }
            });
        }

        private void OnModlistChanged()
        {
            _profile.Modlist.Clear();
            _profile.Modlist.AddRange(
                _modlist.Select(file => file.ToString())
                );
            _profile.SaveFile();
        }

        private void OnModlistCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnModlistChanged();
        }

        private void OnModlistCreate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Create", string.Empty);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This mod list name is already used").ShowDialog();
                return;
            }

            _profile = ModListProfile.CreateProfile(_config, ModListProfile.GetPath(_config, name));
            _profile.SaveFile();
            RefreshProfiles();
            SelectedModlist = name;
        }

        private void OnModlistDelete(object? obj)
        {
            if (string.IsNullOrEmpty(_selectedModlist)) return;
            if (_profile == null) return;

            QuestionModal question = new QuestionModal("Deletion", $"Do you wish to delete the selected modlist {_selectedModlist} ?");
            question.ShowDialog();
            if (question.Result == System.Windows.Forms.DialogResult.Yes)
            {
                _profile.DeleteFile();

                string profile = string.Empty;
                ModListProfile.ResolveProfile(_config, ref profile);
                RefreshProfiles();
                SelectedModlist = profile;
            }
        }

        private void OnModlistDuplicate(object? obj)
        {
            ChooseNameModal modal = new ChooseNameModal("Duplicate", _selectedModlist);
            modal.ShowDialog();
            string name = modal.Name;
            if (string.IsNullOrEmpty(name)) return;
            if (_profiles.Contains(name))
            {
                new ErrorModal("Already Exitsts", "This mod list name is already used").ShowDialog();
                return;
            }

            string path = Path.Combine(ModListProfile.GetPath(_config, name));
            _profile.CopyFileTo(path);
            _profile = ModListProfile.LoadProfile(_config, path);
            _profile.SaveFile();
            RefreshProfiles();
            SelectedModlist = name;
        }

        private void OnModlistRefresh(object? obj)
        {
            LoadModlist();
        }

        private void OnModlistURLChanged()
        {
            _profile.SyncURL = _modlistURL;
            _profile.SaveFile();
            OnPropertyChanged("ModlistURL");
        }

        private void OnSearchClosing(object? sender, CancelEventArgs e)
        {
            if (_searchWindow == null) return;
            _searchWindow.Closing -= OnSearchClosing;
            _searchWindow.ModAdded -= OnModAdded;
            _searchWindow = null;
        }

        private void OnSelectionChanged()
        {
            ModListProfile.ResolveProfile(_config, ref _selectedModlist);
            OnPropertyChanged("SelectedModlist");
            _uiConfig.CurrentModlistProfile = _selectedModlist;
            _uiConfig.SaveFile();
            LoadProfile();
        }

        private void RefreshModFileStatus()
        {
            foreach (ModFile file in _modlist)
            {
                string path = file.PublishedFileID.ToString();
                _profile.ResolveMod(ref path);
                file.RefreshFile(path);
            }
        }

        private void RefreshProfiles()
        {
            _profiles = new ObservableCollection<string>(ModListProfile.ListProfiles(_config));
            OnPropertyChanged("Profiles");
        }

        private void SetupFileWatcher()
        {
            if (_modWatcher != null)
            {
                _modWatcher.Dispose();
                _modWatcher = null;
            }

            string path = Path.Combine(_config.InstallPath, Config.FolderWorkshop, _config.ClientAppID.ToString());
            if (string.IsNullOrWhiteSpace(_config.InstallPath) || !Directory.Exists(_config.InstallPath)) return;
            if (!Directory.Exists(path))
                Tools.CreateDir(path);

            _modWatcher = new FileSystemWatcher(path);
            _modWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            _modWatcher.Changed += OnModFileChanged;
            _modWatcher.Created += OnModFileChanged;
            _modWatcher.Deleted += OnModFileChanged;
            _modWatcher.Renamed += OnModFileChanged;
            _modWatcher.IncludeSubdirectories = false;
            _modWatcher.EnableRaisingEvents = true;
        }
    }
}