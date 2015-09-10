using System;
using Microsoft.PSharp;

namespace PSharpAlternative
{
    public class EventInfo
    {
        public Event evt;
        public Type type;
        public object payload;

        public EventInfo(Event evt, object payload)
        {
            this.evt = evt;
            this.payload = payload;
            type = evt.GetType();
        }
    }
}