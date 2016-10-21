/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"ActionMovie.cs"
 * 
 *	Plays movie clips either on a Texture, or full-screen on mobile devices.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionMovie : Action
	{
		
		#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_WEBGL)
		public MovieClipType movieClipType = MovieClipType.FullScreen;

		public Material material;
		public int materialParameterID = -1;

		public MovieTexture movieClip;
		public int movieClipParameterID = -1;

		public Sound sound;
		public int soundID = 0;

		public bool includeAudio;
		public string skipKey;
		#endif
		public bool canSkip;

		public string filePath;
		private GUITexture guiTexture;

		
		public ActionMovie ()
		{
			this.isDisplayed = true;
			title = "Play movie clip";
			category = ActionCategory.Engine;
			description = "Plays movie clips either on a Texture, or full-screen on mobile devices.";
		}


		override public void AssignValues (List<ActionParameter> parameters)
		{
			#if UNITY_WEBGL
			#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8
			#elif UNITY_5 || UNITY_PRO_LICENSE
			material = (Material) AssignObject <Material> (parameters, materialParameterID, material);
			movieClip = (MovieTexture) AssignObject <MovieTexture> (parameters, movieClipParameterID, movieClip);
			#endif

			#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_WEBGL)
			sound = AssignFile (soundID, sound);
			#endif
		}
		
		
		override public float Run ()
		{
			#if UNITY_WEBGL

			return 0f;

			#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8

			if (!isRunning && filePath != "")
			{
				isRunning = true;

				if (canSkip)
				{
					Handheld.PlayFullScreenMovie (filePath, Color.black, FullScreenMovieControlMode.CancelOnInput);
				}
				else
				{
					Handheld.PlayFullScreenMovie (filePath, Color.black, FullScreenMovieControlMode.Full);
				}
				return defaultPauseTime;
			}
			else
			{
				isRunning = false;
				return 0f;
			}

			#elif UNITY_5 || UNITY_PRO_LICENSE

			if (movieClip == null)
			{
				ACDebug.LogWarning ("Cannot play movie - no movie clip set!");
				return 0f;
			}
			if (movieClipType == MovieClipType.OnMaterial && material == null)
			{
				ACDebug.LogWarning ("Cannot play movie - no material has been assigned. A movie clip can only be played as a material's texture, so a material must be assigned.");
				return 0f;
			}
			if (includeAudio && sound == null)
			{
				ACDebug.LogWarning ("Cannot play movie audio - no Sound object has been assigned.");
			}

			if (!isRunning)
			{
				isRunning = true;
				guiTexture = null;

				KickStarter.playerInput.skipMovieKey = "";

				if (movieClipType == MovieClipType.FullScreen)
				{
					CreateFullScreenMovie ();
				}
				else if (movieClipType == MovieClipType.OnMaterial)
				{
					material.mainTexture = movieClip;
				}
				movieClip.Play ();

				if (includeAudio)
				{
					sound.GetComponent <AudioSource>().clip = movieClip.audioClip;
					sound.Play (false);
				}

				if (movieClipType == MovieClipType.FullScreen || willWait)
				{
					if (canSkip && skipKey != "")
					{
						KickStarter.playerInput.skipMovieKey = skipKey;
					}
					return defaultPauseTime;
				}
				return 0f;
			}
			else
			{
				if (movieClip.isPlaying)
				{
					if (!canSkip || KickStarter.playerInput.skipMovieKey != "")
					{
						return defaultPauseTime;
					}
				}

				if (includeAudio)
				{
					sound.Stop ();
				}
				movieClip.Stop ();
				KickStarter.playerInput.skipMovieKey = "";

				if (movieClipType == MovieClipType.FullScreen)
				{
					EndFullScreenMovie ();
				}

				isRunning = false;
				return 0f;
			}
			#else
			ACDebug.LogWarning ("On non-mobile platforms, this Action is only available in Unity 5 or Unity Pro.");
			return 0f;
			#endif
		}


		#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_WEBGL)
		private void CreateFullScreenMovie ()
		{
			GameObject movieOb = new GameObject ("Movie clip");
			movieOb.transform.position = Vector3.zero;
			movieOb.transform.position = new Vector2 (0.5f, 0.5f);

			guiTexture = movieOb.AddComponent<GUITexture>();
			guiTexture.enabled = false;
			guiTexture.texture = movieClip;
			guiTexture.enabled = true;

			KickStarter.sceneSettings.SetFullScreenMovie (movieClip);
		}


		private void EndFullScreenMovie ()
		{
			KickStarter.sceneSettings.StopFullScreenMovie ();
			guiTexture.enabled = false;
			Destroy (guiTexture.gameObject);
		}
		#endif
		
		
		#if UNITY_EDITOR
		
		override public void ShowGUI (List<ActionParameter> parameters)
		{
			#if UNITY_WEBGL

			EditorGUILayout.HelpBox ("This Action is not available on the WebGL platform.", MessageType.Info);

			#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8

			filePath = EditorGUILayout.TextField ("Path to clip file:", filePath);
			canSkip = EditorGUILayout.Toggle ("Player can skip?", canSkip);

			EditorGUILayout.HelpBox ("The clip must be placed in a folder named 'StreamingAssets'.", MessageType.Info);

			#elif UNITY_5 || UNITY_PRO_LICENSE

			movieClipParameterID = Action.ChooseParameterGUI ("Movie clip:", parameters, movieClipParameterID, ParameterType.UnityObject);
			if (movieClipParameterID < 0)
			{
				movieClip = (MovieTexture) EditorGUILayout.ObjectField ("Movie clip:", movieClip, typeof (MovieTexture), false);
			}

			movieClipType = (MovieClipType) EditorGUILayout.EnumPopup ("Play clip:", movieClipType);
			if (movieClipType == MovieClipType.OnMaterial)
			{
				materialParameterID = Action.ChooseParameterGUI ("Material to play on:", parameters, materialParameterID, ParameterType.UnityObject);
				if (materialParameterID < 0)
				{
					material = (Material) EditorGUILayout.ObjectField ("Material to play on:", material, typeof (Material), true);
				}
			}

			includeAudio = EditorGUILayout.Toggle ("Include audio?", includeAudio);
			if (includeAudio)
			{
				sound = (Sound) EditorGUILayout.ObjectField ("Audio source:", sound, typeof (Sound), true);

				soundID = FieldToID (sound, soundID);
				sound = IDToField (sound, soundID, false);
			}

			if (movieClipType == MovieClipType.OnMaterial)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
			if (movieClipType == MovieClipType.FullScreen || willWait)
			{
				canSkip = EditorGUILayout.Toggle ("Player can skip?", canSkip);
				if (canSkip)
				{
					skipKey = EditorGUILayout.TextField ("Skip with Input Button:", skipKey);
				}
			}

			#else
			EditorGUILayout.HelpBox ("On non-mobile platforms, this Action is only available in Unity 5 or Unity Pro.", MessageType.Warning);
			#endif

			AfterRunningOption ();
		}
		
		
		public override string SetLabel ()
		{
			#if UNITY_IOS || UNITY_ANDROID || UNITY_WP8
			if (filePath != "")
			{
				return " (" + filePath + ")";
			}
			#elif !UNITY_WEBGL
			if (movieClip)
			{
				return " (" + movieClip.name + ")";
			}
			#endif
			return "";
		}
		
		#endif
		
	}
	
}