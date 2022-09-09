using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Tiles.Core.Events
{
    public class EventDispatcher<T>
    {
        private static readonly ObjectPool<EventContext> contextPool = new(
            () => new EventContext(),
            defaultCapacity: 0,
            maxSize: int.MaxValue
        );

        private static readonly ObjectPool<InvocationList<T>> listPool = new ObjectPool<InvocationList<T>>(
            () => new InvocationList<T>(),
            defaultCapacity: 0,
            maxSize: int.MaxValue
        );

        private static readonly ObjectPool<Stack<Transform>> capturesPool = new ObjectPool<Stack<Transform>>(
            () => new Stack<Transform>(),
            s => s.Clear(),
            defaultCapacity: 0
        );

        private static int dispatchId = 0;
        private readonly Dictionary<Transform, InvocationList<T>> handlerMap = new();
        private int numCaptures = 0;
        private int numSubscribed = 0;
        private Event<T> evt;

        public EventDispatcher(Event<T> evt)
        {
            this.evt = evt;
        }

        public void Dispatch(Transform target, T data)
        {
            if (numSubscribed == 0) return;

            dispatchId++;
            EventContext context = contextPool.Get();
            context.Event = evt;
            context.Target = target;
            context.cancelled = false;
            context.cancelledImmediate = false;

            try
            {
                // Capture phase
                if (numCaptures > 0)
                {
                    context.Phase = ExecutionPhase.Capturing;
                    Stack<Transform> captures = capturesPool.Get();

                    try
                    {
                        Transform root = target.parent;
                        while (root)
                        {
                            captures.Push(root);
                            root = root.parent;
                        }

                        while (captures.Count > 0)
                        {
                            if (!DispatchEvents(captures.Pop(), context, data)) return;
                        }
                    } finally { capturesPool.Release(captures); }
                }

                // Target phase
                context.Phase = ExecutionPhase.Target;
                if (!DispatchEvents(target, context, data)) return;

                // Bubble phase
                if (numSubscribed - numCaptures > 0)
                {
                    context.Phase = ExecutionPhase.Bubbling;
                    target = target.parent;
                    while (target)
                    {
                        if (!DispatchEvents(target, context, data)) return;
                        target = target.parent;
                    }
                }
            }
            finally
            {
                contextPool.Release(context);
            }
        }

        public void Subscribe(Transform target, EventListener<T> handler, Component owner, bool isCapture = false)
        {
            if (handler is null) return;
            if (target is null) throw new ArgumentNullException(nameof(target));
            if (!handlerMap.ContainsKey(target)) handlerMap[target] = listPool.Get();

            InvocationList<T> list = handlerMap[target];
            list.Register(handler, owner, isCapture, ref numCaptures, ref numSubscribed);
        }

        private void Unsubscribe(Transform target, EventListener<T> handler, Component owner, bool removeEmptyLists)
        {
            if (handler is null) return;
            if (target is null) throw new ArgumentNullException(nameof(target));
            if (!handlerMap.ContainsKey(target)) return;
            InvocationList<T> list = handlerMap[target];
            list.Unregister(handler, owner, ref numCaptures, ref numSubscribed);
            if (removeEmptyLists && list.Count == 0)
            {
                listPool.Release(list);
                handlerMap.Remove(target);
            }
        }

        public void Unsubscribe(Transform target, EventListener<T> handler, Component owner)
        {
            Unsubscribe(target, handler, owner, true);
        }

        public void Unsubscribe(EventListener<T> handler, Component owner)
        {
            var toRemove = capturesPool.Get();
            foreach (var transform in handlerMap.Keys)
            {
                Unsubscribe(transform, handler, owner, false);
                if (handlerMap[transform].Count == 0) toRemove.Push(transform);
            }

            while (toRemove.Count > 0)
            {
                var remove = toRemove.Pop();
                listPool.Release(handlerMap[remove]);
                handlerMap.Remove(remove);
            }

            capturesPool.Release(toRemove);
        }

        public void Unsubscribe(Component owner)
        {
            var toRemove = capturesPool.Get();
            foreach (var transform in handlerMap.Keys)
            {
                var list = handlerMap[transform];
                list.Unregister(owner, ref numCaptures, ref numSubscribed);
                if (list.Count == 0) toRemove.Push(transform);
            }

            while (toRemove.Count > 0)
            {
                var remove = toRemove.Pop();
                listPool.Release(handlerMap[remove]);
                handlerMap.Remove(remove);
            }

            capturesPool.Release(toRemove);
        }

        private bool DispatchEvents(Transform transform, EventContext context, T data)
        {
            // If current target is disabled, don't execute handlers for this transform
            if (!transform.gameObject.activeInHierarchy) return true;
            if (!handlerMap.TryGetValue(transform, out var invocationList)) return true;
            context.CurrentTarget = transform;
            return invocationList.Invoke(dispatchId, context, data);
        }
    }
}
