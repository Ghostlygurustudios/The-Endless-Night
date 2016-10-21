using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (FollowSortingMap))]
	public class FollowSortingMapEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			FollowSortingMap _target = (FollowSortingMap) target;

			_target.followSortingMap = EditorGUILayout.Toggle ("Follow default Sorting Map?", _target.followSortingMap);
			if (!_target.followSortingMap)
			{
				_target.customSortingMap = (SortingMap) EditorGUILayout.ObjectField ("Sorting Map to follow:", _target.customSortingMap, typeof (SortingMap), true);
			}

			if (_target.followSortingMap || _target.customSortingMap != null)
			{
				_target.offsetOriginal = EditorGUILayout.Toggle ("Offset original Order?", _target.offsetOriginal);
				_target.affectChildren = EditorGUILayout.Toggle ("Also affect children?", _target.affectChildren);
				_target.livePreview = EditorGUILayout.Toggle ("Edit-mode preview?", _target.livePreview);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}