using System;
using System.IO;
using System.Threading.Tasks;
using AgentConnect.Updates.Configuration;
using AgentConnect.Updates.Models;
using Newtonsoft.Json;

namespace AgentConnect.Updates.Services
{
    /// <summary>
    /// Service for managing update deferrals.
    /// Persists deferral state to local app data.
    /// </summary>
    public class DeferralService : IDeferralService
    {
        private readonly string _deferralFilePath;
        private DeferralState _currentState;

        public DeferralService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(appData,
                UpdateConstants.AppDataFolderName,
                UpdateConstants.UpdatesFolderName);
            Directory.CreateDirectory(settingsDir);
            _deferralFilePath = Path.Combine(settingsDir, UpdateConstants.DeferralFileName);
        }

        public async Task<DeferralState> GetDeferralStateAsync(string version)
        {
            if (_currentState != null && _currentState.Version == version)
                return _currentState;

            if (!File.Exists(_deferralFilePath))
                return null;

            try
            {
                var json = await Task.Run(() => File.ReadAllText(_deferralFilePath));
                var state = JsonConvert.DeserializeObject<DeferralState>(json);

                if (state?.Version != version)
                    return null;

                _currentState = state;
                return state;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeferUpdateAsync(string version, int maxDeferrals)
        {
            var state = await GetDeferralStateAsync(version) ?? new DeferralState
            {
                Version = version,
                FirstPromptTime = DateTime.UtcNow,
                DeferralCount = 0
            };

            if (state.DeferralCount >= maxDeferrals)
                return false;

            state.DeferralCount++;
            state.LastDeferralTime = DateTime.UtcNow;
            state.DeferUntil = DateTime.UtcNow.AddHours(UpdateConstants.DeferralHours);

            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            await Task.Run(() => File.WriteAllText(_deferralFilePath, json));

            _currentState = state;
            return true;
        }

        public async Task<bool> CanDeferAsync(string version, int maxDeferrals, int minutesUntilForced)
        {
            var state = await GetDeferralStateAsync(version);

            if (state == null)
                return true; // First time seeing this update

            // Check deferral count
            if (state.DeferralCount >= maxDeferrals)
                return false;

            // Check time limit
            var forcedTime = state.FirstPromptTime.AddMinutes(minutesUntilForced);
            if (DateTime.UtcNow >= forcedTime)
                return false;

            return true;
        }

        public async Task ClearDeferralAsync(string version)
        {
            if (File.Exists(_deferralFilePath))
            {
                var state = await GetDeferralStateAsync(version);
                if (state?.Version == version)
                {
                    await Task.Run(() => File.Delete(_deferralFilePath));
                    _currentState = null;
                }
            }
        }
    }
}
