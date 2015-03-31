﻿//#define dbg_level_1
//#define dbg_level_2
#define dbg_controls

using System;
using System.Collections.Generic;
using Fasterflect;
using UnityEditor;
using UnityEngine;
using Vexe.Editor.Helpers;
using Vexe.Runtime.Extensions;
using UnityObject = UnityEngine.Object;

namespace Vexe.Editor.GUIs
{
	public partial class RabbitGUI : BaseGUI, IDisposable
	{
		public float height { private set; get; }

		public float width  { private set; get; }

		public override Rect LastRect
		{
			get
			{
				if (!allocatedMemory)
					return kDummyRect;

				if (nextControlIdx == 0)
					throw new InvalidOperationException("Can't get last rect - there are no previous controls to get the last rect from");

				if (nextControlIdx - 1 >= controls.Count)
				{
					#if dbg_level_1
					Debug.Log("Last rect out of range. Returning dummy rect. If that's causing problems, maybe request a seek instead. nextControlIdx {0}. controls.Count {1}".FormatWith(nextControlIdx, controls.Count));
					#endif
					return kDummyRect; // or maybe request reset?
				}

				return controls[nextControlIdx - 1].rect;
			}
		}

		private enum GUIPhase { Layout, Draw }
		private GUIPhase currentPhase;
		private List<GUIControl> controls;
		private List<GUIBlock> blocks;
		private Stack<GUIBlock> blockStack;
		private Rect start;
		private Rect? validRect;
		private int nextControlIdx;
		private int nextBlockIdx;
		private float prevInspectorWidth;
		private bool pendingLayoutRequest;
		private bool allocatedMemory;

		#if dbg_level_1
			private bool _pendingResetRequest;
			private bool pendingResetRequest
			{
				get { return _pendingResetRequest; }
				set
				{
					if (value)
					{ 
						Debug.Log("Setting pending reset to true");
						LogCallStack();
					}
					_pendingResetRequest = value;
				}
			}
		#else
		private bool pendingResetRequest;
		#endif

		#if dbg_level_1
		private int dbgMaxDepth;
		public static void LogCallStack()
		{
			var stack = new System.Diagnostics.StackTrace();
			foreach (var frame in stack.GetFrames())
			{
				Debug.Log("Call stack: " + frame.GetMethod().Name);
			}
		}
		#endif


		void HandlePlaymodeTransition()
		{
			pendingLayoutRequest = true;
		}

		public RabbitGUI()
		{
			EditorApplication.playmodeStateChanged += HandlePlaymodeTransition;

			currentPhase = GUIPhase.Layout;
			controls     = new List<GUIControl>();
			blocks       = new List<GUIBlock>();
			blockStack   = new Stack<GUIBlock>();

			#if dbg_level_1
				Debug.Log("Instantiated Rabbit");
			#endif
		}

		public override void OnGUI(Action guiCode, Vector2 padding)
		{
			var rect = GUILayoutUtility.GetRect(0f, 0f);

			if (Event.current.type == EventType.Repaint)
			{
				if (!validRect.HasValue || validRect.Value.y != rect.y)
				{
					validRect = rect;
					pendingLayoutRequest = true;
				}
			}

			if (validRect.HasValue)
			{
				var start = new Rect(validRect.Value.x + padding.x, validRect.Value.y, EditorGUIUtility.currentViewWidth - padding.y, validRect.Value.height);
				using (Begin(start))
				{
					guiCode();
				}
			}

			GUILayoutUtility.GetRect(width, height);
		}

		public RabbitGUI Begin(Rect start)
		{
			if (currentPhase == GUIPhase.Layout)
			{
				#if dbg_level_1
					Debug.Log("Layout phase. Was pending layout: {0}. Was pending reset: {1}".FormatWith(pendingLayoutRequest, pendingResetRequest));
				#endif
				this.start = start;
				height = 0f;
				width = start.width;
				pendingLayoutRequest = false;
				pendingResetRequest = false;
			}

			nextControlIdx = 0;
			nextBlockIdx   = 0;
			BeginVertical(styles.None);

			return this;
		}

		private void End()
		{
			allocatedMemory = true;
			var main = blocks[0];
			main.Dispose();

			if (currentPhase == GUIPhase.Layout)
			{
				main.ResetDimensions();
				main.Layout(start);
				height = main.height.Value;
				currentPhase = GUIPhase.Draw;

				#if dbg_level_1
					Debug.Log("Done layout. Deepest Block depth: {0}. Total number of blocks created: {1}. Total number of controls {2}"
								.FormatWith(dbgMaxDepth, blocks.Count, controls.Count));
				#endif
			}
			else
			{
				if (pendingResetRequest || nextControlIdx != controls.Count || nextBlockIdx != blocks.Count)
				{
					#if dbg_level_1
					if (pendingResetRequest)
						Debug.Log("Resetting - Theres a reset request pending");
					else Debug.Log("Resetting -  The number of controls/blocks drawn doesn't match the total number of controls/blocks");
					#endif
					controls.Clear();
					blocks.Clear();
					allocatedMemory = false;
					currentPhase = GUIPhase.Layout;
					//EditorHelper.RepaintAllInspectors();
				}
				else if (pendingLayoutRequest)
				{
					#if dbg_level_1
						Debug.Log("Pending layout request. Doing layout in next phase");
					#endif
					currentPhase = GUIPhase.Layout;
					EditorHelper.RepaintAllInspectors();
				}
				else
				{
					bool resized = prevInspectorWidth != EditorGUIUtility.currentViewWidth;
					if (resized)
					{
						#if dbg_level_1
							Debug.Log("Resized inspector. Doing layout in next phase");
						#endif
						prevInspectorWidth = EditorGUIUtility.currentViewWidth;
						currentPhase = GUIPhase.Layout;
					}
				}
			}
		}

		private T BeginBlock<T>(GUIStyle style) where T : GUIBlock, new()
		{
			if (pendingResetRequest)
			{
				#if dbg_level_1
					Debug.Log("Pending reset. Can't begin block of type: " + typeof(T).Name);
				#endif
				return null;
			}

			if (allocatedMemory && nextBlockIdx >= blocks.Count)
			{
				#if dbg_level_1
					Debug.Log("Requesting Reset. Can't begin block {0}. We seem to have created controls yet nextBlockIdx {1} > blocks.Count {2}"
								.FormatWith(typeof(T).Name, nextBlockIdx, blocks.Count));
				#endif
				pendingResetRequest = true;
				return null;
			}

			T result;
			if (!allocatedMemory)
			{
				blocks.Add(result = new T
				{
					onDisposed = EndBlock,
					data = new ControlData
					{
						type = typeof(T).Name.ParseEnum<ControlType>(),
						style = style,
					}
				});

				if (blockStack.Count > 0)
				{
					var owner = blockStack.Peek();
					owner.AddBlock(result);
				}

				#if dbg_level_1
					Debug.Log("Created new block of type {0}. Blocks count {1}. Is pending reset? {2}".FormatWith(typeof(T).Name, blocks.Count, pendingResetRequest));
				#endif
			}
			else
			{
				result = blocks[nextBlockIdx] as T;

				if (result != null)
				{
					GUI.Box(result.rect, string.Empty, result.data.style = style);
				}
				else
				{
					var requestedType = typeof(T);
					var resultType = blocks[nextBlockIdx].GetType();
					if (requestedType != resultType)
					{
						#if dbg_level_1
							Debug.Log("Requested block result is null. " +
										 "The type of block requested {0} doesn't match the block type {1} at index {2}. " +
										 "This is probably due to the occurance of new blocks revealed by a foldout for ex. " +
										 "Requesting Reset".FormatWith(requestedType.Name, resultType.Name, nextBlockIdx));
						#endif
						pendingResetRequest = true;
						return null;
					}

					#if dbg_level_1
						Debug.Log("Result block is null. Count {0}, Idx {1}, Request type {2}".FormatWith(blocks.Count, nextBlockIdx, typeof(T).Name));
						for (int i = 0; i < blocks.Count; i++)
						{
							Debug.Log("Block {0} at {1} has {2} controls".FormatWith(blocks[i].data.type.ToString(), i, blocks[i].controls.Count));
						}

						Debug.Log("Block Stack count " + blockStack.Count);
						var array = blockStack.ToArray();
						for (int i = 0; i < array.Length; i++)
						{
							Debug.Log("Block {0} at {1} has {2} controls".FormatWith(array[i].data.type.ToString(), i, array[i].controls.Count));
						}
					#endif

					throw new NullReferenceException("result");
				}
			}

			nextBlockIdx++;
			blockStack.Push(result);

			#if dbg_level_2
				Debug.Log("Pushed {0}. Stack {1}. Total {2}. Next {3}".FormatWith(result.GetType().Name, blockStack.Count, blocks.Count, nextBlockIdx));
			#endif

			#if dbg_level_1
			if (blockStack.Count > dbgMaxDepth)
				dbgMaxDepth = blockStack.Count;
			#endif

			return result;
		}

		private void EndBlock()
		{
			if (!pendingResetRequest)
				blockStack.Pop();
		}

		private bool CanDrawControl(out Rect position, ControlData data)
		{
			position = kDummyRect;

			if (pendingResetRequest)
			{
				#if dbg_level_1
					Debug.Log("Can't draw control of type " + data.type + " There's a Reset pending.");
				#endif
				return false;
			}

			if (nextControlIdx >= controls.Count && currentPhase == GUIPhase.Draw)
			{
				#if dbg_level_1
					Debug.Log("Can't draw control of type {0} nextControlIdx {1} is >= controls.Count {2}. Requesting reset".FormatWith(data.type, nextControlIdx, controls.Count));
					LogCallStack();
				#endif
				pendingResetRequest = true;
				return false;
			}

			if (!allocatedMemory)
			{
				NewControl(data);
				return false;
			}

			position = controls[nextControlIdx++].rect;
			return true;
		}

		private GUIControl NewControl(ControlData data)
		{
			var parent  = blockStack.Peek();
			var control = new GUIControl(data);
			parent.controls.Add(control);
			controls.Add(control);
			#if dbg_level_2
				Debug.Log("Created control {0}. Count {1}".FormatWith(data.type, controls.Count));
			#endif
			return control;
		}

		public void RequestReset()
		{
			pendingResetRequest = true;
		}

		public void RequestLayout()
		{
			pendingLayoutRequest = true;
		}

		public void Dispose()
		{
			End();
		}

		public override Bounds BoundsField(GUIContent content, Bounds value, Layout option)
		{
			var bounds = new ControlData(content, styles.None, option, ControlType.Bounds);

			Rect position;
			if (CanDrawControl(out position, bounds))
			{
				return EditorGUI.BoundsField(position, content, value);
			}

			return value;
		}

		public override Rect Rect(GUIContent content, Rect value, Layout option)
		{
			var data = new ControlData(content, styles.None, option, ControlType.RectField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.RectField(position, content, value);
			}

			return value;
		}

		public override void Box(GUIContent content, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.Box);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				GUI.Box(position, content, style);
			}
		}

		public override void HelpBox(string message, MessageType type)
		{
			var content = GetContent(message);
			var height  = GUIHelper.HelpBox.CalcHeight(content, width);
			var layout  = Layout.sHeight(height);
			var data    = new ControlData(content, GUIHelper.HelpBox, layout, ControlType.HelpBox);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				EditorGUI.HelpBox(position, message, type);
			}
		}

		public override bool Button(GUIContent content, GUIStyle style, Layout option, ControlType buttonType)
		{
			var data = new ControlData(content, style, option, buttonType);

			Rect position;
			if (!CanDrawControl(out position, data))
				return false;

#if dbg_controls
			// due to the inability of unity's debugger to successfully break inside generic classes
			// I'm forced to write things this way so I could break when I hit buttons in my drawers,
			// since I can't break inside them cause they're generics... Unity...
			var pressed = GUI.Button(position, content, style);
			if (pressed)
				return true;
			return false;
#else 
			return GUI.Button(position, content, style);
#endif
		}

		public override Color Color(GUIContent content, Color value, Layout option)
		{
			var data = new ControlData(content, styles.ColorField, option, ControlType.ColorField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.ColorField(position, content, value);
			}

			return value;
		}

		public override Enum EnumPopup(GUIContent content, Enum selected, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.EnumPopup);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.EnumPopup(position, content, selected, style);
			}

			return selected;
		}

		public override float Float(GUIContent content, float value, Layout option)
		{
			var data = new ControlData(content, styles.NumberField, option, ControlType.Float);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.FloatField(position, content, value);
			}

			return value;
		}

		public override bool Foldout(GUIContent content, bool value, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.Foldout);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.Foldout(position, value, content, true, style);
			}

			return value;
		}

		static int prev;

		public override int Int(GUIContent content, int value, Layout option)
		{
			var data = new ControlData(content, styles.NumberField, option, ControlType.IntField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				//Debug.Log("intdrawer got: " + value);
				var newValue = EditorGUI.IntField(position, content, value);
				if (prev != newValue)
				{
					prev = newValue;
				}
				//Debug.Log("intdrawer new: " + newValue);
				return newValue;
			}

			return value;
		}

		public override void Label(GUIContent content, GUIStyle style, Layout option)
		{
			var label = new ControlData(content, style, option, ControlType.Label);

			Rect position;
			if (CanDrawControl(out position, label))
			{
				GUI.Label(position, content, style);
			}
		}

		public override int Mask(GUIContent content, int mask, string[] displayedOptions, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.MaskField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.MaskField(position, content, mask, displayedOptions, style);
			}

			return mask;
		}

		public override UnityObject Object(GUIContent content, UnityObject value, Type type, bool allowSceneObjects, Layout option)
		{
			var data = new ControlData(content, styles.ObjectField, option, ControlType.ObjectField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.ObjectField(position, content, value, type, allowSceneObjects);
			}

			return value;
		}

		public override int Popup(string text, int selectedIndex, string[] displayedOptions, GUIStyle style, Layout option)
		{
			var content = GetContent(text);
			var popup = new ControlData(content, style, option, ControlType.Popup);

			Rect position;
			if (CanDrawControl(out position, popup))
			{
				return EditorGUI.Popup(position, content.text, selectedIndex, displayedOptions, style);
			}

			return selectedIndex;
		}

		protected override void BeginScrollView(ref Vector2 pos, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, Layout option)
		{
			throw new NotImplementedException("I need to implement ExpandWidth and ExpandHeight first, sorry");
		}

		protected override void EndScrollView()
		{
			throw new NotImplementedException();
		}

		public override float FloatSlider(GUIContent content, float value, float leftValue, float rightValue, Layout option)
		{
			var data = new ControlData(content, styles.HorizontalSlider, option, ControlType.Slider);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.Slider(position, content, value, leftValue, rightValue);
			}

			return value;
		}

		public override void Space(float pixels)
		{
			if (!allocatedMemory)
			{
				var parent = blockStack.Peek();
				var option = parent.Space(pixels);
				var space  = new ControlData(GUIContent.none, GUIStyle.none, option, ControlType.Space);
				NewControl(space);
			}
			else
			{
				nextControlIdx++;
			}
		}

		public override void FlexibleSpace()
		{
			if (!allocatedMemory)
			{
				var flexible = new ControlData(GUIContent.none, GUIStyle.none, null, ControlType.FlexibleSpace);
				NewControl(flexible);
			}
			else
			{
				nextControlIdx++;
			}
		}

		public override string Text(GUIContent content, string value, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.TextField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.TextField(position, content, value, style);
			}

			return value;
		}

		public override string ToolbarSearch(string value, Layout option)
		{
			var data = new ControlData(GetContent(value), styles.TextField, option, ControlType.TextField);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				int searchMode = 0;
				return GUIHelper.ToolbarSearchField(null, position, null, searchMode, value) as string;
			}

			return value;
		}

		public override bool ToggleLeft(GUIContent content, bool value, GUIStyle labelStyle, Layout option)
		{
			var data = new ControlData(content, labelStyle, option, ControlType.Toggle);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.ToggleLeft(position, content, value, labelStyle);
			}

			return value;
		}

		public override bool Toggle(GUIContent content, bool value, GUIStyle style, Layout option)
		{
			var data = new ControlData(content, style, option, ControlType.Toggle);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.Toggle(position, content, value, style);
			}

			return value;
		}

		protected override HorizontalBlock BeginHorizontal(GUIStyle style)
		{
			return BeginBlock<HorizontalBlock>(style);
		}

		protected override VerticalBlock BeginVertical(GUIStyle style)
		{
			return BeginBlock<VerticalBlock>(style);
		}

		protected override void EndHorizontal()
		{
			EndBlock();
		}

		protected override void EndVertical()
		{
			EndBlock();
		}

		public override string TextArea(string value, Layout option)
		{
			if (option == null)
				option = new Layout();

			if (!option.height.HasValue)
				option.height = 50f;

			var data = new ControlData(GetContent(value), styles.TextArea, option, ControlType.TextArea);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.TextArea(position, value);
			}

			return value;
		}

		public override bool InspectorTitlebar(bool foldout, UnityObject target)
		{
			var data = new ControlData(GUIContent.none, styles.None, null, ControlType.Foldout);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.InspectorTitlebar(position, foldout, target);
			}

			return foldout;
		}

		public override string Tag(GUIContent content, string tag, GUIStyle style, Layout layout)
		{
			var data = new ControlData(content, style, layout, ControlType.Popup);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.TagField(position, content, tag, style);
			}

			return tag;
		}

		public override int Layer(GUIContent content, int layer, GUIStyle style, Layout layout)
		{
			var data = new ControlData(content, style, layout, ControlType.Popup);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				return EditorGUI.LayerField(position, content, layer, style);
			}

			return layer;
		}

		public override void Prefix(string label)
		{
			if (string.IsNullOrEmpty(label)) return;
			var content = GetContent(label);
			var style = EditorStyles.label;
			var data = new ControlData(content, style, Layout.sWidth(EditorGUIUtility.labelWidth), ControlType.PrefixLabel);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				EditorGUI.HandlePrefixLabel(position, position, content, 0, style);
			}
		}

		private static MethodInvoker _scrollableTextArea;
		private static MethodInvoker scrollableTextArea
		{
			get
			{
				if (_scrollableTextArea == null)
				{
					var type = typeof(EditorGUI);
					var method = type.GetMethod("ScrollableTextAreaInternal",
						new Type[] { typeof(Rect), typeof(string), typeof(Vector2).MakeByRefType(), typeof(GUIStyle) },
						Flags.StaticAnyVisibility);

					_scrollableTextArea = method.DelegateForCallMethod();

				}
				return _scrollableTextArea;
			}
		}

		public override string ScrollableTextArea(string value, ref Vector2 scrollPos, GUIStyle style, Layout option)
		{
			if (option == null)
				option = new Layout();

			if (!option.height.HasValue)
				option.height = 50f;

			var content = GetContent(value);
			var data = new ControlData(content, style, option, ControlType.TextArea);

			Rect position;
			if (CanDrawControl(out position, data))
			{
				var args = new object[] { position, value, scrollPos, style };
				var newValue = scrollableTextArea.Invoke(null, args) as string;
				scrollPos = (Vector2)args[2];
				return newValue;
			}

			return value;
		}
	}

	public static class RabbitExtensions
	{
		public static void RequestResetIfRabbit(this BaseGUI gui)
		{
			var rabbit = gui as RabbitGUI;
			if (rabbit != null)
				rabbit.RequestReset();
		}
	}
}