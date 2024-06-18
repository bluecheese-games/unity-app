//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;

namespace BlueCheese.App
{
    public partial class UnityApp
    {
        public class Builder
        {
            private readonly UnityApp _app = new();
            public ServiceContainer ServiceContainer => _app.ServiceContainer;

            public Builder()
            {
                _app.ServiceContainer = ServiceContainer.Default;
            }

            public Builder(ServiceContainer serviceContainer)
            {
                _app.ServiceContainer = serviceContainer;
            }

            public Builder UseEnvironment(Environment environment)
            {
                _app.Environment = environment;
                return this;
            }

            public UnityApp Build()
            {
                _app.ServiceContainer.Register<IApp>(_app);
                return _app;
            }
        }
    }
}