//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(BackHandler))]
	public class BackHandlerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			BackHandler backHandler = (BackHandler)target;

			switch (backHandler.Behaviour)
			{
				case BackHandler.BackBehaviour.HideView:
				case BackHandler.BackBehaviour.DestroyView:
					if (!backHandler.HasComponent<UIView>())
					{
						EditorGUILayout.HelpBox("Warning: This BackBehaviour requires a UIView component on the same GameObject to function properly.", MessageType.Warning);
						if (GUILayout.Button("Add UIView Component"))
						{
							backHandler.gameObject.AddComponent<UIView>();
						}
					}
					break;
			}

		}
	}
}
