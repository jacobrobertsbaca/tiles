using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tiles.Core
{
    public abstract class Actor : MonoBehaviour, IInitializable
    {
        #region Initialization
        InitStatus IInitializable.InitStatus => initStatus;
        private InitStatus initStatus;
        private Queue<Action> initQueue;

        void IInitializable.Initialize()
        {
            if (initStatus != InitStatus.Uninitialized) return;
            initStatus = InitStatus.Initializing;
            if (Initialize()) FinishInit();
        }

        public void OnInitialized(Action initAction)
        {
            if (initAction is null) return;
            if (initStatus == InitStatus.Initialized)
            {
                initAction();
                return;
            }

            initQueue ??= new Queue<Action>();
            initQueue.Enqueue(initAction);
        }

        public void OnInitialized(IInitializable initializable)
        {
            if (initializable is null) return;
            if (initializable.InitStatus != InitStatus.Uninitialized) return;
            OnInitialized(initializable.Initialize);
        }

        protected void FinishInit()
        {
            if (initStatus == InitStatus.Initialized) return;
            if (initStatus == InitStatus.Uninitialized)
            {
                Debug.LogWarning($"{nameof(FinishInit)} called on actor before {nameof(IInitializable.Initialize)}", this);
                return;
            }

            if (initQueue is not null)
            {
                while (initQueue.Count > 0) initQueue.Dequeue()();
            }
            initStatus = InitStatus.Initialized;
        }

        protected virtual bool Initialize() { return true; }

        /// <summary>
        /// Gets the underlying <see cref="IInitializable"/> of this <see cref="Actor"/>
        /// </summary>
        /// <returns>An <see cref="IInitializable"/></returns>
        public IInitializable GetInitializable() => this; 

        #endregion

        #region Unity Messages

        private void Awake() => OnAwake();
        protected virtual void OnAwake() { }
        private void Start() => OnStart();
        protected virtual void OnStart() { }

        #endregion
    }

    public static class ActorExtensions
    {
        public static void OnInitialized(this IEnumerable<Actor> actors, Action initAction)
        {
            if (initAction is null) return;

            int numActors = actors.Count();
            int numInited = 0;
            void OnInit()
            {
                if (++numInited == numActors) initAction();
            }

            foreach (var actor in actors) actor.OnInitialized(OnInit);
        }

        public static void OnInitialized(this IEnumerable<Actor> actors, IInitializable initializable)
        {
            if (initializable is null) return;
            if (initializable.InitStatus != InitStatus.Uninitialized) return;
            actors.OnInitialized(initializable.Initialize);
        }
    }
}