//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App.Services
{
    [CreateAssetMenu(menuName = "UI/ViewBank", fileName = "UIViewBank")]
    public class UIViewBank : ScriptableObject
    {
       public UIView[] ViewPrefabs;
    }
}
