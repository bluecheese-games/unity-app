//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
	public interface IRecyclable
    {
        /// <summary>
        /// This method is called when the instance is recycled from the pool.
        /// </summary>
        void Recycle();
    }
}
