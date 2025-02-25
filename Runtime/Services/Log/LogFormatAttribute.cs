//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System;
using UnityEngine;

namespace BlueCheese.App
{

	public class LogFormatAttribute : Attribute
	{
		private readonly string _prefixLabel = null;
		private readonly string _prefixColor = "fff";
		private readonly FontStyle _prefixStyle = FontStyle.Bold;

		/// <summary>
		/// Custom log format attribute
		/// </summary>
		/// <param name="prefixLabel">Customize the prefix label. If null, the class name will be used as label.</param>
		/// <param name="prefixColor">Customize the prefix color. If null or empty, the default color will be applied.</param>
		/// <param name="prefixStyle">Customize the prefix font style.</param>
		public LogFormatAttribute(string prefixLabel = null, string prefixColor = "fff", FontStyle prefixStyle = FontStyle.Bold)
		{
			_prefixLabel = prefixLabel;
			_prefixColor = prefixColor;
			_prefixStyle = prefixStyle;
		}

		public string FormatPrefix(string label)
		{
			if (_prefixLabel != null)
			{
				label = _prefixLabel;
			}

			if (string.IsNullOrEmpty(label))
			{
				return string.Empty;
			}

			string styleOn = _prefixStyle switch
			{
				FontStyle.Bold => "<b>",
				FontStyle.Italic => "<i>",
				FontStyle.BoldAndItalic => "<b><i>",
				_ => ""
			};

			string styleOff = _prefixStyle switch
			{
				FontStyle.Bold => "</b>",
				FontStyle.Italic => "</i>",
				FontStyle.BoldAndItalic => "</i></b>",
				_ => ""
			};

			if (string.IsNullOrEmpty(_prefixColor))
			{
				return $"{styleOn}{label}{styleOff}";
			}
			return $"<color=#{_prefixColor}>{styleOn}{label}{styleOff}</color>";
		}
	}
}
