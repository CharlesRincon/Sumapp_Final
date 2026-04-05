using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;

namespace Networking.Managers
{
    /// <summary>
    /// Exact replication of OtherGame GameManager.
    /// Manages game state and player data tracking.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnRunnerShutDownEvent;
        
        private Dictionary<PlayerRef, Networking.Models.PlayerSessionData> _playerData = new Dictionary<PlayerRef, Networking.Models.PlayerSessionData>();

        public enum GameState
        {
            Lobby,
            Playing,
            Loading
        }

        public GameState State { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this.transform.parent.gameObject);
            }
            DontDestroyOnLoad(transform.parent);
        }

        private void OnEnable()
        {
            OnPlayerLeftEvent.RegisterResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RegisterResponse(DisconnectedFromSession);
        }

        private void OnDisable()
        {
            OnPlayerLeftEvent.RemoveResponse(PlayerDisconnected);
            OnRunnerShutDownEvent.RemoveResponse(DisconnectedFromSession);
        }

        public void SetGameState(GameState state)
        {
            State = state;
        }

        public Networking.Models.PlayerSessionData GetPlayerData(PlayerRef player, NetworkRunner runner)
        {
            // Try to get from stored dictionary first (most reliable)
            if (_playerData.ContainsKey(player))
            {
                Debug.Log($"[GameManager.GetPlayerData] Player {player.PlayerId}: Using dictionary reference. MinigameClickCount={_playerData[player].MinigameClickCount}");
                return _playerData[player];
            }

            // Fallback to runner lookup if not in dictionary
            NetworkObject NO;
            if (runner.TryGetPlayerObject(player, out NO))
            {
                Networking.Models.PlayerSessionData data = NO.GetComponent<Networking.Models.PlayerSessionData>();
                Debug.Log($"[GameManager.GetPlayerData] Player {player.PlayerId}: Using runner lookup. MinigameClickCount={data?.MinigameClickCount ?? -1}");
                return data;
            }
            else
            {
                Debug.LogWarning($"[GameManager.GetPlayerData] Player {player.PlayerId}: Not found in dictionary or runner!");
                return null;
            }
        }

        public void PlayerDisconnected(PlayerRef player, NetworkRunner runner)
        {
            if (_playerData.ContainsKey(player))
            {
                if (_playerData[player].Instance != null)
                {
                    runner.Despawn(_playerData[player].Instance);
                }
                runner.Despawn(_playerData[player].Object);
                _playerData.Remove(player);
            }
        }

        public void LeaveRoom()
        {
            _ = LeaveRoomAsync();
        }

        private async Task LeaveRoomAsync()
        {
            await ShutdownRunner();
        }

        private async Task ShutdownRunner()
        {
            if (Networking.Services.FusionNetworkService.LocalRunner != null)
            {
                await Networking.Services.FusionNetworkService.LocalRunner.Shutdown();
            }
            SetGameState(GameState.Lobby);
            _playerData.Clear();
        }

        public void DisconnectedFromSession(PlayerRef player, NetworkRunner runner)
        {
            Debug.Log("Disconnected from the session");
            ExitSession();
        }

        public void ExitSession()
        {
            _ = ShutdownRunner();
            SceneManager.LoadScene(0);
        }

        public void ExitGame()
        {
            _ = ShutdownRunner();
            Application.Quit();
        }

        public void SetPlayerDataObject(PlayerRef objectInputAuthority, Networking.Models.PlayerSessionData playerData)
        {
            if (!_playerData.ContainsKey(objectInputAuthority))
            {
                _playerData.Add(objectInputAuthority, playerData);
            }
        }

        /// <summary>
        /// Get list of available characters (not selected by any player).
        /// Used by character selection UI to display available options.
        /// </summary>
        public List<Networking.Models.CharacterConfig> GetAvailableCharacters(NetworkRunner runner)
        {
            var selectedIds = new HashSet<int>();

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = GetPlayerData(player, runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                {
                    selectedIds.Add(playerData.SelectedCharacterId);
                }
            }

            return CharacterDatabase.Instance.GetAvailableCharacters(selectedIds);
        }

        /// <summary>
        /// Get set of all selected character IDs across all connected players.
        /// </summary>
        public HashSet<int> GetSelectedCharacterIds(NetworkRunner runner)
        {
            var selectedIds = new HashSet<int>();

            foreach (var player in runner.ActivePlayers)
            {
                var playerData = GetPlayerData(player, runner);
                if (playerData != null && playerData.SelectedCharacterId > 0)
                {
                    selectedIds.Add(playerData.SelectedCharacterId);
                }
            }

            return selectedIds;
        }

        /// <summary>
        /// Check if a specific character is available.
        /// </summary>
        public bool IsCharacterAvailable(int characterId, NetworkRunner runner)
        {
            var selectedIds = GetSelectedCharacterIds(runner);
            return CharacterDatabase.Instance.IsCharacterAvailable(characterId, selectedIds);
        }
    }
}
