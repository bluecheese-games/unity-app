//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public interface IAssetFinderService
	{
		T FindAssetInResources<T>() where T : Object;
		T[] FindAssetsInResources<T>() where T : Object;
	}
}