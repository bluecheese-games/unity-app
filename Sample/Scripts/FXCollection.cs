//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.App.Sample
{
	[CreateAssetMenu(fileName = "FXCollection", menuName = "BlueCheese/Sample/FXCollection", order = 1)]
	public class FXCollection : AutoCollection<FXDef>
	{
#if UNITY_EDITOR
		protected override bool Filter(FXDef asset) => asset.IsValid;
#endif
	}
}
