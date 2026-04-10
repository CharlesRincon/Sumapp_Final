using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Fusion;

namespace FusionUtilsEvents
{
    /// <summary>
    /// Event system for Fusion network callbacks.
    /// Decouples network events from UI and business logic through ScriptableObject-based event bus.
    /// 
    /// Architecture: Observers register action delegates with this event. When Raise() is called,
    /// all registered actions are invoked with the current PlayerRef and NetworkRunner context.
    /// 
    /// This enables: loose coupling between network systems and UI, easy testability, 
    /// and serialized event references in Inspector for wiring gameplay systems.
    /// </summary>
    [CreateAssetMenu(menuName = "Networking/Fusion Event")]
    public class FusionEvent : ScriptableObject
    {
        public List<Action<PlayerRef, NetworkRunner>> Responses = new List<Action<PlayerRef, NetworkRunner>>();

        /// <summary>
        /// Raises the event, invoking all registered responses with the given context.
        /// </summary>
        public void Raise(PlayerRef player = default, NetworkRunner runner = null)
        {
            for (int i = 0; i < Responses.Count; i++)
            {
                try
                {
                    Responses[i].Invoke(player, runner);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error in FusionEvent '{name}': {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Registers an action to be called when this event is raised.
        /// </summary>
        public void RegisterResponse(Action<PlayerRef, NetworkRunner> response)
        {
            if (response == null) return;
            if (!Responses.Contains(response))
            {
                Responses.Add(response);
            }
        }

        /// <summary>
        /// Unregisters an action from this event.
        /// </summary>
        public void RemoveResponse(Action<PlayerRef, NetworkRunner> response)
        {
            if (response == null) return;

            // Remove all occurrences to clean up any accidental duplicate subscriptions.
            while (Responses.Contains(response))
            {
                Responses.Remove(response);
            }
        }

        /// <summary>
        /// Clears all registered responses. Used for cleanup.
        /// </summary>
        public void ClearAllResponses()
        {
            Responses.Clear();
        }
    }
}
