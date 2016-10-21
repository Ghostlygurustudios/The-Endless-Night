using UnityEngine;
using System.Collections;
using UnityEditor;

namespace AC
{
	
	[CustomEditor (typeof (Highlight))]
	public class HighlightEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			Highlight _target = (Highlight) target;

			_target.brightenMaterials = EditorGUILayout.Toggle ("Auto-brighten materials?", _target.brightenMaterials);
			_target.affectChildren = EditorGUILayout.Toggle ("Also affect child Renderers?", _target.affectChildren);
			_target.maxHighlight = EditorGUILayout.Slider ("Maximum highlight intensity:", _target.maxHighlight, 1f, 5f);
			_target.fadeTime = EditorGUILayout.Slider ("Transition time (s):", _target.fadeTime, 0f, 5f);

			_target.callEvents = EditorGUILayout.Toggle ("Call custom events?", _target.callEvents);
			if (_target.callEvents)
			{
				var serializedObject = new UnityEditor.SerializedObject (_target);

				SerializedProperty onHighlightOn = serializedObject.FindProperty ("onHighlightOn");
				SerializedProperty onHighlightOff = serializedObject.FindProperty ("onHighlightOff");

				EditorGUILayout.PropertyField (onHighlightOn, true);
				EditorGUILayout.PropertyField (onHighlightOff, true);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	
	}
}
