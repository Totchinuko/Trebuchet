﻿using SteamKit2;
using SteamKit2.Internal;

/// GNU GENERAL PUBLIC LICENSE // Version 2, June 1991
/// Copyright (C) 2023 Totchinuko https://github.com/Totchinuko
/// Full license text: LICENSE.txt at the project root

namespace Trebuchet
{
    public partial class SteamSession
    {
        private CallbackManager _callbackManager;

        private Config _config;

        private bool _isRunning;

        public SteamSession(Config config)
        {
            _config = config;
            Client = new SteamClient();
            _callbackManager = new CallbackManager(Client);
            User = Client.GetHandler<SteamUser>() ?? throw new Exception("Could not get SteamUser Handler.");
            Apps = Client.GetHandler<SteamApps>() ?? throw new Exception("Could not get SteamApps Handler.");
            Content = Client.GetHandler<SteamContent>() ?? throw new Exception("Could not get SteamContent Handler.");
            var steamUnifiedMessages = Client.GetHandler<SteamUnifiedMessages>() ?? throw new Exception("Could not get SteamUnifiedMessage Handler.");
            steamPublishedFile = steamUnifiedMessages.CreateService<IPublishedFile>();
            ContentDownloader = new ContentDownloader(this, _config, _config.ServerAppID);

            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _callbackManager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
        }

        public event EventHandler<SteamClient.ConnectedCallback>? Connected;

        public event EventHandler<SteamClient.DisconnectedCallback>? Disconnected;

        public event EventHandler<SteamUser.LoggedOnCallback>? LoggedOn;

        public event EventHandler? TimedOut;

        public SteamApps Apps { get; }

        public SteamClient Client { get; }

        public SteamContent Content { get; }

        public ContentDownloader ContentDownloader { get; }

        public SteamUser User { get; }

        public void Connect()
        {
            _isRunning = true;
            Task.Run(CallbackLoop);
            Log.Write("Connecting to steam...", LogSeverity.Info);
            Client.Connect();
        }

        public void Disconnect()
        {
            ContentDownloader.Shutdown();
            Client.Disconnect();
        }

        private void CallbackLoop()
        {
            while (_isRunning)
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Log.Write("Connected.", LogSeverity.Info);
            Log.Write("Login as Anonymous...", LogSeverity.Info);
            connectTime = DateTime.Now;
            Connected?.Invoke(this, callback);
            User.LogOnAnonymous();
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            _isRunning = false;
            Disconnected?.Invoke(this, callback);
            Log.Write("Disconnected from steam.", LogSeverity.Info);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Log.Write($"Login failed: {callback.Result}", LogSeverity.Error);
                Client.Disconnect();
                return;
            }
            Log.Write("Login successful.", LogSeverity.Info);
            LoggedOn?.Invoke(this, callback);
            this.currentSessionIndex++;
        }
    }
}