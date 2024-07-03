//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Text;

namespace BlueCheese.App
{
    public interface ITrackingService
    {
        void Track(Event evt);

        public readonly ref struct Event
        {
            private readonly string _name;
            private readonly Dictionary<string, object> _parameters; 

            public Event(string name)
            {
                _name = name;
                _parameters = new();
            }

            public Event WithParameter(string key, object value)
            {
                _parameters[key] = value;
                return this;
            }

            public static implicit operator string(Event e) => e._name;
            public static implicit operator Event(string name) => new(name);

            public override string ToString()
            {
                var sb = new StringBuilder(_name);
                foreach (var kvp in _parameters)
                {
                    sb.AppendFormat(" {0}:{1}", kvp.Key, kvp.Value);
                }
                return sb.ToString();
            }
        }
    }
}
