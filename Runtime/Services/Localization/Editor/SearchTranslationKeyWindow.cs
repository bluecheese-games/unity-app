﻿using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	public class SearchTranslationKeyWindow : EditorWindow
	{
		public static void Open(Action<string> callback)
		{
			_callback = callback;
			var window = GetWindow<SearchTranslationKeyWindow>();
			window.titleContent = new GUIContent("Search Translation Key");
			window.minSize = new Vector2(300, 300);
			window.Show();
		}

		private static Action<string> _callback;

		private string[] _keys;
		private string _searchText;
		private Vector2 _scrollPosition;

		private void OnEnable()
		{
			_keys = ((EditorTranslationService)EditorServices.Get<ITranslationService>())
				.GetAllKeys()
				.ToArray();
		}

		private void OnGUI()
		{
			var searchIcon = EditorGUIUtility.Load("Search On Icon") as Texture2D;

			var keyStyle = new GUIStyle(EditorStyles.textField)
			{
				hover = new GUIStyleState
				{
					textColor = Color.yellow
				},
			};

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(searchIcon, GUILayout.Width(25), GUILayout.Height(20));
			GUI.SetNextControlName("search-text");
			_searchText = EditorGUILayout.TextField(_searchText);
			EditorGUILayout.EndHorizontal();

			if (_keys != null)
			{
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
				foreach (var key in _keys)
				{
					if (string.IsNullOrWhiteSpace(_searchText) || key.Contains(_searchText))
					{
						if (GUILayout.Button(new GUIContent(key), keyStyle))
						{
							_callback?.Invoke(key);
							Close();
						}
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
			}

			EditorGUI.FocusTextInControl("search-text");
		}
	}
}