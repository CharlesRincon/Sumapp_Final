using UnityEngine;
using TMPro;
using Networking.Models;
using System.Collections.Generic;
using System.Text;

namespace Networking.UI
{
    public class CardDebugUI : MonoBehaviour
    {
        [Header("Databases")]
        public CardDatabase cardDatabase;
        public ProjectDatabase projectDatabase;

        [Header("Event Card Panel References")]
        public GameObject eventCardPanel;
        public TextMeshProUGUI eventTitleText;
        public TextMeshProUGUI eventNameText;
        public TextMeshProUGUI eventLoreText;
        public TextMeshProUGUI eventEffectText;

        [Header("Project Decision Panel References")]
        public GameObject projectDecisionPanel;
        public TextMeshProUGUI projectTitleText;
        public TextMeshProUGUI projectNameText;
        public TextMeshProUGUI projectPriceText;
        public TextMeshProUGUI projectBodyText;

        [Header("Debug Controls")]
        public TextMeshProUGUI currentAssetLabel;
        
        private List<CardDefinition> _allCards = new List<CardDefinition>();
        private List<ProjectDefinition> _allProjects = new List<ProjectDefinition>();
        
        private int _currentCardIndex = 0;
        private int _currentProjectIndex = 0;
        private bool _browsingProjects = false;

        private void Start()
        {
            // Initialize lists from databases via reflection since we don't have public accessors for the arrays
            if (cardDatabase != null)
            {
                var cardsField = typeof(CardDatabase).GetField("_cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cardsField != null)
                {
                    var cards = (CardDefinition[])cardsField.GetValue(cardDatabase);
                    if (cards != null) _allCards.AddRange(cards);
                }
            }

            if (projectDatabase != null)
            {
                var projectsField = typeof(ProjectDatabase).GetField("_projects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (projectsField != null)
                {
                    var projects = (ProjectDefinition[])projectsField.GetValue(projectDatabase);
                    if (projects != null) _allProjects.AddRange(projects);
                }
            }

            HideAll();
            UpdateBrowserLabel();
        }

        public void ToggleMode()
        {
            _browsingProjects = !_browsingProjects;
            UpdateBrowserLabel();
        }

        public void NextAsset()
        {
            if (_browsingProjects)
            {
                if (_allProjects.Count == 0) return;
                _currentProjectIndex = (_currentProjectIndex + 1) % _allProjects.Count;
            }
            else
            {
                if (_allCards.Count == 0) return;
                _currentCardIndex = (_currentCardIndex + 1) % _allCards.Count;
            }
            UpdateBrowserLabel();
        }

        public void PreviousAsset()
        {
            if (_browsingProjects)
            {
                if (_allProjects.Count == 0) return;
                _currentProjectIndex = (_currentProjectIndex - 1 + _allProjects.Count) % _allProjects.Count;
            }
            else
            {
                if (_allCards.Count == 0) return;
                _currentCardIndex = (_currentCardIndex - 1 + _allCards.Count) % _allCards.Count;
            }
            UpdateBrowserLabel();
        }

        private void UpdateBrowserLabel()
        {
            if (currentAssetLabel == null) return;

            if (_browsingProjects)
            {
                if (_allProjects.Count > 0)
                    currentAssetLabel.text = $"Project: {_allProjects[_currentProjectIndex].DisplayName} ({_currentProjectIndex + 1}/{_allProjects.Count})";
                else
                    currentAssetLabel.text = "No Projects found";
            }
            else
            {
                if (_allCards.Count > 0)
                    currentAssetLabel.text = $"Card: {_allCards[_currentCardIndex].DisplayName} ({_currentCardIndex + 1}/{_allCards.Count})";
                else
                    currentAssetLabel.text = "No Cards found";
            }
        }

        public void SimulateScan()
        {
            HideAll();

            if (_browsingProjects)
            {
                if (_allProjects.Count == 0) return;
                ShowProject(_allProjects[_currentProjectIndex]);
            }
            else
            {
                if (_allCards.Count == 0) return;
                ShowCard(_allCards[_currentCardIndex]);
            }
        }

        public void HideAll()
        {
            if (eventCardPanel != null) eventCardPanel.SetActive(false);
            if (projectDecisionPanel != null) projectDecisionPanel.SetActive(false);
        }

        private void ShowCard(CardDefinition card)
        {
            if (eventCardPanel == null) return;

            if (eventNameText != null) eventNameText.text = card.DisplayName;
            if (eventLoreText != null) eventLoreText.text = card.LoreText;
            if (eventEffectText != null) eventEffectText.text = card.EffectDescription;
            
            eventCardPanel.SetActive(true);
            LogCardEffects(card);
        }

        private void ShowProject(ProjectDefinition project)
        {
            if (projectDecisionPanel == null) return;

            if (projectNameText != null) projectNameText.text = project.DisplayName;
            if (projectPriceText != null) projectPriceText.text = $"Price: {project.Price}";
            
            if (projectBodyText != null)
            {
                string desc = project.Description;
                projectBodyText.text = 
                    (string.IsNullOrWhiteSpace(desc) ? "" : $"{desc}\n\n") +
                    "Zone: (Simulated Andean)\n" +
                    $"Water / round: {project.BaseWaterPerRound}\n" +
                    $"Money / round: {project.BaseMoneyPerRound}";
            }

            projectDecisionPanel.SetActive(true);
            LogProjectEffects(project);
        }

        private void LogCardEffects(CardDefinition card)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[DEBUG] Simulating Card Scan: {card.DisplayName}");
            sb.AppendLine($"- ID: {card.CardId}");
            
            if (card.WaterDelta != 0) sb.AppendLine($"- Water Delta: {card.WaterDelta}");
            if (card.MoneyDelta != 0) sb.AppendLine($"- Money Delta: {card.MoneyDelta}");
            if (card.BasinDelta != 0) sb.AppendLine($"- Basin Delta: {card.BasinDelta}");
            
            if (card.AllPlayersWaterDelta != 0) sb.AppendLine($"- GLOBAL Water: {card.AllPlayersWaterDelta}");
            if (card.AllPlayersMoneyDelta != 0) sb.AppendLine($"- GLOBAL Money: {card.AllPlayersMoneyDelta}");
            
            if (card.IsWeatherCard) sb.AppendLine($"- WEATHER: {card.WeatherTag} ({card.WeatherDurationRounds} rounds)");
            if (card.IsDroughtEvent) sb.AppendLine("- EVENT: Drought Active");
            if (card.IsDeforestationEvent) sb.AppendLine($"- EVENT: Deforestation ({card.DeforestationProjectMoneyPercentPenalty}%)");
            
            if (card.TeleportMode != TeleportMode.None) 
                sb.AppendLine($"- TELEPORT: {card.TeleportMode} to {card.SelfMoveToTile}/{card.TeleportTargetTileType}");

            if (card.RequiresDecision)
            {
                sb.AppendLine("- REQUIRES DECISION:");
                sb.AppendLine($"  A: {card.DecisionChoiceA.Label} (W:{card.DecisionChoiceA.WaterDelta}, M:{card.DecisionChoiceA.MoneyDelta})");
                sb.AppendLine($"  B: {card.DecisionChoiceB.Label} (W:{card.DecisionChoiceB.WaterDelta}, M:{card.DecisionChoiceB.MoneyDelta})");
            }

            Debug.Log(sb.ToString());
        }

        private void LogProjectEffects(ProjectDefinition project)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[DEBUG] Simulating Project Scan: {project.DisplayName}");
            sb.AppendLine($"- ID: {project.ProjectId}");
            sb.AppendLine($"- Price: {project.Price}");
            sb.AppendLine($"- Base Income: W:{project.BaseWaterPerRound}, M:{project.BaseMoneyPerRound}");
            sb.AppendLine($"- Passive Behaviours: {project.PassiveBehaviours}");
            
            Debug.Log(sb.ToString());
        }
    }
}