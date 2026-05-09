using System;

namespace BiblePlaylist.Client.Delegates
{
    public class EventArgs<T> : EventArgs
    {

        public T Value { get; private set; }

        public EventArgs(T val)
        {
            Value = val;
        }

    }
}
