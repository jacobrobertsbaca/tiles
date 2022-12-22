using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core.Events
{
    public class Event
    {
        public string Name { get; }
        public bool Cancelable { get; }

        public Event(string name, bool cancelable = false)
        {
            Name = name;
            Cancelable = cancelable;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class Event<T> : Event
    {
        public Event(string name, bool cancelable = false) : base(name, cancelable) {}

        private EventDispatcher<T> dispatcher;
        internal EventDispatcher<T> Dispatcher
        {
            get
            {
                dispatcher ??= new EventDispatcher<T>(this);
                return dispatcher;
            }
        }

        public void Execute(Component component, T data) => Dispatcher.Dispatch(component.transform, data);
    }
}