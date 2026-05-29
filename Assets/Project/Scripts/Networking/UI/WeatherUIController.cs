using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using FusionUtilsEvents;
using System.Collections.Generic;
using System.Linq;
using Networking.Managers;
using Networking.Models;

namespace Networking.UI
{
    public class WeatherUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.UI.Image _cardIllustration;
        [SerializeField] private TextMeshProUGUI _cardDescription;
        [SerializeField] private UnityEngine.UI.Slider _timerBar;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private UnityEngine.UI.Slider _nextCardBar;
        [SerializeField] private Transform _scoreContainer;
        [SerializeField] private GameObject _playerCardPrefab;
        [SerializeField] private GameObject _leaderboardPlayerPrefab;
        [SerializeField] private GameObject _leaderboardPanel;
        [SerializeField] private Transform _leaderboardContainer;
        [SerializeField] private UnityEngine.UI.Button _elNinoButton;
        [SerializeField] private UnityEngine.UI.Button _laNinaButton;
        [SerializeField] private RectTransform _cardPanel;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private TMP_FontAsset _mainFont;

        [Header("Events")]
        [SerializeField] private FusionEvent OnGameEndEvent;

        private WeatherMinigameManager _manager;
        private NetworkRunner _runner;
        private int _lastCardIndex = -1;
        private Dictionary<PlayerRef, TextMeshProUGUI> _scoreTexts = new Dictionary<PlayerRef, TextMeshProUGUI>();
        private bool _hasAnsweredCurrentCard = false;

        private void Start()
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            _manager = FindFirstObjectByType<WeatherMinigameManager>();
            
            if (_leaderboardPanel != null)
                _leaderboardPanel.SetActive(false);

            if (_elNinoButton != null)
                _elNinoButton.onClick.AddListener(() => OnButtonClick(true));
            
            if (_laNinaButton != null)
                _laNinaButton.onClick.AddListener(() => OnButtonClick(false));

            if (_feedbackText != null)
                _feedbackText.gameObject.SetActive(false);

            if (_cardPanel != null)
                _cardPanel.localScale = Vector3.zero;
        }

        private void OnEnable()
        {
            if (OnGameEndEvent == null)
                OnGameEndEvent = Resources.Load<FusionEvent>("Events/OnGameEndEvent");

            if (OnGameEndEvent != null) 
                OnGameEndEvent.RegisterResponse(OnGameEnd);
        }

        private void OnDisable()
        {
            if (OnGameEndEvent != null) 
                OnGameEndEvent.RemoveResponse(OnGameEnd);
        }

        private bool _scoresInitialized = false;

        private void Update()
        {
            if (_manager == null)
            {
                _manager = FindFirstObjectByType<WeatherMinigameManager>();
                return;
            }

            if (!_scoresInitialized && _runner != null && _runner.ActivePlayers.Count() > 0)
            {
                InitializeScoreUI();
                _scoresInitialized = true;
            }

            if (!_manager.IsGameActive()) return;

            float rem = _manager.GetRemainingTime();
            if (_timerBar != null) _timerBar.value = rem / 60f; 
            if (_timerText != null) _timerText.text = Mathf.CeilToInt(rem).ToString();

            if (_nextCardBar != null)
            {
                float timeInCycle = (60f - rem) % 5f;
                _nextCardBar.value = 1f - (timeInCycle / 5f);
            }

            int currentIdx = _manager.GetCurrentCardIndex();
            if (currentIdx != _lastCardIndex)
            {
                _lastCardIndex = currentIdx;
                _hasAnsweredCurrentCard = false;
                UpdateCardUI();
                SetButtonsInteractable(true);
            }

            UpdateScoreUI();
        }

        private void UpdateCardUI()
        {
            var card = _manager.GetCurrentCard();
            
            if (_cardPanel != null)
            {
                LeanTween.cancel(_cardPanel.gameObject);
                LeanTween.scale(_cardPanel, Vector3.zero, 0.25f).setEaseInBack().setOnComplete(() => {
                    if (card != null)
                    {
                        if (_cardIllustration != null) _cardIllustration.sprite = card.Illustration;
                        if (_cardDescription != null) _cardDescription.text = card.Description;
                        LeanTween.scale(_cardPanel, Vector3.one, 0.35f).setEaseOutBack();
                    }
                    else
                    {
                        if (_cardIllustration != null) _cardIllustration.sprite = null;
                        if (_cardDescription != null) _cardDescription.text = "¡Finalizando!";
                        LeanTween.scale(_cardPanel, Vector3.one, 0.35f).setEaseOutBack();
                    }
                });
            }
        }

        private void OnButtonClick(bool isElNino)
        {
            if (_hasAnsweredCurrentCard || _runner == null) return;
            _hasAnsweredCurrentCard = true;
            SetButtonsInteractable(false);

            GameObject btn = isElNino ? _elNinoButton.gameObject : _laNinaButton.gameObject;
            LeanTween.scale(btn, Vector3.one * 1.15f, 0.1f).setLoopPingPong(1);

            _manager.RPC_SubmitAnswer(_runner.LocalPlayer, isElNino);
        }

        public void OnAnswerResult(bool isCorrect, int points)
        {
            Debug.Log($"[WeatherUIController] OnAnswerResult received: Correct={isCorrect}, Points={points}");
            
            if (_feedbackText == null) 
            {
                Debug.LogError("[WeatherUIController] _feedbackText is MISSING! Re-searching in hierarchy...");
                _feedbackText = GameObject.Find("FeedbackText")?.GetComponent<TextMeshProUGUI>();
                if (_feedbackText == null) return;
            }

            // Set content and color
            _feedbackText.text = isCorrect ? $"¡CORRECTO!\n<size=120%><color=yellow>+{points}</color></size>" : "¡INCORRECTO!";
            _feedbackText.color = isCorrect ? Color.green : Color.red;
            
            // Prepare for animation
            _feedbackText.gameObject.SetActive(true);
            _feedbackText.transform.SetAsLastSibling();
            _feedbackText.transform.localScale = Vector3.zero;

            // Kill any previous animations on this object
            LeanTween.cancel(_feedbackText.gameObject);
            
            // 1. Pop up with Bounce
            LeanTween.scale(_feedbackText.gameObject, Vector3.one, 0.4f).setEaseOutBack();
            
            // 2. Add a little shake if incorrect, or punch if correct
            if (isCorrect)
            {
                LeanTween.moveLocalY(_feedbackText.gameObject, _feedbackText.transform.localPosition.y + 20f, 0.2f).setLoopPingPong(1);
            }
            else
            {
                LeanTween.moveLocalX(_feedbackText.gameObject, 10f, 0.05f).setLoopPingPong(3);
            }

            // 3. Auto hide after delay
            LeanTween.delayedCall(_feedbackText.gameObject, 1.5f, () => {
                if (this == null || _feedbackText == null) return;
                LeanTween.scale(_feedbackText.gameObject, Vector3.zero, 0.3f)
                    .setEaseInBack()
                    .setOnComplete(() => {
                        _feedbackText.gameObject.SetActive(false);
                    });
            });
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_elNinoButton != null) _elNinoButton.interactable = interactable;
            if (_laNinaButton != null) _laNinaButton.interactable = interactable;
        }

        private void InitializeScoreUI()
        {
            if (_runner == null || _scoreContainer == null || _playerCardPrefab == null) return;
            
            foreach (Transform child in _scoreContainer) Destroy(child.gameObject);
            _scoreTexts.Clear();

            foreach (var p in _runner.ActivePlayers)
            {
                var entry = Instantiate(_playerCardPrefab, _scoreContainer);
                entry.transform.localScale = Vector3.one;
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null) text = entry.GetComponent<TextMeshProUGUI>();

                if (text != null)
                {
                    var data = GameManager.Instance.GetPlayerData(p, _runner);
                    text.text = $"{(data != null ? (string)data.Nick : "P" + p.PlayerId)}: 0";
                    if (_mainFont != null) text.font = _mainFont;
                    _scoreTexts[p] = text;
                }
            }
        }

        private void UpdateScoreUI()
        {
            foreach (var kvp in _scoreTexts)
            {
                var data = GameManager.Instance.GetPlayerData(kvp.Key, _runner);
                if (data != null)
                {
                    string newText = $"{data.Nick}: {data.MinigameClickCount}";
                    if (kvp.Value.text != newText)
                    {
                        kvp.Value.text = newText;
                        LeanTween.cancel(kvp.Value.gameObject);
                        kvp.Value.transform.localScale = Vector3.one;
                        LeanTween.scale(kvp.Value.gameObject, Vector3.one * 1.2f, 0.15f).setLoopPingPong(1);
                    }
                }
            }
        }

        private void OnGameEnd(PlayerRef player, NetworkRunner runner)
        {
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(true);
                DisplayLeaderboard();
            }
        }

        private void DisplayLeaderboard()
        {
            if (_leaderboardContainer == null || _manager == null) return;
            
            GameObject prefabToUse = _leaderboardPlayerPrefab != null ? _leaderboardPlayerPrefab : _playerCardPrefab;
            if (prefabToUse == null) return;

            foreach (Transform child in _leaderboardContainer) Destroy(child.gameObject);
            
            var leaderboard = _manager.GetLeaderboard();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = Instantiate(prefabToUse, _leaderboardContainer);
                entry.transform.localScale = Vector3.one;
                var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null) text = entry.GetComponent<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = $"#{i + 1} {leaderboard[i].name}: {leaderboard[i].score}";
                    if (_mainFont != null) text.font = _mainFont;
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }
}
