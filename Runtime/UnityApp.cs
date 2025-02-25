//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using UnityEngine;

namespace BlueCheese.App
{
    public partial class UnityApp : IApp
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ReloadDomain()
        {
            ServiceContainer.Default.Reset();
        }

        public ServiceContainer ServiceContainer { get; private set; }

        public Environment Environment { get; private set; } = Environment.Development;

        private Version _version;
        public Version Version
        {
            get
            {
                if (_version == null)
                {
                    _version = new Version(Application.version);
                }
                return _version;
            }
        }

        private UnityApp() { }

        public void Run()
        {
            ServiceContainer.Startup();
            Application.quitting += Stop;
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void Stop()
        {
            Application.quitting -= Stop;
            ServiceContainer.Shutdown();
        }
    }
}