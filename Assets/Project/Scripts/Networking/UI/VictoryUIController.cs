using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.UI
{
    public class VictoryUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private Transform _victoryRankingContainer;
        [SerializeField] private GameObject _victoryRankingEntryPrefab;
        [SerializeField] private Button _victoryReturnButton;

        private GameObject _minigameReadyPanel;
        private Button _rollDiceButton;
        private Button _openVuforiaButton;

        private bool _victoryShown;

        public GameObject Panel => _victoryPanel;

        private void Awake()
        {
            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.AddListener(OnVictoryReturnClicked);
            }
        }

        private void OnDestroy()
        {
            if (_victoryReturnButton != null)
            {
                _victoryReturnButton.onClick.RemoveListener(OnVictoryReturnClicked);
            }
        }

        public void Configure(
            GameObject minigameReadyPanel,
            Button rollDiceButton,
            Button openVuforiaButton)
        {
            _minigameReadyPanel = minigameReadyPanel;
            _rollDiceButton = rollDiceButton;
            _openVuforiaButton = openVuforiaButton;
        }

        public void ResetVictoryState()
        {
            _victoryShown = false;
        }

        public void HidePanel()
        {
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(false);
            }
        }

        public void RefreshVictoryPanel(NetworkRunner runner)
        {
            if (_victoryPanel == null || _victoryShown || runner == null) return;

            var gm = Networking.Managers.GameManager.Instance;
            if (gm == null) return;

            var localData = gm.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || !localData.IsGameOver) return;

            _victoryShown = true;

            if (_minigameReadyPanel != null) _minigameReadyPanel.SetActive(false);
            if (_rollDiceButton != null) _rollDiceButton.interactable = false;
            if (_openVuforiaButton != null) _openVuforiaButton.interactable = false;

            if (localData.IsDefeat)
            {
                PopulateDefeatMessage();
            }
            else
            {
                PopulateVictoryRanking(runner, gm);
            }

            _victoryPanel.SetActive(true);
            Debug.Log($"[VictoryUIController] {(localData.IsDefeat ? "Defeat" : "Victory")} panel shown.");
        }

        public void OnVictoryReturnClicked()
        {
            Debug.Log("[VictoryUIController] Victory return button clicked - ending session.");
            Networking.Managers.GameManager.Instance?.ExitSession();
        }

        private void PopulateVictoryRanking(NetworkRunner runner, Networking.Managers.GameManager gm)
        {
            if (_victoryRankingContainer == null || _victoryRankingEntryPrefab == null) return;

            foreach (Transform child in _victoryRankingContainer)
            {
                Destroy(child.gameObject);
            }

            var ranked = runner.ActivePlayers
                .Select(player => gm.GetPlayerData(player, runner))
                .Where(data => data != null)
                .OrderByDescending(data => data.WaterAmount)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                var data = ranked[i];
                var entry = Instantiate(_victoryRankingEntryPrefab, _victoryRankingContainer);
                entry.name = $"Rank_{i + 1}";

                var label = entry.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"#{i + 1}. {data.Nick} - Water: {data.WaterAmount}";
                }
            }
        }

        private void PopulateDefeatMessage()
        {
            if (_victoryRankingContainer == null || _victoryRankingEntryPrefab == null) return;

            foreach (Transform child in _victoryRankingContainer)
            {
                Destroy(child.gameObject);
            }

            var entry = Instantiate(_victoryRankingEntryPrefab, _victoryRankingContainer);
            entry.name = "DefeatMessage";

            var label = entry.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "<color=#FF4444><b>Everyone Lost!</b></color>\nThe basin has been destroyed.";
            }
        }
    }
}