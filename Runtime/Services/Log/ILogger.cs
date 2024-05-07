//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Unity.App.Services
{
    [Flags]
    public enum LogType
    {
        None,
        Info,
        Warning,
        Error,
        Exception,
    }

    public interface ILogger<TClass> where TClass : class
    {
        LogType LogTypes { get; set; }
        void Log(string message, UnityEngine.Object context = null);
        void LogWarning(string message, UnityEngine.Object context = null);
        void LogError(string message, UnityEngine.Object context = null);
        void LogException(Exception exeption, UnityEngine.Object context = null);
    }
}
