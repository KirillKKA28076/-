using UnityEngine;

namespace WarmBread
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Time.timeScale = 1f;
            WorldArt.PrepareFont();

            var game = gameObject.AddComponent<GameSession>();
            game.Initialize();

            var queueObject = new GameObject("Очередь");
            var queue = queueObject.AddComponent<QueueManager>();
            queue.Initialize(game);
            game.Queue = queue;

            var atmosphere = gameObject.AddComponent<Atmosphere>();
            var ui = gameObject.AddComponent<GameUI>();

            var worldObject = new GameObject("Мир");
            var world = worldObject.AddComponent<WorldBuilder>();
            world.Build(game, ui, atmosphere);

            var playerObject = new GameObject("Продавец");
            playerObject.transform.position = new Vector3(0f, .16f, -.25f);
            var player = playerObject.AddComponent<PlayerController>();
            player.Initialize(game);

            atmosphere.Initialize(game, world.Sun, player.View);
            ui.Initialize(game, player, atmosphere);
        }
    }
}
