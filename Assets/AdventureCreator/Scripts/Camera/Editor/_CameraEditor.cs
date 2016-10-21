using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor(typeof(_Camera))]
	public class _CameraEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			_Camera _target = (_Camera) target;

			EditorGUILayout.HelpBox ("Attach this script to a custom Camera type to integrate it with Adventure Creator.", MessageType.Info);

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Depth of field", EditorStyles.boldLabel);
				_target.focalDistance = EditorGUILayout.FloatField ("Focal distance", _target.focalDistance);
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}