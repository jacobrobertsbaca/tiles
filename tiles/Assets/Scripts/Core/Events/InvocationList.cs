using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Tiles.Core.Events
{
    internal class InvocationList<T>
    {
        private static readonly ObjectPool<EventRegistration> registrationPool = new ObjectPool<EventRegistration>(
            () => new EventRegistration(),
            defaultCapacity: 0,
            maxSize: int.MaxValue
        );

        public class EventRegistration
        {
            public int DispatchId;
            public bool HasOwner;
            public Component Owner;
            public EventListener<T> Handler;
            public bool IsCapture;

            public bool IsSame(EventListener<T> handler, Component owner)
            {
                bool hasOwner = owner;
                if (Handler != handler) return false;
                if (HasOwner != hasOwner) return false;
                if (HasOwner) return Owner == owner;
                return true;
            }
        }

        public int Count => invocationList.Count;

        private readonly List<EventRegistration> invocationList = new List<EventRegistration>();

        public void Register(
            EventListener<T> handler, 
            Component owner, 
            bool isCapture, 
            ref int captureCount, 
            ref int totalCount)
        {
            if (handler is null) return;
            InvalidateList(ref captureCount, ref totalCount);

            foreach (var reg in invocationList)
            {
                // Check to see if an existing registration exists for this owner/handler pair and update it
                if (reg.IsSame(handler, owner))
                {
                    if (isCapture && !reg.IsCapture) captureCount++;
                    else if (!isCapture && reg.IsCapture) captureCount--;
                    reg.IsCapture = isCapture;
                    return;
                }
            }

            var registration = registrationPool.Get();
            registration.DispatchId = 0;
            registration.HasOwner = owner;
            registration.Owner = owner;
            registration.Handler = handler;
            registration.IsCapture = isCapture;
            invocationList.Add(registration);
            totalCount++;
        }

        public bool Invoke(int dispatchId, EventContext context, T data)
        {
            foreach (var reg in invocationList)
            {
                // Don't execute non-capture handlers during capture phase
                if (context.Phase == ExecutionPhase.Capturing && !reg.IsCapture) continue;

                // Don't execute if registration has owner and owner is null or gameObject disabled
                if (reg.HasOwner && (!reg.Owner || !reg.Owner.gameObject.activeInHierarchy)) continue;

                // Don't execute if event has already been processed
                if (reg.DispatchId == dispatchId) continue;

                // Mark event processed
                reg.DispatchId = dispatchId;

                // Execute event
                context.Owner = reg.Owner;
                reg.Handler(context, data);
                if (context.cancelledImmediate) return false;
            }

            return !context.cancelled;
        }

        public void Unregister(EventListener<T> handler, Component owner, ref int captureCount, ref int totalCount)
        {
            if (handler is null) return;
            InvalidateList(ref captureCount, ref totalCount);

            for (int i = 0; i < invocationList.Count; i++)
            {
                EventRegistration reg = invocationList[i];
                if (reg.IsSame(handler, owner))
                {
                    totalCount--;
                    if (reg.IsCapture) captureCount--;
                    invocationList.RemoveAt(i);
                    registrationPool.Release(reg);
                }
            }
        }

        public void Unregister(Component owner, ref int captureCount, ref int totalCount)
        {
            if (owner == null) return;
            InvalidateList(ref captureCount, ref totalCount);

            for (int i = invocationList.Count - 1; i >= 0; i--)
            {
                EventRegistration reg = invocationList[i];
                if (reg.HasOwner && reg.Owner == owner)
                {
                    totalCount--;
                    if (reg.IsCapture) captureCount--;
                    invocationList.RemoveAt(i);
                    registrationPool.Release(reg);
                }
            }
        } 

        private void InvalidateList(ref int captureCount, ref int totalCount)
        {
            // Strip registrations which should have owners but whose owners have been destroyed
            int capturesRemoved = 0;
            int totalRemoved = 0;
            invocationList.RemoveAll(reg =>
            {
                bool remove = reg.HasOwner && !reg.Owner;
                if (remove)
                {
                    totalRemoved++;
                    if (reg.IsCapture) capturesRemoved++;
                }
                return remove;
            });
            captureCount -= capturesRemoved;
            totalCount -= totalRemoved;
        }
    }
}
