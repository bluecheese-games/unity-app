//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;

namespace BlueCheese.App.Services
{
    public interface IUIService : IInitializable
    {
        /// <summary>
        /// Creates a UIView and returns it.
        /// </summary>
        /// <param name="viewName">The name of the view to create.</param>
        UIView CreateView(string viewName);

        /// <summary>
        /// Register the UIView once it is enabled.
        /// </summary>
        /// <param name="view">The UIView to register.</param>
        void RegisterView(UIView view);

        /// <summary>
        /// Unregister the UIView once it is disabled or destroyed.
        /// </summary>
        /// <param name="view">The UIView to unregister.</param>
        void UnregisterView(UIView view);
    }
}
