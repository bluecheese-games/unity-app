//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
    public class UnityLogger<TClass> : ILogger<TClass> where TClass : class
    {
        private readonly IApp _app;

        private readonly string _prefix = string.Empty;

        public LogType LogTypes { get; set; } = LogType.Info | LogType.Warning | LogType.Error | LogType.Exception;

        public UnityLogger(IApp app)
        {
            _app = app;
            _prefix = $"<b><color=fff>[{typeof(TClass).Name}]</color></b> ";
        }

        public void Log(string message, Object context = null)
        {
            if (_app.Environment == Environment.Development && LogTypes.HasFlag(LogType.Info))
            {
                Debug.Log($"{_prefix}{message}", context);
            }
        }

        public void LogWarning(string message, Object context = null)
        {
            if (_app.Environment == Environment.Development && LogTypes.HasFlag(LogType.Warning))
            {
                Debug.LogWarning($"{_prefix}{message}", context);
            }
        }

        public void LogError(string message, Object context = null)
        {
            if (_app.Environment == Environment.Development && LogTypes.HasFlag(LogType.Error))
            {
                Debug.LogError($"{_prefix}{message}", context);
            }
        }

        public void LogException(System.Exception exeption, Object context = null)
        {
            if (_app.Environment == Environment.Development && LogTypes.HasFlag(LogType.Exception))
            {
                Debug.LogException(exeption, context);
            }
        }
    }
}
