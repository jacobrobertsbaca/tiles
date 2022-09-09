namespace Tiles.Core.Events
{
    public delegate void EventListener<T>(EventContext context, T data);
}
