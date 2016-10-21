using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (RememberAnimator), true)]
	public class RememberAnimatorEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			#if UNITY_5
			SharedGUI ();
			#else
			EditorGUILayout.HelpBox ("This component is only compatible with Unity 5.", MessageType.Info);
			#endif
		}
		
	}
	
}