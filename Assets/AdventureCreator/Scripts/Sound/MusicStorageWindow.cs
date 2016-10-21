#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage which music tracks can be played in-game.
	 */
	public class MusicStorageWindow : EditorWindow
	{

		private Vector2 scrollPos;


		/**
		 * <summary>Initialises the window.</summary>
		 */
		public static void Init ()
		{
			MusicStorageWindow window = (MusicStorageWindow) EditorWindow.GetWindow (typeof (MusicStorageWindow));
			UnityVersionHandler.SetWindowTitle (window, "Music storage");
			window.position = new Rect (300, 200, 350, 360);
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			EditorGUILayout.HelpBox ("Assign any music tracks you want to be able to play using the 'Sound: Play music' Action here.", MessageType.Info);
			EditorGUILayout.Space ();

			scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (255f));//Mathf.Min (settingsManager.musicStorages.Count * 50, 255f)+5));

			for (int i=0; i<settingsManager.musicStorages.Count; i++)
			{
				EditorGUILayout.BeginVertical ("Button");

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (settingsManager.musicStorages[i].ID.ToString () + ":", EditorStyles.boldLabel);
				if (GUILayout.Button ("-", GUILayout.MaxWidth (20f)))
				{
					Undo.RecordObject (settingsManager, "Delete music entry");
					settingsManager.musicStorages.RemoveAt (i);
					i=0;
					return;
				}
				EditorGUILayout.EndHorizontal ();

				settingsManager.musicStorages[i].audioClip = (AudioClip) EditorGUILayout.ObjectField ("Clip:", settingsManager.musicStorages[i].audioClip, typeof (AudioClip), false);
				settingsManager.musicStorages[i].relativeVolume = EditorGUILayout.Slider ("Relative volume:", settingsManager.musicStorages[i].relativeVolume, 0f, 1f);

				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.EndScrollView ();

			if (GUILayout.Button ("Add new music clip"))
			{
				Undo.RecordObject (settingsManager, "Delete music entry");
				settingsManager.musicStorages.Add (new MusicStorage (GetIDArray (settingsManager.musicStorages.ToArray ())));
			}

			EditorGUILayout.Space ();
			settingsManager.playMusicWhilePaused = EditorGUILayout.ToggleLeft ("Music can play when game is paused?", settingsManager.playMusicWhilePaused);

			if (GUI.changed)
			{
				EditorUtility.SetDirty (settingsManager);
			}
		}


		private int[] GetIDArray (MusicStorage[] musicStorages)
		{
			List<int> idArray = new List<int>();
			foreach (MusicStorage musicStorage in musicStorages)
			{
				idArray.Add (musicStorage.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}
	
}

#endif