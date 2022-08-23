using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core
{
    public abstract class Game<T> : Actor where T : Game<T>
    {
        private static T current;
        public static T Current
        {
            get
            {
                EnsureInstance();
                return current;
            }
        }

        protected override void OnAwake()
        {
            GetInitializable().Initialize();
        }

        protected static void EnsureInstance()
        {
            if (current) return;
            var gameGO = new GameObject("[Game]", typeof(T));
            current = gameGO.GetComponent<T>();
            DontDestroyOnLoad(gameGO);
        }
    }
}
