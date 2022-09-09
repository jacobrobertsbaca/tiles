using UnityEngine;

namespace Tiles
{
    public class Actor : Core.Actor<Game> {}

    public class Game : Core.Game<Game>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void CreateGame() => EnsureInstance();

        protected override bool OnInitialize()
        {
            Debug.Log("Initialized Game");
            return true;
        }
    }
}
