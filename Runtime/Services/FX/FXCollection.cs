//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "BlueCheese/FX/FX Collection", fileName = "FXCollection")]
	public class FXCollection : AutoCollection<FXDef>
	{
#if UNITY_EDITOR
		protected override bool CollectFilter(FXDef asset) => asset.IsValid;
#endif
	}
}
