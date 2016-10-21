/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"AdvGame.cs"
 * 
 *	This script provides a number of static functions used by various game scripts.
 * 
 * 	The "DrawTextOutline" function is based on Bérenger's code: http://wiki.unity3d.com/index.php/ShadowAndOutline
 * 
 */

using UnityEngine;
#if UNITY_5
using UnityEngine.Audio;
#endif
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A class that provides a number of useful functons for both editor and runtime.
	 */
	public class AdvGame : ScriptableObject
	{

		/** A List of Action classes currently stored in the copy buffer. */
		public static List<AC.Action> copiedActions = new List<AC.Action>();

		private static References references = null;

		#if UNITY_EDITOR
		private static Texture2D _aaLineTex = null;
		#endif
		
		
		#if UNITY_5
		/**
		 * <summary>Sets the volume of an Audio Mixer Group (Unity 5 only).</summary>
		 * <param name = "audioMixerGroup">The Audio Mixer Group to affect</param>
		 * <param name = "parameter">The name of the attenuation parameter</param>
		 * <param name = "volume">The new volume (ranges from 0 to 1)</param>
		 */
		public static void SetMixerVolume (AudioMixerGroup audioMixerGroup, string parameter, float volume)
		{
			if (audioMixerGroup != null && KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				float attenuation = ((2f * volume) - (volume * volume) - 1f) * 80f;

				audioMixerGroup.audioMixer.SetFloat (parameter, attenuation);
			}
		}
		#endif

		/**
		 * <summary>Sets the 'Output Audio Mixer Group' of an Audio Source, based on its sound type (Unity 5 only).</summary>
		 * <param name = "audioSource">The Audio Source component to affect</param>
		 * <param name = "soundType">The sound type that controls the volume</param>
		 * <param name = "isSpeech">True if the Audio Source is used to play speech</param>
		 */
		public static void AssignMixerGroup (AudioSource audioSource, SoundType soundType, bool isSpeech = false)
		{
			#if UNITY_5
			if (audioSource != null && KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				if (audioSource.outputAudioMixerGroup != null)
				{
					return;
				}

				if (soundType == SoundType.Music)
				{
					if (KickStarter.settingsManager.musicMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.musicMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a music AudioMixerGroup!");
					}
				}
				else if (soundType == SoundType.SFX)
				{
					if (KickStarter.settingsManager.sfxMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.sfxMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a sfx AudioMixerGroup!");
					}
				}
				else if (soundType == AC.SoundType.Speech)
				{
					if (KickStarter.settingsManager.speechMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.speechMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a speech AudioMixerGroup!");
					}
				}
			}
			#endif
		}

		
		/**
		 * <summary>Returns the integer value of the AnimLayer enum.
		 * Necessary because two Neck layers are used, though only one is present in the enum.</summary>
		 * <param name = "animLayer">The AnimLayer enum</param>
		 * <returns>The integer value</returns>
		 */
		public static int GetAnimLayerInt (AnimLayer animLayer)
		{
			int layerInt = (int) animLayer;
			
			// Hack, because we actually use two neck layers
			if (layerInt > 4)
			{
				layerInt ++;
			}
			
			return layerInt;
		}
		

		/**
		 * Returns the References asset, which should be located in a Resources directory.
		 */
		public static References GetReferences ()
		{
			if (references == null)
			{
				references = (References) Resources.Load (Resource.references);
			}
			return references;
		}
		

		/**
		 * <summary>Runs an ActionList asset file.
		 * If the ActionList contains an Integer parameter, the parameter's value can be set here.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "parameterID">The ID of the parameter to set</param>
		 * <param name = "parameterValue">The value to set the parameter to, provided that IsIntegerBased returns True</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, int parameterID = -1, int parameterValue = 0)
		{
			if (parameterID >= 0 && actionListAsset != null && actionListAsset.useParameters && actionListAsset.parameters.Count > 0)
			{
				for (int i=0; i<actionListAsset.parameters.Count; i++)
				{
					if (actionListAsset.parameters[i].ID == parameterID)
					{
						if (actionListAsset.parameters[i].IsIntegerBased ())
						{
							actionListAsset.parameters[i].intValue = parameterValue;
						}
						else
						{
							ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + actionListAsset.parameters[i].label + "' because it's value is not integer-based.");
						}
						break;
					}
				}
			}

			return RunActionListAsset (actionListAsset, null, 0, false, true);
		}


		/**
		 * <summary>Runs an ActionList asset file, and sets the value of the first parameter, provided that it is a GameObject.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "parameterValue">The value to set the GameObject parameter to</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, GameObject parameterValue)
		{
			if (actionListAsset != null && actionListAsset.useParameters && actionListAsset.parameters.Count > 0)
			{
				if (actionListAsset.parameters[0].parameterType == ParameterType.GameObject)
				{
					actionListAsset.parameters[0].gameObject = parameterValue;
				}
				else
				{
					ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + actionListAsset.parameters[0].label + "' because it is not a GameObject!");
				}
			}
			
			return RunActionListAsset (actionListAsset, null, 0, false, true);
		}


		/**
		 * <summary>Runs an ActionList asset file, and sets the value of a parameter, provided that it is a GameObject.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "parameterID">The ID of the parameter to set</param>
		 * <param name = "parameterValue">The value to set the GameObject parameter to</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, int parameterID, GameObject parameterValue)
		{
			if (parameterID >= 0 && actionListAsset != null && actionListAsset.useParameters && actionListAsset.parameters.Count > 0)
			{
				for (int i=0; i<actionListAsset.parameters.Count; i++)
				{
					if (actionListAsset.parameters[i].ID == parameterID)
					{
						if (actionListAsset.parameters[i].parameterType == ParameterType.GameObject)
						{
							actionListAsset.parameters[i].gameObject = parameterValue;
							if (parameterValue.GetComponent <ConstantID>())
							{
								actionListAsset.parameters[i].intValue = parameterValue.GetComponent <ConstantID>().constantID;
							}
							else
							{
								ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + actionListAsset.parameters[i].label + "' because " + parameterValue + " has no Constant ID component.");
							}
						}
						else
						{
							ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + actionListAsset.parameters[i].label + "' because it is not of type 'Game Object'.");
						}
						break;
					}
				}
			}

			return RunActionListAsset (actionListAsset, null, 0, false, true);
		}
		

		/**
		 * <summary>Runs an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "i">The index of the Action to start from</param>
		 * <param name = "addToSkipQueue">True if the ActionList should be added to the skip queue</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, int i, bool addToSkipQueue)
		{
			return RunActionListAsset (actionListAsset, null, i, false, addToSkipQueue);
		}
		

		/**
		 * <summary>Runs an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "endConversation">The Conversation to enable when the ActionList is complete</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, Conversation endConversation)
		{
			return RunActionListAsset (actionListAsset, endConversation, 0, false, true);
		}
		

		/**
		 * <summary>Runs or skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "endConversation">The Conversation to enable when the ActionList is complete</param>
		 * <param name = "i">The index of the Action to start from</param>
		 * <param name = "doSkip">If True, all Actions within the ActionList will be run and completed instantly.</param>
		 * <param name = "addToSkipQueue">True if the ActionList should be added to the skip queue</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, Conversation endConversation, int i, bool doSkip, bool addToSkipQueue)
		{
			if (actionListAsset != null && actionListAsset.actions.Count > 0)
			{
				GameObject runtimeActionListObject = (GameObject) Instantiate (Resources.Load (Resource.runtimeActionList));
				RuntimeActionList runtimeActionList = runtimeActionListObject.GetComponent <RuntimeActionList>();
				runtimeActionList.DownloadActions (actionListAsset, endConversation, i, doSkip, addToSkipQueue);

				GameObject cutsceneFolder = GameObject.Find ("_Cutscenes");
				if (cutsceneFolder != null && cutsceneFolder.transform.position == Vector3.zero)
				{
					runtimeActionList.transform.parent = cutsceneFolder.transform;
				}
			
				return runtimeActionList;
			}
			
			return null;
		}


		/**
		 * <summary>Skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to skip</param>
		 * <returns>the temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList SkipActionListAsset (ActionListAsset actionListAsset)
		{
			return RunActionListAsset (actionListAsset, null, 0, true, false);
		}


		/**
		 * <summary>Skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to skip</param>
		 * <param name = "i">The index of the Action to skip from</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList SkipActionListAsset (ActionListAsset actionListAsset, int i)
		{
			return RunActionListAsset (actionListAsset, null, i, true, false);
		}


		/**
		 * <summary>Calculates a formula (Not available for Windows Phone devices).</summary>
		 * <param name = "formula">The formula string to calculate</param>
		 * <returns>The result</returns>
		 */
		public static double CalculateFormula (string formula)
		{
			#if UNITY_WP8 || UNITY_WINRT
			return 0;
			#else
			try
			{
				return ((double) new System.Xml.XPath.XPathDocument
				        (new System.IO.StringReader("<r/>")).CreateNavigator().Evaluate
				        (string.Format("number({0})", new System.Text.RegularExpressions.Regex (@"([\+\-\*])").Replace (formula, " ${1} ").Replace ("/", " div ").Replace ("%", " mod "))));
			}
			catch
			{
				ACDebug.LogWarning ("Cannot compute formula: " + formula);
				return 0;
			}
			#endif
		}


		/**
		 * <summary>Converts a string's tokens into their true values.
		 * The '[var:ID]' token will be replaced by the value of global variable 'ID'.
		 * The '[localvar:ID]' token will be replaced by the value of local variable 'ID'.</summary>
		 * <param name = "_text">The original string with tokens</param>
		 * <returns>The converted string without tokens</returns>
		 */
		public static string ConvertTokens (string _text)
		{
			return ConvertTokens (_text, Options.GetLanguage ());
		}
		

		/**
		 * <summary>Converts a string's tokens into their true values.
		 * The '[var:ID]' token will be replaced by the value of global variable 'ID'.
		 * The '[localvar:ID]' token will be replaced by the value of local variable 'ID'.</summary>
		 * <param name = "_text">The original string with tokens</param>
		 * <param name = "languageNumber">The index number of the game's current language</param>
		 * <param name = "localVariables">The LocalVariables script to read local variables from, if not the scene default</param>
		 * <returns>The converted string without tokens</returns>
		 */
		public static string ConvertTokens (string _text, int languageNumber, LocalVariables localVariables = null)
		{
			if (!Application.isPlaying)
			{
				return _text;
			}

			if (localVariables == null) localVariables = KickStarter.localVariables;
			
			if (_text != null)
			{
				if (_text.Contains ("[var:"))
				{
					foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
					{
						string tokenText = "[var:" + _var.id + "]";
						if (_text.Contains (tokenText))
						{
							_var.Download ();
							_text = _text.Replace (tokenText, _var.GetValue (languageNumber));
						}
					}
				}
				if (_text.Contains ("[localvar:"))
				{
					foreach (GVar _var in localVariables.localVars)
					{
						string tokenText = "[localvar:" + _var.id + "]";
						if (_text.Contains (tokenText))
						{
							_text = _text.Replace (tokenText, _var.GetValue (languageNumber));
						}
					}
				}

				if (KickStarter.runtimeVariables)
				{
					_text = KickStarter.runtimeVariables.ConvertCustomTokens (_text);
				}
			}
			
			return _text;
		}
		
		
		#if UNITY_EDITOR

		/**
		 * <summary>Draws a cube gizmo in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "color">The colour of the cube</param>
		 */
		public static void DrawCubeCollider (Transform transform, Color color)
		{
			if (transform.GetComponent <BoxCollider2D>() != null)
			{
				BoxCollider2D _boxCollider2D = transform.GetComponent <BoxCollider2D>();
				Vector2 pos = UnityVersionHandler.GetBoxCollider2DCentre (_boxCollider2D);

				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = color;
				Gizmos.DrawCube (pos, _boxCollider2D.size);
				Gizmos.matrix = Matrix4x4.identity;
			}
			else if (transform.GetComponent <BoxCollider>() != null)
			{
				BoxCollider _boxCollider = transform.GetComponent <BoxCollider>();

				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = color;
				Gizmos.DrawCube (_boxCollider.center, _boxCollider.size);
				Gizmos.matrix = Matrix4x4.identity;
			}
		}


		/**
		 * <summary>Draws a box gizmo in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "color">The colour of the box</param>
		 */
		public static void DrawBoxCollider (Transform transform, Color color)
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = color;
			Gizmos.DrawLine (new Vector3 (-0.5f, -0.5f), new Vector3 (-0.5f, 0.5f));
			Gizmos.DrawLine (new Vector3 (-0.5f, 0.5f), new Vector3 (0.5f, 0.5f));
			Gizmos.DrawLine (new Vector3 (0.5f, 0.5f), new Vector3 (0.5f, -0.5f));
			Gizmos.DrawLine (new Vector3 (0.5f, -0.5f), new Vector3 (-0.5f, -0.5f));
		}


		/**
		 * <summary>Draws an outline of a Polygon Collider 2D in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "poly">The Polygon Collider 2D</param>
		 * <param name = "color">The colour of the outline</param>
		 */
		public static void DrawPolygonCollider (Transform transform, PolygonCollider2D poly, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawLine (transform.TransformPoint (poly.points [0]), transform.TransformPoint (poly.points [poly.points.Length-1]));
			for (int i=0; i<poly.points.Length-1; i++)
			{
				Gizmos.DrawLine (transform.TransformPoint (poly.points [i]), transform.TransformPoint (poly.points [i+1]));
			}
		}


		/**
		 * <summary>Locates an object with a supplied ConstantID number (Unity Editor only).
		 * If the object is not found in the current scene, all scenes in the Build Settings will be searched.
		 * Once an object is found, it will be pinged in the Hierarchy window.</summary>
		 * <param name = "_constantID">The ConstantID number of the object to find</param>
		 */
		public static void FindObjectWithConstantID (int _constantID)
		{
			string originalScene = UnityVersionHandler.GetCurrentSceneName ();
			
			if (UnityVersionHandler.SaveSceneIfUserWants ())
			{
				// Search scene files for ID
				string[] sceneFiles = GetSceneFiles ();
				foreach (string sceneFile in sceneFiles)
				{
					UnityVersionHandler.OpenScene (sceneFile);

					ConstantID[] idObjects = FindObjectsOfType (typeof (ConstantID)) as ConstantID[];
					if (idObjects != null && idObjects.Length > 0)
					{
						foreach (ConstantID idObject in idObjects)
						{
							if (idObject.constantID == _constantID)
							{
								ACDebug.Log ("Found Constant ID: " + _constantID + " on '" + idObject.gameObject.name + "' in scene: " + sceneFile);
								EditorGUIUtility.PingObject (idObject.gameObject);
								EditorGUIUtility.ExitGUI ();
								return;
							}
						}
					}
				}
				
				ACDebug.LogWarning ("Cannot find object with Constant ID: " + _constantID);
				UnityVersionHandler.OpenScene (originalScene);
			}
		}
		

		/**
		 * <summary>Returns all scene filenames listed in the Build Settings (Unity Editor only).</summary>
		 * <returns>An array of scene filenames as strings</returns>
		 */
		public static string[] GetSceneFiles ()
		{
			List<string> temp = new List<string>();
			foreach (UnityEditor.EditorBuildSettingsScene S in UnityEditor.EditorBuildSettings.scenes)
			{
				if (S.enabled)
				{
					temp.Add(S.path);
				}
			}
			
			return temp.ToArray();
		}


		/**
		 * <summary>Generates a Global Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected global variable's ID number</param>
		 * <returns>The newly-selected global variable's ID number</returns>
		 */
		public static int GlobalVariableGUI (string label, int variableID)
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				int variableNumber = -1;

				if (variablesManager.vars.Count > 0)
				{
					foreach (GVar _var in variablesManager.vars)
					{
						labelList.Add (_var.label);
						
						// If a GlobalVar variable has been removed, make sure selected variable is still valid
						if (_var.id == variableID)
						{
							variableNumber = i;
						}
						
						i++;
					}
					
					if (variableNumber == -1)
					{
						// Wasn't found (variable was deleted?), so revert to zero
						ACDebug.LogWarning ("Previously chosen variable no longer exists!");
						variableNumber = 0;
						variableID = 0;
					}
					
					variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray());
					variableID = variablesManager.vars [variableNumber].id;
				}
				else
				{
					EditorGUILayout.HelpBox ("No global variables exist!", MessageType.Info);
					variableID = -1;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No Variables Manager exists!", MessageType.Info);
				variableID = -1;
			}

			return variableID;
		}
		

		/**
		 * <summary>Draws a curve between two Actions in the ActionList Editor window (Unity Editor only).</summary>
		 * <param name = "start">The Rect of the Action to draw from</param>
		 * <param name = "end">The Rect of the Action to draw to</param>
		 * <param name = "color">The colour of the curve</param>
		 * <param name = "offset">How far the line should be offset along the rect</param>
		 * <param name = "onSide">True if the curve should begin on the side of the Action</param>
		 * <param name = "isDisplayed">True if the Action to draw from is expanded</param>
		 */
		public static void DrawNodeCurve (Rect start, Rect end, Color color, int offset, bool onSide, bool isDisplayed)
		{
			float endOffset = 0f;
			if (onSide)
			{
				endOffset = ((float) offset)/4f;
			}

			bool arrangeVertically = true;
			if (AdvGame.GetReferences ().actionsManager && AdvGame.GetReferences ().actionsManager.displayActionsInEditor == DisplayActionsInEditor.ArrangedHorizontally)
			{
				arrangeVertically = false;
			}

			Color originalColor = GUI.color;
			GUI.color = color;

			if (arrangeVertically)
			{
				Vector2 endPos = new Vector2 (end.x + end.width / 2f + endOffset, end.y - 8);
				DrawNodeCurve (start, endPos, color, offset, onSide, !arrangeVertically, isDisplayed);
				Texture2D arrow = (Texture2D) AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Graphics/Textures/node-arrow.png", typeof (Texture2D));
				GUI.Label (new Rect (endPos.x-5, endPos.y-4, 12, 16), arrow, "Label");
			}
			else
			{
				Vector2 endPos = new Vector2 (end.x - 8f, end.y + 10 + endOffset);
				DrawNodeCurve (start, endPos, color, offset, onSide, !arrangeVertically, isDisplayed);
				Texture2D arrow = (Texture2D) AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Graphics/Textures/node-arrow-side.png", typeof (Texture2D));
				GUI.Label (new Rect (endPos.x-4, endPos.y-7, 16, 12), arrow, "Label");
			}

			GUI.color = originalColor;
		}
		
		
		/**
		 * <summary>Draws a curve between two Actions in the ActionList Editor window (Unity Editor only).</summary>
		 * <param name = "start">The Rect of the Action to draw from</param>
		 * <param name = "end">The point to draw to</param>
		 * <param name = "color">The colour of the curve</param>
		 * <param name = "offset">How far the line should be offset along the rect</param>
		 * <param name = "fromSide">True if the curve should begin on the side of the Action</param>
		 * <param name = "toSide">True if the curve should end on the side of the Action</param>
		 * <param name = "isDisplayed">True if the Action to draw from is expanded</param>
		 */
		public static void DrawNodeCurve (Rect start, Vector2 end, Color color, int offset, bool fromSide, bool toSide, bool isDisplayed)
		{
			Vector3 endPos = new Vector3(end.x, end.y - 1, 0);

			if (fromSide)
			{
				if (!isDisplayed)
				{
					offset = 0;
				}
				Vector3 startPos = new Vector3(start.x + start.width + 10, start.y + start.height - offset - 4, 0);
				if (!isDisplayed)
				{
					startPos.x -= 10;
				}
				float dist = Mathf.Abs (startPos.y - endPos.y);

				Vector3 startTan = startPos + Vector3.right * Mathf.Min (Mathf.Abs (startPos.x - endPos.x), 200f) / 2f;

				if (toSide)
				{
					Vector3 endTan = endPos + Vector3.left * Mathf.Min (dist, 200) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
				else
				{
					Vector3 endTan = endPos + Vector3.down * Mathf.Min (dist, 200) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
			}
			else
			{
				Vector3 startPos = new Vector3(start.x + start.width / 2f, start.y + start.height + offset + 2, 0);
				float dist = Mathf.Abs (startPos.y - endPos.y);
				Vector3 startTan = startPos + Vector3.up * Mathf.Min (dist, 200f) / 2f;
				if (endPos.y < startPos.y && endPos.x <= startPos.x && !toSide)
				{
					startTan.x -= Mathf.Min (dist, 200f) / 2f;
				}

				if (toSide)
				{
					Vector3 endTan = endPos + Vector3.left * Mathf.Min (dist, 200f) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
				else
				{
					Vector3 endTan = endPos + Vector3.down * Mathf.Min (dist, 200f) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
			}
		}


		private static Texture2D adLineTex
		{
			get
			{
				if (!_aaLineTex)
				{
					_aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, true);
					_aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
					_aaLineTex.SetPixel(0, 1, Color.white);
					_aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
					_aaLineTex.Apply();
				}
				return _aaLineTex;
			}
		}
		

		/**
		 * Duplicates the Actions within the copy buffer, so that they do not reference their original source (Unity Editor only).
		 */
		public static void DuplicateActionsBuffer ()
		{
			List<AC.Action> tempList = new List<AC.Action>();
			foreach (Action action in copiedActions)
			{
				if (action != null)
				{
					Action copyAction = Object.Instantiate (action) as Action;
					copyAction.skipActionActual = null;
					tempList.Add (copyAction);
				}
			}
			
			copiedActions.Clear ();
			copiedActions = tempList;
		}


		public static LayerMask LayerMaskField (string label, LayerMask layerMask)
		{
			List<int> layerNumbers = new List<int>();
			string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
			
			for (int i = 0; i < layers.Length; i++)
				layerNumbers.Add(LayerMask.NameToLayer(layers[i]));
			
			int maskWithoutEmpty = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if (((1 << layerNumbers[i]) & layerMask.value) > 0)
					maskWithoutEmpty |= (1 << i);
			}
			
			maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);
			
			int mask = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if ((maskWithoutEmpty & (1 << i)) > 0)
					mask |= (1 << layerNumbers[i]);
			}
			layerMask.value = mask;
			
			return layerMask;
		}

		#endif


		/**
		 * <summary>Returns the vector between two world-space points when converted to screen-space.</summary?
		 * <param name = "originWorldPosition">The first point in world-space</param>
		 * <param name = "targetWorldPosition">The second point in world-space<param>
		 * <returns>The vector between the two points in screen-space</returns>
		 */
		public static Vector3 GetScreenDirection (Vector3 originWorldPosition, Vector3 targetWorldPosition)
		{
			Vector3 originScreenPosition = Camera.main.WorldToScreenPoint (originWorldPosition);
			Vector3 targetScreenPosition = Camera.main.WorldToScreenPoint (targetWorldPosition);
			
			Vector3 lookVector = targetScreenPosition - originScreenPosition;
			lookVector.z = lookVector.y;
			lookVector.y = 0;
			
			return (lookVector);
		}
		

		/**
		 * <summary>Returns the percieved point on a NavMesh of a world-space position, when viewed through screen-space.</summary>
		 * <param name = "targetWorldPosition">The position in world-space<param>
		 * <returns>The point on the NavMesh that the position lies when viewed through screen-space</returns>
		 */
		public static Vector3 GetScreenNavMesh (Vector3 targetWorldPosition)
		{
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			
			Vector3 targetScreenPosition = Camera.main.WorldToScreenPoint (targetWorldPosition);
			Ray ray = Camera.main.ScreenPointToRay (targetScreenPosition);
			RaycastHit hit = new RaycastHit();
			
			if (settingsManager && Physics.Raycast (ray, out hit, settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (settingsManager.navMeshLayer)))
			{
				return hit.point;
			}
			
			return targetWorldPosition;
		}
		

		/**
		 * <summary>Returns the screen dimensions as a vector.</summary>
		 * <returns>The screen dimensions</returns>
		 */
		public static Vector2 GetMainGameViewSize ()
		{
			#if UNITY_EDITOR && !UNITY_5_4_OR_NEWER
			System.Type T = System.Type.GetType("UnityEditor.GameView, UnityEditor");
			System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod ("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			System.Object Res = GetSizeOfMainGameView.Invoke (null, null);
			return (Vector2) Res;
			#else
			return new Vector2 (Screen.width, Screen.height);
			#endif
		}
		

		/**
		 * <summary>Sets the vanishing point of a perspective-locked camera.</summary>
		 * <param name = "_camera">The Camera to affect</param>
		 * <param name = "perspectiveOffset">The offset from the perspective's centre</param>
		 * <returns>A Matrix4x4 of the corrected perspective</returns>
		 */
		public static Matrix4x4 SetVanishingPoint (Camera _camera, Vector2 perspectiveOffset)
		{
			Matrix4x4 m = _camera.projectionMatrix;
			float w = 2f * _camera.nearClipPlane / m.m00;
			float h = 2f * _camera.nearClipPlane / m.m11;
			
			float left = -(w / 2) + perspectiveOffset.x;
			float right = left + w;
			float bottom = -(h / 2) + perspectiveOffset.y;
			float top = bottom + h;
			
			return (PerspectiveOffCenter (left, right, bottom, top, _camera.nearClipPlane, _camera.farClipPlane));
		}
		
		
		private static Matrix4x4 PerspectiveOffCenter (float left, float right, float bottom, float top, float near, float far)
		{
			float x =  (2f * near) / (right - left);
			float y =  (2f * near) / (top - bottom);
			float a =  (right + left) / (right - left);
			float b =  (top + bottom) / (top - bottom);
			float c = -(far + near) / (far - near);
			float d = -(2f * far * near) / (far - near);
			float e = -1f;
			
			Matrix4x4 m = new Matrix4x4();
			m[0,0] = x;		m[0,1] = 0f;	m[0,2] = a;		m[0,3] = 0f;
			m[1,0] = 0f;	m[1,1] = y;		m[1,2] = b;		m[1,3] = 0f;
			m[2,0] = 0f;	m[2,1] = 0f;	m[2,2] = c;		m[2,3] =   d;
			m[3,0] = 0f;	m[3,1] = 0f;	m[3,2] = e;		m[3,3] = 0f;
			return m;
		}
		

		/**
		 * <summary>Generates a unique name for a GameObject by adding numbers to the end of it.</summary>
		 * <param name = "name">The original name of the GameObject</param>
		 * <returns>A unique name for the GameObject</retuns>
		 */
		public static string UniqueName (string name)
		{
			if (GameObject.Find (name))
			{
				string newName = name;
				
				for (int i=2; i<20; i++)
				{
					newName = name + i.ToString ();
					
					if (!GameObject.Find (newName))
					{
						break;
					}
				}
				
				return newName;
			}
			else
			{
				return name;
			}
		}
		

		/**
		 * <summary>Gets the name of an asset file, given its path.</summary>
		 * <param name = "resourceName">The full path of the asset file</param>
		 * <returns>The name of the asset</returns>
		 */
		public static string GetName (string resourceName)
		{
			int slash = resourceName.IndexOf ("/");
			string newName;
			
			if (slash > 0)
			{
				newName = resourceName.Remove (0, slash+1);
			}
			else
			{
				newName = resourceName;
			}
			
			return newName;
		}


		/**
		 * <summary>Generates a Rect from a square.</summary>
		 * <param name = "centre_x">The centre of the square in the x-direction</param>
		 * <param name = "centre_y">The centre of the square in the y-direction</param>
		 * <param name = "size">The size of the square</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIBox (float centre_x, float centre_y, float size)
		{
			Rect newRect;
			newRect = GUIRect (centre_x, centre_y, size, size);
			return (newRect);
		}
		
		
		/**
		 * <summary>Generates a Rect from a square.</summary>
		 * <param name = "posVector">The top-left corner of the square</param>
		 * <param name = "size">The size of the square</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIBox (Vector2 posVector, float size)
		{
			return GUIRect (posVector.x / Screen.width, (Screen.height - posVector.y) / Screen.height, size, size);
		}


		/**
		 * <summary>Generates a Rect from a rectangle.</summary>
		 * <param name = "centre_x">The centre of the rectangle in the x-direction</param>
		 * <param name = "centre_y">The centre of the rectangle in the y-direction</param>
		 * <param name = "width">The width of the rectangle</param>
		 * <param name = "height">The height of the rectangle</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIRect (float centre_x, float centre_y, float width, float height)
		{
			Rect newRect;
			newRect = new Rect (Screen.width * centre_x - (Screen.width * width)/2, Screen.height * centre_y - (Screen.width * height)/2, Screen.width * width, Screen.width * height);
			return (newRect);
		}


		private static void AddAnimClip (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode, WrapMode wrapMode, Transform mixingBone)
		{
			if (clip != null && _animation != null)
			{
				// Initialises a clip
				_animation.AddClip (clip, clip.name);
				
				if (mixingBone != null)
				{
					_animation [clip.name].AddMixingTransform (mixingBone);
				}
				
				// Set up the state
				if (_animation [clip.name])
				{
					_animation [clip.name].layer = layer;
					_animation [clip.name].normalizedTime = 0f;
					_animation [clip.name].blendMode = blendMode;
					_animation [clip.name].wrapMode = wrapMode;
					_animation [clip.name].enabled = true;
				}
			}
		}
		

		/**
		 * <summary>Initialises and plays a legacy AnimationClip on an Animation component, starting from a set point.</summary>
		 * <param name = "_animation">The Animation component</param>
		 * <param name = "layer">The layer to play the animation on</param>
		 * <param name = "clip">The AnimatonClip to play</param>
		 * <param name = "blendMode">The animation's AnimationBlendMode</param>
		 * <param name = "wrapMode">The animation's WrapMode</param>
		 * <param name = "fadeTime">The transition time to the new animation</param>
		 * <param name = "mixingBone">The transform to set as the animation's mixing transform</param>
		 * <param name = "normalisedFrame">How far along the timeline the animation should start from (0 to 1)</param>
		 */
		public static void PlayAnimClipFrame (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode, WrapMode wrapMode, float fadeTime, Transform mixingBone, float normalisedFrame)
		{
			if (clip != null)
			{
				AddAnimClip (_animation, layer, clip, blendMode, wrapMode, mixingBone);
				_animation [clip.name].normalizedTime = normalisedFrame;
				_animation [clip.name].speed *= 1f;
				_animation.Play (clip.name);
				CleanUnusedClips (_animation);
			}
		}
		
		
		/**
		 * <summary>Initialises and plays a legacy AnimationClip on an Animation component.</summary>
		 * <param name = "_animation">The Animation component</param>
		 * <param name = "layer">The layer to play the animation on</param>
		 * <param name = "clip">The AnimatonClip to play</param>
		 * <param name = "blendMode">The animation's AnimationBlendMode</param>
		 * <param name = "wrapMode">The animation's WrapMode</param>
		 * <param name = "fadeTime">The transition time to the new animation</param>
		 * <param name = "mixingBone">The transform to set as the animation's mixing transform</param>
		 * <param name = "reverse">True if the animation should be reversed</param>
		 */
		public static void PlayAnimClip (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode = AnimationBlendMode.Blend, WrapMode wrapMode = WrapMode.ClampForever, float fadeTime = 0f, Transform mixingBone = null, bool reverse = false)
		{
			if (clip != null)
			{
				AddAnimClip (_animation, layer, clip, blendMode, wrapMode, mixingBone);
				if (reverse)
				{
					_animation[clip.name].speed *= -1f;
				}
				_animation.CrossFade (clip.name, fadeTime);
				CleanUnusedClips (_animation);
			}
		}
		

		/**
		 * <summary>Cleans the supplied Animation component of any clips not being played.</summary>
		 * <param name = "_animation">The Animation component to clean</param>
		 */
		public static void CleanUnusedClips (Animation _animation)
		{
			// Remove any non-playing animations
			List <string> removeClips = new List <string>();
			
			foreach (AnimationState state in _animation)
			{
				if (!_animation [state.name].enabled)
				{
					// Queued animations get " - Queued Clone" appended to it, so remove
					
					int queueIndex = state.name.IndexOf (" - Queued Clone");
					
					if (queueIndex > 0)
					{
						removeClips.Add (state.name.Substring (0, queueIndex));
					}
					else
					{
						removeClips.Add (state.name);
					}
				}
			}
			
			foreach (string _clip in removeClips)
			{
				_animation.RemoveClip (_clip);
			}
		}
		

		/**
		 * <summary>Lerps from one float to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value</param>
		 * <returns>The lerped float</returns>
		 */
		public static float Lerp (float from, float to, float t)
		{
			if (t <= 1)
			{
				return Mathf.Lerp (from, to, t);
			}
			
			return from + (to-from)*t;
		}
		
		
		/**
		 * <summary>Lerps from one Vector3 to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value</param>
		 * <returns>The lerped Vector3</returns>
		 */
		public static Vector3 Lerp (Vector3 from, Vector3 to, float t)
		{
			if (t <= 1)
			{
				return Vector3.Lerp (from, to, t);
			}
			
			return from + (to-from)*t;
		}
		

		/**
		 * <summary>Lerps from one Quaternion to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value</param>
		 * <returns>The lerped Quaternion</returns>
		 */
		public static Quaternion Lerp (Quaternion from, Quaternion to, float t)
		{
			if (t <= 1)
			{
				return Quaternion.Lerp (from, to, t);
			}
			
			Vector3 fromVec = from.eulerAngles;
			Vector3 toVec = to.eulerAngles;
			
			if (fromVec.x - toVec.x > 180f)
			{
				toVec.x -= 360f;
			}
			else if (fromVec.x - toVec.x > 180f)
			{
				toVec.x += 360;
			}
			if (fromVec.y - toVec.y < -180f)
			{
				toVec.y -= 360f;
			}
			else if (fromVec.y - toVec.y > 180f)
			{
				toVec.y += 360;
			}
			if (fromVec.z - toVec.z > 180f)
			{
				toVec.z -= 360f;
			}
			else if (fromVec.z - toVec.z > 180f)
			{
				toVec.z += 360;
			}
			
			return Quaternion.Euler (Lerp (fromVec, toVec, t));
		}
		

		/**
		 * <summary>Interpolates a float over time, according to various interpolation methods.</summary>
		 * <param name = "startT">The starting time</param>
		 * <param name = "deltaT">The time difference</param>
		 * <param name = "moveMethod">The method of interpolation (Linear, Smooth, Curved, EaseIn, EaseOut, Curved)</param>
		 * <param name = "timeCurve">The AnimationCurve to interpolate against, if the moveMethod = MoveMethod.Curved</param>
		 * <returns>The interpolated float</returns>
		 */
		public static float Interpolate (float startT, float deltaT, MoveMethod moveMethod, AnimationCurve timeCurve = null)
		{
			if (moveMethod == MoveMethod.Curved)
			{
				moveMethod = MoveMethod.Smooth;
			}
			
			else if (moveMethod == MoveMethod.Smooth)
			{
				return -0.5f * (Mathf.Cos (Mathf.PI * (Time.time - startT) / deltaT) - 1f);
			}
			else if (moveMethod == MoveMethod.EaseIn)
			{
				return 1f - Mathf.Cos ((Time.time - startT) / deltaT * (Mathf.PI / 2));
			}
			else if (moveMethod == MoveMethod.EaseOut)
			{
				return Mathf.Sin ((Time.time - startT) / deltaT * (Mathf.PI / 2));
			}
			else if (moveMethod == MoveMethod.CustomCurve)
			{
				if (timeCurve == null || timeCurve.length == 0)
				{
					return 1f;
				}
				float startTime = timeCurve [0].time;
				float endTime = timeCurve [timeCurve.length - 1].time;
				
				return timeCurve.Evaluate ((endTime - startTime) * (Time.time - startT) / deltaT + startTime);
			}
			
			return ((Time.time - startT) / deltaT);
		}
		

		/**
		 * <summary>Draws GUI text with an outline and/or shadow.</summary>
		 * <param name = "rect">The Rect of the GUI text</param>
		 * <param name = "text">The text itself</param>
		 * <param name = "style">The GUIStyle that the GUI text uses</param>
		 * <param name = "outColour">The colour of the text's outline/shadow</param>
		 * <param name = "inColour">The colour of the text itself</param>
		 * <param name = "size">The size of the text</param>
		 * <param name = "textEffects">The type of text effect (Outline, Shadow, OutlineAndShadow, None)</param>
		 */
		public static void DrawTextEffect (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, TextEffects textEffects)
		{
			if (AdvGame.GetReferences ().menuManager != null && AdvGame.GetReferences ().menuManager.scaleTextEffects)
			{
				size = AdvGame.GetMainGameViewSize ().x / 200f / size;
			}

			int i=0;
			string effectText = text;

			if (effectText != null)
			{
				while (i < text.Length && text.IndexOf ("<color=", i) > 0)
				{
					int startPos = effectText.IndexOf ("<color=", i);
					int endPos = 0;
					if (effectText.IndexOf (">", startPos) > 0)
					{
						endPos = effectText.IndexOf (">", startPos);
					}

					if (endPos > 0)
					{
						effectText = effectText.Substring (0, startPos) + "<color=black>" + effectText.Substring (endPos + 1);
					}

					i = startPos + i;
				}

				if (textEffects == TextEffects.Outline || textEffects == TextEffects.OutlineAndShadow)
				{
					AdvGame.DrawTextOutline (rect, text, style, outColor, inColor, size, effectText);
				}
				if (textEffects == TextEffects.Shadow || textEffects == TextEffects.OutlineAndShadow)
				{
					AdvGame.DrawTextShadow (rect, text, style, outColor, inColor, size, effectText);
				}
			}
		}
		
		
		private static void DrawTextShadow (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, string effectText = "")
		{
			GUIStyle backupStyle = new GUIStyle(style);
			Color backupColor = GUI.color;

			if (effectText.Length == 0)
			{
				effectText = text;
			}

			if (style.normal.background != null)
			{
				GUI.Label(rect, "", style);
			}
			style.normal.background = null;

			outColor.a = GUI.color.a;
			style.normal.textColor = outColor;
			GUI.color = outColor;
			
			rect.x += size;
			GUI.Label(rect, effectText, style);
			
			rect.y += size;
			GUI.Label(rect, effectText, style);
			
			rect.x -= size;
			rect.y -= size;
			style.normal.textColor = inColor;
			GUI.color = backupColor;
			GUI.Label(rect, text, style);
			
			style = backupStyle;
		}
		
		
		private static void DrawTextOutline (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, string effectText = "")
		{
			float halfSize = size * 0.5F;
			GUIStyle backupStyle = new GUIStyle(style);
			Color backupColor = GUI.color;

			if (effectText.Length == 0)
			{
				effectText = text;
			}

			if (style.normal.background != null)
			{
				GUI.Label(rect, "", style);
			}
			style.normal.background = null;
			
			outColor.a = GUI.color.a;
			style.normal.textColor = outColor;
			GUI.color = outColor;
			
			rect.x -= halfSize;
			GUI.Label(rect, effectText, style);

			rect.y -= halfSize;
			GUI.Label(rect, effectText, style);

			rect.x += halfSize;
			GUI.Label(rect, effectText, style);

			rect.x += halfSize;
			GUI.Label(rect, effectText, style);

			rect.y += halfSize;
			GUI.Label(rect, effectText, style);

			rect.y += halfSize;
			GUI.Label(rect, effectText, style);

			rect.x -= halfSize;
			GUI.Label(rect, effectText, style);

			rect.x -= halfSize;
			GUI.Label(rect, effectText, style);

			rect.x += halfSize;
			rect.y -= halfSize;
			style.normal.textColor = inColor;
			GUI.color = backupColor;
			GUI.Label(rect, text, style);
			
			style = backupStyle;
		}


		/*
		 * <summary>Converts any special characters in a string that might conflict with save game file data into temporary replacements.</summary>
		 * <param name = "_string">The original string</param>
		 * <returns>The modified string, ready to be placed in save game file data</returns>
		 */
		public static string PrepareStringForSaving (string _string)
		{
			_string = _string.Replace (SaveSystem.pipe, "*PIPE*");
			_string = _string.Replace (SaveSystem.colon, "*COLON*");
			
			return _string;
		}
		
		
		/*
		 * <summary>Converts temporarily replacements in a string back into special characters that might conflict with save game file data.</summary>
		 * <param name = "_string">The string to convert</param>
		 * <returns>The original string</returns>
		 */
		public static string PrepareStringForLoading (string _string)
		{
			_string = _string.Replace ("*PIPE*", SaveSystem.pipe);
			_string = _string.Replace ("*COLON*", SaveSystem.colon);
			
			return _string;
		}

	}
	
}	