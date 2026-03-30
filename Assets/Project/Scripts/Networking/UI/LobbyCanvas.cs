using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Threading.Tasks;

namespace Networking.UI
{
    public class LobbyCanvas : MonoBehaviour
    {
        private GameMode _gameMode;

        public string Nickname = "Player";
        public Networking.Managers.GameLauncher Launcher;

        public FusionEvent OnPlayerJoinedEvent;
        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnShutdownEvent;
        public FusionEvent OnPlayerDataSpawnedEvent;

        [Space]
        [SerializeField] private GameObject _initPanel;
        [SerializeField] private GameObject _lobbyPanel;
        [SerializeField] private TextMeshProUGUI _lobbyPlayerText;
        [SerializeField] private TextMeshProUGUI _lobbyRoomName;
        [SerializeField] private Button _startButton;
        [Space]
        [SerializeField] private GameObject _modeButtons;
        [SerializeField] private TMP_InputField _nickname;
        [SerializeField] private TMP_InputField _room;

        private void OnEnable()
        {
            OnPlayerJoinedEvent.RegisterResponse(ShowLobbyCanvas);
            OnShutdownEvent.RegisterResponse(ResetCanvas);
            OnPlayerLeftEvent.RegisterResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RegisterResponse(UpdateLobbyList);
        }

        private void OnDisable()
        {
            OnPlayerJoinedEvent.RemoveResponse(ShowLobbyCanvas);
            OnShutdownEvent.RemoveResponse(ResetCanvas);
            OnPlayerLeftEvent.RemoveResponse(UpdateLobbyList);
            OnPlayerDataSpawnedEvent.RemoveResponse(UpdateLobbyList);
        }

        //Called from button
        public void SetGameMode(int gameMode)
        {
            Networking.Managers.GameManager.Instance.SetGameState(Networking.Managers.GameManager.GameState.Lobby);
            _gameMode = (GameMode)gameMode;
            _modeButtons.SetActive(false);
            _nickname.transform.parent.gameObject.SetActive(true);
        }

        //Called from button
        public void StartLauncher()
        {
            Launcher = FindFirstObjectByType<Networking.Managers.GameLauncher>();
            Nickname = _nickname.text;
            PlayerPrefs.SetString("Nick", Nickname);
            Launcher.Launch(_gameMode, _room.text);
            _nickname.transform.parent.gameObject.SetActive(false);
        }

        //Called from button
        public void ExitGame()
        {
            Networking.Managers.GameManager.Instance.ExitGame();
        }

        //Called from button
        public void LeaveLobby()
        {
            _ = LeaveLobbyAsync();
        }

        //Called from button
        public void StartGame()
        {
            Networking.Services.FusionNetworkService.LocalRunner.SessionInfo.IsOpen = false;
            Networking.Services.FusionNetworkService.LocalRunner.SessionInfo.IsVisible = false;
            // TODO: Add scene loading when needed
        }

        private async Task LeaveLobbyAsync()
        {
            if (Networking.Services.FusionNetworkService.LocalRunner.IsServer)
            {
                CloseLobby();
            }
            await Networking.Services.FusionNetworkService.LocalRunner?.Shutdown();
        }

        public void CloseLobby()
        {
            foreach(var player in Networking.Services.FusionNetworkService.LocalRunner.ActivePlayers)
            {
                if (player != Networking.Services.FusionNetworkService.LocalRunner.LocalPlayer)
                    Networking.Services.FusionNetworkService.LocalRunner.Disconnect(player);
            }
        }

        private void ResetCanvas(PlayerRef player, NetworkRunner runner)
        {
            _initPanel.SetActive(true);
            _modeButtons.SetActive(true);
            _lobbyPanel.SetActive(false);
            _startButton.gameObject.SetActive(runner.IsServer);
        }

        public void ShowLobbyCanvas(PlayerRef player, NetworkRunner runner)
        {
            _initPanel.SetActive(false);
            _lobbyPanel.SetActive(true);
        }

        public void UpdateLobbyList(PlayerRef playerRef, NetworkRunner runner)
        {
            _startButton.gameObject.SetActive(runner.IsServer);
            string players = default;
            string isLocal;
            foreach(var player in runner.ActivePlayers)
            {
                isLocal = player == runner.LocalPlayer ? " (You)" : string.Empty;
                var playerData = Networking.Managers.GameManager.Instance.GetPlayerData(player, runner);
                if (playerData != null)
                {
                    players += playerData.Nick + isLocal + " \n";
                }
            }
            _lobbyPlayerText.text = players;
            _lobbyRoomName.text = $"Room: {runner.SessionInfo.Name}";
        }
    }
}
