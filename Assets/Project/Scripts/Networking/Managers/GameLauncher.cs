using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Networking.Managers
{
    public class GameLauncher : MonoBehaviour
    {
        public GameObject LauncherPrefab;

        public void Launch(GameMode _gameMode, string _room)
        {
            Networking.Services.FusionLauncher launcher = FindFirstObjectByType<Networking.Services.FusionLauncher>();
            if (launcher == null)
                launcher = Instantiate(LauncherPrefab).GetComponent<Networking.Services.FusionLauncher>();

            LevelManager lm = FindFirstObjectByType<LevelManager>();
            if (lm != null)
            {
                lm.Launcher = launcher;
            }

            launcher.Launch(_gameMode, _room, lm);
        }
    }
}
