//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
    public class DefaultErrorHandlingService : IErrorHandlingService, IDisposable
    {
        public void Initialize()
        {
            Application.logMessageReceived += HandleLogMessageReceived;
        }

        private void HandleLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
        {
            if (type == UnityEngine.LogType.Exception)
            {
                // TODO
            }
        }

        public void Dispose()
        {
            Application.logMessageReceived -= HandleLogMessageReceived;
        }
    }
}
