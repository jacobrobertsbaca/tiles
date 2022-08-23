using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tiles.Core
{
    /// <summary>
    /// The base <see cref="MonoBehaviour"/> utilizing dependent initialization.
    /// </summary>
    /// <typeparam name="TGame">The specific <see cref="Game{T}"/> type, which is also the first actor to be initialized</typeparam>
    /// <remarks>
    /// By default, all actors are initialized after <typeparamref name="TGame"/> is initialized.
    /// This behaviour can be changed by overriding <see cref="Actor{TGame}.OnAwake"/>.
    /// </remarks>
    public abstract class Actor<TGame> : MonoBehaviour, IInitializable
        where TGame : Game<TGame>
    {
        #region Initialization
        /// <summary>
        /// Whether this actor has finished initializing
        /// </summary>
        public bool IsInit => initStatus == InitStatus.Initialized;
        public InitStatus InitStatus => initStatus;
        private InitStatus initStatus;
        private Queue<Action> initQueue;

        public void Initialize()
        {
            if (initStatus != InitStatus.Uninitialized) return;
            initStatus = InitStatus.Initializing;
            if (OnInitialize()) FinishInit();
        }

        /// <summary>
        /// Schedules work to be run once this actor has initialized
        /// </summary>
        /// <param name="initAction">A callback to be run on initialization</param>
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

        /// <summary>
        /// Schedules an object to be initialized once this actor has initialized
        /// </summary>
        /// <param name="initializable">The object to be initialized</param>
        public void OnInitialized(IInitializable initializable)
        {
            if (initializable is null) return;
            if (initializable.InitStatus != InitStatus.Uninitialized) return;
            OnInitialized(initializable.Initialize);
        }

        /// <summary>
        /// When returning <c>false</c> from <see cref="OnInitialize"/>,
        /// call this method at a later point to mark initialization as completed and
        /// run any enqueued initialization callbacks.
        /// </summary>
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

        /// <summary>
        /// Override to implement initialization logic for this actor.
        /// </summary>
        /// <returns><c>true</c> if initialization completed, or <c>false</c> if initialization will be completed at a later point.
        /// If returning <c>false</c>, make sure to call <see cref="FinishInit"/> once initialization has completed.</returns>
        protected virtual bool OnInitialize() { return true; }

        #endregion

        #region Unity Messages

        /// <summary>
        /// Override to add behaviour to run on Awake.
        /// </summary>
        /// <remarks>
        /// This should primarily be used for scheduling initialization order.
        /// Initialization logic should be implemented in <see cref="OnInitialize"/>
        /// </remarks>
        protected virtual void OnAwake() { Game<TGame>.Current.OnInitialized(this); }
        private void Awake() => OnAwake();

        /// <summary>
        /// Override to add behaviour to run on Start.
        /// </summary>
        /// <remarks>
        /// This should primarily be used for scheduling initialization order.
        /// Initialization logic should be implemented in <see cref="OnInitialize"/>
        /// </remarks>
        protected virtual void OnStart() { }
        private void Start() => OnStart();

        #endregion
    }

    public static class ActorExtensions
    {
        /// <summary>
        /// Schedules work to be run after all actors have been initialized.
        /// </summary>
        /// <typeparam name="TGame">The game type.</typeparam>
        /// <param name="actors">A collection of actors. Elements may be null.</param>
        /// <param name="initAction">A callback to be run on initialization</param>
        public static void OnInitialized<TGame>(this IEnumerable<Actor<TGame>> actors, Action initAction)
            where TGame : Game<TGame>
        {
            if (initAction is null) return;

            int numActors = 0;
            int numInited = 0;

            foreach (var actor in actors)
            {
                if (actor) numActors++;
            }

            if (numActors == 0)
            {
                initAction();
                return;
            }

            void OnInit()
            {
                if (++numInited == numActors) initAction();
            }

            foreach (var actor in actors)
            {
                if (actor) actor.OnInitialized(OnInit);
            }
        }

        /// <summary>
        /// Schedules an object to be initialized once all actors have been initialized
        /// </summary>
        /// <typeparam name="TGame">The game type.</typeparam>
        /// <param name="actors">A collection of actors. Elements may be null.</param>
        /// <param name="initializable">The object to be initialized</param>
        public static void OnInitialized<TGame>(this IEnumerable<Actor<TGame>> actors, IInitializable initializable)
            where TGame : Game<TGame>
        {
            if (initializable is null) return;
            if (initializable.InitStatus != InitStatus.Uninitialized) return;
            actors.OnInitialized(initializable.Initialize);
        }
    }
}