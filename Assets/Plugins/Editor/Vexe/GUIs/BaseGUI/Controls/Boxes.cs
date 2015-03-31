﻿using UnityEditor;
using UnityEngine;


namespace Vexe.Editor.GUIs
{
	public abstract partial class BaseGUI
	{
		public void Box(string text, Layout option)
		{
			Box(text, styles.Box, option);
		}

		public void Box(string text, GUIStyle style)
		{
			Box(text, style, null);
		}

		public void Box(string text, GUIStyle style, Layout option)
		{
			Box(GetContent(text), style, option);
		}

		public abstract void Box(GUIContent content, GUIStyle style, Layout option);

		public void Splitter(float thickness)
		{
			Box(string.Empty, Layout.sExpandWidth(true).Height(thickness));
		}

		public void Splitter()
		{
			Splitter(1f);
		}

		public void HelpBox(string message)
		{
			HelpBox(message, MessageType.Info);
		}

		public abstract void HelpBox(string message, MessageType type);
	}
}