using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine.UI;

namespace BlueCheese.App
{
	public class UIService : IUIService
	{
		private readonly IGameObjectPoolService _poolService;
		private readonly UISettings _settings;
		private bool _isInitialized;

		public UIService(IGameObjectPoolService poolService, IOptions<UISettings> settings)
		{
			_poolService = poolService;
			_settings = settings.Value;
		}

		public void Initialize()
		{
			if (_isInitialized)
			{
				return;
			}

			PrewarmAsync().Forget();

			_isInitialized = true;
		}

		// Eagerly resolves and pool-prewarms only the views tagged UIViewDef.PrewarmTag. Every other
		// registered UIViewDef is left untouched here -- AssetBank.GetAllAssets() (used nowhere in this
		// class) would only expose lightweight metadata anyway, but GetAssetsByTagAsync itself DOES load
		// the matched assets, so scoping this query to the prewarm tag is what keeps non-prewarmed views
		// (and their prefabs) out of memory until SpawnView resolves them on demand.
		private async UniTask PrewarmAsync()
		{
			var viewDefs = await AssetBank.GetAssetsByTagAsync<UIViewDef>(UIViewDef.PrewarmTag);
			foreach (var viewDef in viewDefs)
			{
				if (viewDef == null || !viewDef.IsValid)
				{
					continue;
				}

				var pool = _poolService.SetupPool(viewDef.ViewPrefab.gameObject, viewDef.ToPoolOptions());

				// Spread the Instantiate calls across frames so a long prewarm list doesn't spike a single
				// frame (same technique as FXService.PrewarmAsync).
				for (int count = pool.CountAvailable + pool.CountInUse + 1; count <= viewDef.PrewarmPoolSize; count++)
				{
					pool.Fill(count);
					await UniTask.Yield();
				}
			}
		}

		public UIView SpawnView(string viewName)
		{
			var viewDef = AssetBank.GetAssetByName<UIViewDef>(viewName);
			if (viewDef == null)
			{
				throw new ArgumentException($"Unable to spawn UIView: no UIViewDef registered with name '{viewName}'.", nameof(viewName));
			}

			return SpawnView(viewDef);
		}

		public UIView SpawnView(UIViewDef viewDef)
		{
			if (viewDef == null || !viewDef.IsValid)
			{
				throw new ArgumentException("Unable to spawn UIView: the given UIViewDef is null or invalid (missing ViewPrefab).", nameof(viewDef));
			}

			// SetupPool is idempotent (a no-op reconfigure when the options already match), so this both
			// creates the pool lazily on first spawn and keeps it aligned with the def's PoolOptions.
			var pool = _poolService.SetupPool(viewDef.ViewPrefab.gameObject, viewDef.ToPoolOptions());
			var view = pool.Spawn<UIView>();

			// Force every spawned view's CanvasScaler (if any) to the single app-wide configuration, so
			// individual view/popup prefabs can never drift out of sync on reference resolution/scale mode
			// -- whatever happens to be saved on the prefab is irrelevant, this always wins. Re-applied on
			// every spawn (not just first creation) so it stays correct even for pooled/reused instances.
			if (view.TryGetComponent<CanvasScaler>(out var scaler))
			{
				_settings.Canvas.ApplyTo(scaler);
			}

			return view;
		}

		public void DespawnView(UIView view)
		{
			if (view == null)
			{
				return;
			}

			// Each pooled instance remembers the pool it was spawned from via its PoolItem component, so
			// despawning doesn't need a name/prefab lookup on this side. This also fixes a pre-existing bug:
			// the previous implementation tracked pools in a Dictionary<UIView, IGameObjectPool> keyed by
			// the *prefab* reference, then looked it up using the *spawned instance* passed here -- a key
			// that was never actually in the dictionary, so DespawnView effectively always threw.
			if (!view.TryGetComponent<GameObjectPool.PoolItem>(out var poolItem))
			{
				throw new ArgumentException($"Unable to despawn UIView '{view.name}': it was not spawned by {nameof(UIService)}.", nameof(view));
			}

			poolItem.Pool.Despawn(view);
		}
	}
}
