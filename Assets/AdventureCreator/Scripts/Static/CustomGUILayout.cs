#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace AC
{

	public class CustomGUILayout
	{

		public static System.Enum EnumPopup (string label, System.Enum value, string api = "")
		{
			value = EditorGUILayout.EnumPopup (label, value);
			CreateMenu (api);
			return value;
		}


		public static bool Toggle (string label, bool value, string api = "")
		{
			value = EditorGUILayout.Toggle (label, value);
			CreateMenu (api);
			return value;
		}


		public static bool Toggle (bool value, string api = "")
		{
			value = EditorGUILayout.Toggle (value);
			CreateMenu (api);
			return value;
		}


		public static bool ToggleLeft (string label, bool value, string api = "")
		{
			value = EditorGUILayout.ToggleLeft (label, value);
			CreateMenu (api);
			return value;
		}

		
		public static int IntField (string label, int value, string api = "")
		{
			value = EditorGUILayout.IntField (label, value);
			CreateMenu (api);
			return value;
		}


		public static int IntField (int value, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.IntField (value, layoutOption);
			CreateMenu (api);
			return value;
		}
		
		
		public static int IntSlider (string label, int value, int min, int max, string api = "")
		{
			value = EditorGUILayout.IntSlider (label, value, min, max);
			CreateMenu (api);
			return value;
		}
		
		
		public static float FloatField (string label, float value, string api = "")
		{
			value = EditorGUILayout.FloatField (label, value);
			CreateMenu (api);
			return value;
		}


		public static float FloatField (float value, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.FloatField (value, layoutOption);
			CreateMenu (api);
			return value;
		}
		
		
		public static float Slider (string label, float value, float min, float max, string api = "")
		{
			value = EditorGUILayout.Slider (label, value, min, max);
			CreateMenu (api);
			return value;
		}
		

		public static string TextField (string value, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.TextField (value, layoutOption);
			CreateMenu (api);
			return value;
		}

		
		public static string TextField (string label, string value, string api = "")
		{
			value = EditorGUILayout.TextField (label, value);
			CreateMenu (api);
			return value;
		}


		public static string TextArea (string value, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.TextArea (value, EditorStyles.textArea, layoutOption);
			CreateMenu (api);
			return value;
		}


		public static int Popup (string label, int value, string[] list, string api = "")
		{
			value = EditorGUILayout.Popup (label, value, list);
			CreateMenu (api);
			return value;
		}


		public static int Popup (int value, string[] list, string api = "")
		{
			value = EditorGUILayout.Popup (value, list);
			CreateMenu (api);
			return value;
		}


		public static Color ColorField (string label, Color value, string api = "")
		{
			value = EditorGUILayout.ColorField (label, value);
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (string label, Object value, bool allowSceneObjects, string api = "")
		{
			value = EditorGUILayout.ObjectField (label, value, typeof (T), allowSceneObjects);
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (Object value, bool allowSceneObjects, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.ObjectField (value, typeof (T), allowSceneObjects, layoutOption);
			CreateMenu (api);
			return value;
		}


		public static Object ObjectField <T> (Object value, bool allowSceneObjects, GUILayoutOption option1, GUILayoutOption option2, string api = "")
		{
			value = EditorGUILayout.ObjectField (value, typeof (T), allowSceneObjects, option1, option2);
			CreateMenu (api);
			return value;
		}


		public static Vector2 Vector2Field (string label, Vector2 value, GUILayoutOption layoutOption, string api = "")
		{
			value = EditorGUILayout.Vector2Field (label, value, layoutOption);
			CreateMenu (api);
			return (value);
		}
		

		public static AnimationCurve CurveField (string label, AnimationCurve value, string api = "")
		{
			value = EditorGUILayout.CurveField (label, value);
			CreateMenu (api);
			return (value);
		}


		private static void CreateMenu (string api)
		{
			if (api != "" && Event.current.type == EventType.ContextClick && GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition))
			{
				GenericMenu menu = new GenericMenu ();
				menu.AddDisabledItem (new GUIContent (api));
				menu.AddItem (new GUIContent ("Copy script variable"), false, CustomCallback, api);
				menu.ShowAsContext ();
			}
		}


		private static void CustomCallback (object obj)
		{
			if (obj != null)
			{
				TextEditor te = new TextEditor ();
				#if UNITY_5_3 || UNITY_5_4 || UNITY_5_3_OR_NEWER
				te.text = obj.ToString ();
				#else
				te.content = new GUIContent (obj.ToString ());
				#endif
				te.SelectAll ();
				te.Copy ();
			}
		}

	}


	public class CustomStyles
	{

		public static GUIStyle subHeader;
		public static GUIStyle managerHeader;
		public static GUIStyle smallCentre;
		public static GUIStyle thinBox;

		private static bool isInitialised;

		static CustomStyles ()
		{
			Init ();
		}


		private static void Init()
		{
			if (isInitialised)
			{
				return;
			}

			subHeader = new GUIStyle (GUI.skin.label);
			subHeader.fontSize = 13;
			subHeader.margin.top = 10;
			subHeader.fixedHeight = 21;
			if (EditorGUIUtility.isProSkin) subHeader.normal.textColor = Color.white;

			managerHeader = new GUIStyle (GUI.skin.label);
			managerHeader.fontSize = 17;
			managerHeader.alignment = TextAnchor.MiddleCenter;
			if (EditorGUIUtility.isProSkin) managerHeader.normal.textColor = Color.white;

			smallCentre = new GUIStyle (GUI.skin.label);
			smallCentre.richText = true;
			smallCentre.alignment = TextAnchor.MiddleCenter;

			thinBox = new GUIStyle(GUI.skin.box);
			thinBox.padding = new RectOffset(0, 0, 0, 0);

			isInitialised = true;
		}

	}

}

#endif