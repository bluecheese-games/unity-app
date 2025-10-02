//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using TMPro;
using UnityEditor;

namespace BlueCheese.App.Editor
{
	public static class TMPContextMenu
	{
		[MenuItem("CONTEXT/TextMeshProUGUI/Localize")]
		private static void AddLocalizedTextComponent(MenuCommand command)
		{
			TextMeshProUGUI tmp = (TextMeshProUGUI)command.context;
			if (tmp != null && tmp.gameObject.GetComponent<LocalizedText>() == null)
			{
				tmp.gameObject.AddComponent<LocalizedText>();
			}
		}
	}
}