using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using FusionUtilsEvents;

namespace Networking.Models
{
    /// <summary>
    /// Exact replication of OtherGame PlayerData.
    /// Network-synchronized player data (nickname, instance reference).
    /// </summary>
    public class PlayerSessionData : NetworkBehaviour
    {
        [Networked]
        public NetworkString<_16> Nick { get; set; }
        
        [Networked]
        public NetworkObject Instance { get; set; }

        public FusionEvent OnPlayerDataSpawnedEvent;

        private ChangeDetector _changeDetector;

        [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
        public void RPC_SetNick(string nick)
        {
            Nick = nick;
        }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
            if (Object.HasInputAuthority)
            {
                string nickName = PlayerPrefs.GetString("Nick", string.Empty);
                RPC_SetNick(string.IsNullOrEmpty(nickName) ? $"Player {Object.InputAuthority.AsIndex}" : nickName);
            }

            DontDestroyOnLoad(this);
            Runner.SetPlayerObject(Object.InputAuthority, Object);
            OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);

            if (Object.HasStateAuthority)
            {
                Networking.Managers.GameManager.Instance.SetPlayerDataObject(Object.InputAuthority, this);
            }
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Nick):
                        OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
                        break;
                }
            }
        }
    }
}

