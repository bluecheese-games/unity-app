using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
	public interface IAssetLoaderService
    {
        T LoadAssetFromResources<T>(string path) where T : Object;
		UniTask<T> LoadAssetFromResourcesAsync<T>(string path) where T : Object;
        T[] LoadAssetsFromResources<T>(string path) where T : Object;
    }
}
