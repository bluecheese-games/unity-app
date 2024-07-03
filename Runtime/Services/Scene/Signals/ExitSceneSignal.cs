//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public readonly struct ExitSceneSignal
    {
        public readonly string ExitingSceneName;
        public readonly string NextSceneName;
        private readonly object _payload;

        public ExitSceneSignal(string exitingSceneName, string nextSceneName, object payload = null)
        {
            ExitingSceneName = exitingSceneName;
            NextSceneName = nextSceneName;
            _payload = payload;
        }

        public readonly T GetPayload<T>()
        {
            return (T)_payload;
        }
    }
}