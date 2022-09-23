using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core
{
    public abstract class Game<TGame> : Actor where TGame : Game<TGame>
    {
        private static TGame current;

        /// <summary>
        /// Gets the current <typeparamref name="TGame"/> instance.
        /// </summary>
        public static TGame Current
        {
            get
            {
                EnsureInstance();
                return current;
            }
        }

        protected override void OnAwake()
        {
            Initialize();
        }

        /// <summary>
        /// Ensures that a <see cref="TGame"/> instance has been created.
        /// </summary>
        protected static void EnsureInstance()
        {
            if (current) return;
            var gameGO = new GameObject("[Game]", typeof(TGame));
            current = gameGO.GetComponent<TGame>();
            DontDestroyOnLoad(gameGO);
        }
    }
}
