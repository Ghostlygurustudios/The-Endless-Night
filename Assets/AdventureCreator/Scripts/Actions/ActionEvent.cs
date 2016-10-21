/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionSendMessage.cs"
 * 
 *	This action calls "SendMessage" on a GameObject.
 *	Both standard messages, and custom ones with paremeters, can be sent.
 * 
 */

using UnityEngine;
using System.Collections;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionEvent : Action
	{
		
		public UnityEvent unityEvent;
		public UnityEvent skipEvent;
		public bool ignoreWhenSkipping = false;
		
		
		public ActionEvent ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Call event";
			description = "Sends a given message to a GameObject. Can be either a message commonly-used by Adventure Creator (Interact, TurnOn, etc) or a custom one, with an integer argument.";
		}
		

		override public float Run ()
		{
			if (unityEvent != null)
			{
				unityEvent.Invoke ();
			}
			
			return 0f;
		}
		
		override public void Skip ()
		{
			if (!ignoreWhenSkipping)
			{
				Run ();
			}
			else if (skipEvent != null)
			{
				skipEvent.Invoke ();
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			var serializedObject = new UnityEditor.SerializedObject (this);

			SerializedProperty eventProperty = serializedObject.FindProperty ("unityEvent");
			EditorGUILayout.PropertyField (eventProperty, true);

			ignoreWhenSkipping = EditorGUILayout.Toggle ("Ignore when skipping?", ignoreWhenSkipping);

			if (ignoreWhenSkipping)
			{
				SerializedProperty skipEventProperty = serializedObject.FindProperty ("skipEvent");
				EditorGUILayout.PropertyField (skipEventProperty, true);
			}

			serializedObject.ApplyModifiedProperties ();

			AfterRunningOption ();
		}

		#endif
		
	}
	
}