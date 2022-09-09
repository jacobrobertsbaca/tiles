using UnityEngine;

namespace Tiles.Core.Events
{
    public class EventContext
    {
        public ExecutionPhase Phase { get; internal set; }
        public Event Event { get; internal set; }
        public Transform Target { get; internal set; }
        public Transform CurrentTarget { get; internal set; }
        public Component Owner { get; internal set; }

        internal bool cancelled;
        internal bool cancelledImmediate;

        public void Cancel(bool immediate = false)
        {
            cancelled = true;
            if (immediate) cancelledImmediate = true;
        }

        public override string ToString()
        {
            return $"[\n" +
                $"\tEvent: {Event}\n" +
                $"\tPhase: {Phase}\n" +
                $"\tTarget: {Target.gameObject.name}\n" +
                $"\tCurrent: {CurrentTarget.gameObject.name}\n" +
                $"\tOwner: {(Owner ? Owner.gameObject.name : "null")}\n" +
                $"]";
        }
    }
}
