//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public struct EnterSceneSignal
    {
        public string Name;
        public object Payload;

        public EnterSceneSignal(string name, object payload = null)
        {
            Name = name;
            Payload = payload;
        }
    }
}