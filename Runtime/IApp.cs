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
