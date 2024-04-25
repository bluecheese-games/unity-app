//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    [CreateAssetMenu(menuName = "UI/ViewBank", fileName = "UIViewBank")]
    public class UIViewBank : ScriptableObject
    {
       public UIView[] ViewPrefabs;
    }
}
