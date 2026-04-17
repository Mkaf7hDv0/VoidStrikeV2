using DiscordRPC;
using DiscordRPC.Logging;
using System;

namespace VoidStrike.Services
{
    public class DiscordService : IDisposable
    {
        private DiscordRpcClient? _client;
        private static DiscordService? _instance;
        public static DiscordService Instance => _instance ??= new DiscordService();

        private bool _isEnabled = false;
        public bool IsEnabled => _isEnabled;

        private DiscordService() { }

        public void Initialize()
        {
            if (_client != null && _client.IsInitialized) return;

            // Using Elite Application ID
            _client = new DiscordRpcClient("1331281635389608039");
            _client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            _client.Initialize();
            _isEnabled = true;

            SetStatus("Master Core", "Ready for Deployment");
        }

        public void SetStatus(string state, string details, bool showTimer = false)
        {
            if (_client == null || !_client.IsInitialized) return;

            try
            {
                var presence = new RichPresence()
                {
                    Details = details,
                    State = state,
                    Assets = new Assets()
                    {
                        LargeImageKey = "void_main", // Application Icon in Discord Portal
                        LargeImageText = "VoidStrike Elite v12.0",
                        SmallImageKey = "status_active",
                        SmallImageText = "System Online"
                    }
                };

                if (showTimer)
                {
                    presence.Timestamps = Timestamps.Now;
                }

                _client.SetPresence(presence);
            }
            catch { }
        }

        public void Deinitialize()
        {
            if (_client != null)
            {
                _client.ClearPresence();
                _client.Dispose();
                _client = null;
            }
            _isEnabled = false;
        }

        public void Dispose()
        {
            Deinitialize();
        }
    }
}
