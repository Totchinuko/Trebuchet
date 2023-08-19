using SteamKit2;
using System;

namespace GoogGUI
{
    public class SteamHandler
    {
        private CallbackManager _callbackManager;

        public SteamHandler()
        {
            Client = new SteamClient();
            _callbackManager = new CallbackManager(Client);
            User = Client.GetHandler<SteamUser>() ?? throw new Exception("Could not get SteamUser Handler.");
            Apps = Client.GetHandler<SteamApps>() ?? throw new Exception("Could not get SteamApps Handler.");

            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        }

        public SteamApps Apps { get; }

        public SteamClient Client { get; }

        public SteamUser User { get; }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            throw new NotImplementedException();
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            throw new NotImplementedException();
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            throw new NotImplementedException();
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}