//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
    [CreateAssetMenu(menuName = "BlueCheese/UI/UI View Bank", fileName = "UIViewBank")]
    public class UIViewBank : ScriptableObject
    {
       public UIView[] ViewPrefabs;
    }
}
