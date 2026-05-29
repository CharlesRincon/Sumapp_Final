using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

namespace Networking.Managers
{
    public class LevelManager : NetworkSceneManagerDefault
    {
        [HideInInspector]
        public Networking.Services.FusionLauncher Launcher;
<<<<<<< HEAD
        [SerializeField] private LoadingManager _loadingManager;
=======
        // [SerializeField] private LoadingManager _loadingManager;
>>>>>>> projects-logic
        private Scene _loadedScene;

        public void ResetLoadedScene()
        {
<<<<<<< HEAD
            _loadingManager.ResetLastLevelsIndex();
=======
            // _loadingManager.ResetLastLevelsIndex();
>>>>>>> projects-logic
            _loadedScene = default;
        }

        protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters sceneParams)
        {
<<<<<<< HEAD
            _loadingManager.StartLoadingScreen();
=======
            // _loadingManager.StartLoadingScreen();
>>>>>>> projects-logic
            GameManager.Instance.SetGameState(GameManager.GameState.Loading);
            Launcher.SetConnectionStatus(Networking.Services.FusionLauncher.ConnectionStatus.Loading, "");
            yield return new WaitForSeconds(1.0f);
            yield return base.LoadSceneCoroutine(sceneRef, sceneParams);
            Launcher.SetConnectionStatus(Networking.Services.FusionLauncher.ConnectionStatus.Loaded, "");
            yield return new WaitForSeconds(1f);
<<<<<<< HEAD
            _loadingManager.FinishLoadingScreen();
=======
            // _loadingManager.FinishLoadingScreen();
>>>>>>> projects-logic
        }
    }
}
