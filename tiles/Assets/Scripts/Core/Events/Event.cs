using UnityEngine;

namespace Tiles.Core.Events
{
    public class Event
    {
        public string Name { get; }

        public Event(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class Event<T> : Event
    {
        public Event(string name) : base(name) {}

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