using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (ConstantID), true)]
	public class ConstantIDEditor : Editor
	{

		public override void OnInspectorGUI()
	    {
			SharedGUI ();
		}
		
		
		protected void SharedGUI()
		{
			ConstantID _target = (ConstantID) target;

			EditorGUILayout.BeginVertical ("Button");

			EditorGUILayout.LabelField ("Constant ID number", EditorStyles.boldLabel);

			_target.autoManual = (AutoManual) EditorGUILayout.EnumPopup ("Set:", _target.autoManual);

			_target.retainInPrefab = EditorGUILayout.Toggle ("Retain in prefab?", _target.retainInPrefab);

			if (PrefabUtility.GetPrefabType (_target.gameObject) == PrefabType.Prefab)
			{
				// Prefab
				if (!_target.retainInPrefab && _target.constantID != 0)
				{
					_target.constantID = 0;
				}
				else if (_target.retainInPrefab && _target.constantID == 0)
				{
					_target.SetNewID_Prefab ();
				}
			}

			EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("ID:", GUILayout.Width (50f));
				if (_target.autoManual == AutoManual.Automatic)
				{
					EditorGUILayout.LabelField (_target.constantID.ToString ());
				}
				else
				{
					_target.constantID = EditorGUILayout.IntField (_target.constantID);
				}
				if (GUILayout.Button ("Copy number"))
				{
					EditorGUIUtility.systemCopyBuffer = _target.constantID.ToString ();
				}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}