//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
	public interface IDespawnable
    {
        /// <summary>
		/// This method is called when the instance is despawned from the pool.
		/// </summary>
		void OnDespawn();
    }
}
