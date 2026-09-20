using UnityEngine;

namespace WarmBread
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate=60;Time.timeScale=1;
            WorldArt.PrepareFont();
            var game=gameObject.AddComponent<GameSession>();game.Initialize();
            var queueObject=new GameObject("Очередь");var queue=queueObject.AddComponent<QueueManager>();queue.Initialize(game);game.Queue=queue;
            var mood=gameObject.AddComponent<Atmosphere>();var ui=gameObject.AddComponent<GameUI>();
            var world=new GameObject("Мир").AddComponent<WorldBuilder>();world.Build(game,ui,mood);
            var playerObject=new GameObject("Продавец");playerObject.transform.position=new Vector3(0,.16f,-.25f);
            var player=playerObject.AddComponent<PlayerController>();player.Initialize(game);
            mood.Initialize(game,world.Sun,player.View);mood.SetVolume(.55f);ui.Initialize(game,player,mood);
        }
    }
}
