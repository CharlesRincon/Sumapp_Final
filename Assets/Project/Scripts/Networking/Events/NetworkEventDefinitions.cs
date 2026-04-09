using UnityEngine;
using FusionUtilsEvents;

namespace Networking.Events
{
    /// <summary>
    /// Centralized registry of all network event ScriptableObjects.
    /// Allows easy inspection and wiring in the Inspector.
    /// 
    /// Architecture: Event asset hub for the networking system.
    /// All FusionEvent references are stored here for consistency.
    /// Services and UI reference these from here instead of finding them.
    /// </summary>
    [CreateAssetMenu(menuName = "Networking/Event Definitions")]
    public class NetworkEventDefinitions : ScriptableObject
    {
        [Header("Connection Events")]
        public FusionEvent OnPlayerJoinedEvent;
        public FusionEvent OnPlayerLeftEvent;
        public FusionEvent OnConnectionStatusChangedEvent;
        public FusionEvent OnDisconnectedEvent;

        [Header("Session Events")]
        public FusionEvent OnGameStateChangedEvent;
        public FusionEvent OnEnteredLobbyEvent;
        public FusionEvent OnShutdownEvent;

        [Header("Player Session Events")]
        public FusionEvent OnPlayerSessionCachedEvent;
        public FusionEvent OnPlayerOfflineEvent;
        public FusionEvent OnPlayerDataSpawnedEvent;

        [Header("Scene Events")]
        public FusionEvent OnSceneLoadStartEvent;
        public FusionEvent OnSceneLoadCompleteEvent;

        [Header("Room Events")]
        public FusionEvent OnRoomPropertiesChangedEvent;
        public FusionEvent OnPlayerKickedEvent;

        [Header("Round Slice Events")]
        public FusionEvent OnRoundStartedEvent;
        public FusionEvent OnRoundEndedEvent;
        public FusionEvent OnTurnStartedEvent;
        public FusionEvent OnDiceRolledEvent;
        public FusionEvent OnPlayerMovedEvent;
        public FusionEvent OnPlayerWaterChangedEvent;
        public FusionEvent OnBasinStateChangedEvent;

        /// <summary>
        /// Gets a singleton instance of these definitions.
        /// Can be loaded from Resources or assigned via Inspector.
        /// </summary>
        private static NetworkEventDefinitions _instance;

        public static NetworkEventDefinitions Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<NetworkEventDefinitions>("NetworkEventDefinitions");
                    if (_instance == null)
                    {
                        Debug.LogError("[NetworkEventDefinitions] Failed to load from Resources. Check that the asset exists at Resources/NetworkEventDefinitions.asset");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Validates that all events are assigned.
        /// </summary>
        public bool ValidateAllEventsAssigned()
        {
            return OnPlayerJoinedEvent != null
                && OnPlayerLeftEvent != null
                && OnConnectionStatusChangedEvent != null
                && OnDisconnectedEvent != null
                && OnGameStateChangedEvent != null
                && OnEnteredLobbyEvent != null
                && OnShutdownEvent != null
                && OnPlayerSessionCachedEvent != null
                && OnPlayerOfflineEvent != null
                && OnSceneLoadStartEvent != null
                && OnSceneLoadCompleteEvent != null
                && OnRoomPropertiesChangedEvent != null
                && OnPlayerKickedEvent != null
                && OnRoundStartedEvent != null
                && OnRoundEndedEvent != null
                && OnTurnStartedEvent != null
                && OnDiceRolledEvent != null
                && OnPlayerMovedEvent != null
                && OnPlayerWaterChangedEvent != null
                && OnBasinStateChangedEvent != null;
        }
    }
}
