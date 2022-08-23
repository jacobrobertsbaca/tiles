using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles
{
    public class Game : Core.Game<Game>
    {
        [RuntimeInitializeOnLoadMethod]
        private static void CreateGame() => EnsureInstance();

        protected override bool Initialize()
        {
            Debug.Log("Initialized Game");
            return true;
        }
    }
}
