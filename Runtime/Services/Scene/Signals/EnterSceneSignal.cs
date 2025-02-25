//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public readonly struct EnterSceneSignal
    {
        public readonly string SceneName;
        public readonly string PreviousSceneName;
        private readonly object _payload;

        public EnterSceneSignal(string sceneName, string previousSceneName, object payload = null)
        {
            SceneName = sceneName;
            PreviousSceneName = previousSceneName;
            _payload = payload;
        }

        public readonly T GetPayload<T>()
        {
            return (T)_payload;
        }
    }
}