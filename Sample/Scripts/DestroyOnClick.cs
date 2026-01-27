//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App.Sample
{
    public class DestroyOnClick : MonoBehaviour
    {
		private void OnMouseDown()
		{
			Destroy(gameObject);
		}
    }
}
