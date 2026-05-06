using System.Collections;
using System.Collections.Generic;
using Fusion;
using Networking.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.UI
{
    public class TriviaUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _triviaPanel;

        [Header("Content")]
        [SerializeField] private TMP_Text _questionText;
        [SerializeField] private Button[] _answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] _answerButtonTexts = new TMP_Text[4];
        [SerializeField] private TMP_Text _feedbackText;

        [Header("Data")]
        [SerializeField] private TriviaDatabase _triviaDatabase;

        [Header("Settings")]
        [SerializeField] private float _feedbackDisplaySeconds = 2f;

        private bool _questionShown;
        private bool _answered;
        private bool _seenSubPanelActive;
        private Coroutine _feedbackCoroutine;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void HidePanel()
        {
            _questionShown = false;
            _answered = false;
            _seenSubPanelActive = false;

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }

            if (_triviaPanel != null)
                _triviaPanel.SetActive(false);
        }

        public void Refresh(NetworkRunner runner, GameObject turnNotificationPanel)
        {
            if (runner == null) return;

            var localData = Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            if (localData == null || !localData.IsAwaitingTrivia)
            {
                if (_triviaPanel != null && _triviaPanel.activeSelf && !_answered)
                    HidePanel();
                return;
            }

            // Wait until the turn sub-panel has been shown and then dismissed
            bool subPanelActive = turnNotificationPanel != null && turnNotificationPanel.activeSelf;
            if (subPanelActive)
            {
                _seenSubPanelActive = true;
                return;
            }

            if (!_seenSubPanelActive)
                return;

            if (!_questionShown)
                ShowQuestion(runner);
        }

        // ─── Private ──────────────────────────────────────────────────────────────

        private void ShowQuestion(NetworkRunner runner)
        {
            if (_triviaDatabase == null)
            {
                Debug.LogWarning("[TriviaUIController] No TriviaDatabase assigned.");
                return;
            }

            var question = _triviaDatabase.GetRandom();
            if (question == null) return;

            _questionShown = true;
            _answered = false;

            // Show question text and re-activate buttons
            if (_questionText != null)
            {
                _questionText.gameObject.SetActive(true);
                _questionText.text = question.QuestionText;
            }

            if (_feedbackText != null)
            {
                _feedbackText.gameObject.SetActive(false);
                _feedbackText.text = string.Empty;
            }

            // Build shuffled answer list (Fisher-Yates)
            var answers = new List<(string text, bool correct)>
            {
                (question.CorrectAnswer, true),
                (question.WrongAnswer1, false),
                (question.WrongAnswer2, false),
                (question.WrongAnswer3, false),
            };

            for (int i = answers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (answers[i], answers[j]) = (answers[j], answers[i]);
            }

            // Assign answers to buttons
            for (int i = 0; i < _answerButtons.Length; i++)
            {
                if (_answerButtons[i] == null) continue;

                _answerButtons[i].gameObject.SetActive(true);
                _answerButtons[i].onClick.RemoveAllListeners();

                if (i < answers.Count)
                {
                    bool isCorrect = answers[i].correct;
                    if (_answerButtonTexts != null && i < _answerButtonTexts.Length && _answerButtonTexts[i] != null)
                        _answerButtonTexts[i].text = answers[i].text;

                    _answerButtons[i].onClick.AddListener(() => OnAnswerClicked(isCorrect, runner));
                }
            }

            if (_triviaPanel != null)
                _triviaPanel.SetActive(true);
        }

        private void OnAnswerClicked(bool isCorrect, NetworkRunner runner)
        {
            if (_answered) return;
            _answered = true;

            // Hide question and all buttons, show only feedback
            if (_questionText != null)
                _questionText.gameObject.SetActive(false);

            for (int i = 0; i < _answerButtons.Length; i++)
            {
                if (_answerButtons[i] != null)
                    _answerButtons[i].gameObject.SetActive(false);
            }

            if (_feedbackText != null)
            {
                _feedbackText.text = isCorrect ? "¡Respuesta correcta!" : "Respuesta incorrecta";
                _feedbackText.gameObject.SetActive(true);
            }

            // Send RPC to host
            var localData = Managers.GameManager.Instance?.GetPlayerData(runner.LocalPlayer, runner);
            localData?.RPC_RequestTriviaAnswer(isCorrect);

            // Auto-hide after delay
            if (_feedbackCoroutine != null)
                StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(HideAfterFeedback());
        }

        private IEnumerator HideAfterFeedback()
        {
            yield return new WaitForSeconds(_feedbackDisplaySeconds);
            HidePanel();
        }
    }
}
