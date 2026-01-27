//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
    public interface IApp
    {
        public Environment Environment { get; }
        Version Version { get; }

        void Quit();
    }
}