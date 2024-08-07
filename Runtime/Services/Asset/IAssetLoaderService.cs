//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
	public interface IAssetLoaderService
    {
        T LoadAssetFromResources<T>(string path) where T : Object;
        Task<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object;
        T[] LoadAssetsFromResources<T>(string path) where T : Object;
    }
}