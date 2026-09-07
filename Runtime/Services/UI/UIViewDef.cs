using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	/// <summary>
	/// Registers a <see cref="UIView"/> prefab in the <see cref="AssetBank"/> so <see cref="UIService"/> can
	/// resolve and pool it by name on demand, instead of every screen's prefab reference being embedded in
	/// a single monolithic bank asset loaded from Resources at boot (the former UIViewBank).
	///
	/// <para>
	/// Being an <see cref="AssetBase"/>, each view independently chooses its <see cref="AssetLoadMode"/>
	/// (Resources or Addressables) and <see cref="BundleKey"/> (Addressables grouping) via the inspector,
	/// with no change required in <see cref="UIService"/>.
	/// </para>
	/// </summary>
	[CreateAssetMenu(menuName = "BlueCheese/UI/UI View Def", fileName = "New UIView")]
	public class UIViewDef : AssetBase
	{
		/// <summary>
		/// Tag automatically kept in sync with <see cref="Prewarm"/> (see <see cref="OnValidate"/>).
		/// <see cref="UIService"/> queries this tag to resolve and pool-prewarm only the views that opted
		/// in during <see cref="UIService.Initialize"/>; every other view stays unresolved -- no prefab
		/// loaded into memory -- until its first <see cref="UIService.SpawnView"/> call.
		/// </summary>
		public const string PrewarmTag = "UIPrewarm";

		[Header("Prefab")]
		public UIView ViewPrefab;

		[Header("Pool")]
		[Tooltip("If enabled, the UI service preallocates instances for this view during initialization, avoiding a first-open hitch.")]
		public bool Prewarm = false;

		[Tooltip("Number of pooled instances to preallocate when Prewarm is enabled.")]
		[Min(1)]
		public int PrewarmPoolSize = 1;

		[Tooltip("Maximum number of concurrent instances the pool holds before applying the overflow policy below.")]
		[Min(1)]
		public int Capacity = GameObjectPool.DefaultCapacity;

		[Tooltip("Policy applied when Capacity is exceeded.")]
		public PoolOverflow Overflow = PoolOverflow.LogError;

		[Tooltip("If true, pooled instances persist across scene loads instead of being destroyed with their pool container.")]
		public bool DontDestroyOnLoad = false;

		public bool IsValid => ViewPrefab != null;

		/// <summary>
		/// Builds the <see cref="PoolOptions"/> describing how <see cref="UIService"/> should pool this view.
		/// </summary>
		public PoolOptions ToPoolOptions() => new()
		{
			UseContainer = true,
			FillAmount = Prewarm ? PrewarmPoolSize : 0,
			Capacity = Mathf.Max(Capacity, Prewarm ? PrewarmPoolSize : 0),
			Overflow = Overflow,
			DontDestroyOnLoad = DontDestroyOnLoad,
		};

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if (PrewarmPoolSize < 1) PrewarmPoolSize = 1;
			if (Capacity < 1) Capacity = GameObjectPool.DefaultCapacity;

			// Keep the discovery tag in sync with the Prewarm toggle so designers only ever interact with
			// the single checkbox above; UIService.PrewarmAsync relies on the tag to avoid resolving every
			// registered view just to check its Prewarm flag.
			bool hasTag = Tags.Contains(PrewarmTag);
			if (Prewarm && !hasTag)
			{
				Tags.Combine(new[] { PrewarmTag });
			}
			else if (!Prewarm && hasTag)
			{
				string[] values = Tags;
				Tags = System.Array.FindAll(values, tag => tag != PrewarmTag);
			}
		}
#endif
	}
}
