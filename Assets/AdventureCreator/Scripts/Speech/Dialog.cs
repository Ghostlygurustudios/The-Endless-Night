/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Dialog.cs"
 * 
 *	This script handles the running of dialogue lines, speech or otherwise.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AC
{

	/**
	 * Manages the creation, updating, and removal of all Speech lines.
	 * It should be placed on the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_dialog.html")]
	#endif
	public class Dialog : MonoBehaviour
	{

		/** A List of all active Speech lines */
		public List<Speech> speechList = new List<Speech>();

		private AudioSource defaultAudioSource;


		public void OnAwake ()
		{
			if (KickStarter.speechManager.textScrollSpeed == 0f)
			{
				ACDebug.LogError ("Cannot have a Text Scroll Speed of zero - please amend your Speech Manager");
			}
			
			if (KickStarter.sceneSettings.defaultSound && KickStarter.sceneSettings.defaultSound.GetComponent <AudioSource>())
			{
				defaultAudioSource = this.GetComponent <SceneSettings>().defaultSound.GetComponent <AudioSource>();
			}
		}


		/**
		 * Updates all active Speech lines.
		 * This is called every frame by StateHandler.
		 */
		public void _Update ()
		{
			if (KickStarter.stateHandler.gameState != GameState.Paused)
			{
				for (int i=0; i<speechList.Count; i++)
				{
					speechList[i].UpdateInput ();
				}
			}
		}


		public void _FixedUpdate ()
		{
			for (int i=0; i<speechList.Count; i++)
			{
				speechList[i]._FixedUpdate ();
				if (!speechList[i].isAlive)
				{
					EndSpeech (i);
					return;
				}
			}
		}
		

		/**
		 * <summary>Initialises a new Speech line.</summary>
		 * <param name = "_speaker">The speaking character. If null, the line will be treated as narration</param>
		 * <param name = "_text">The subtitle text to display</param>
		 * <param name = "isBackground">True if the line should play in the background, and not interrupt any Actions or gameplay</param>
		 * <param name = "lineID">The ID number of the line, if it is listed in the Speech Manager</param>
		 * <param name = "noAnimation">True if the character should not play a talking animation</param>
		 * <returns>The generated Speech line</returns>
		 */
		public Speech StartDialog (Char _speaker, string _text, bool isBackground = false, int lineID = -1, bool noAnimation = false)
		{
			// Get the language
			string _language = "";
			int lanuageNumber = Options.GetLanguage ();
			if (lanuageNumber > 0)
			{
				// Not in original language, so pull translation in from Speech Manager
				_language = KickStarter.runtimeLanguages.Languages [lanuageNumber];
			}

			// Remove speaker's previous line
			for (int i=0; i<speechList.Count; i++)
			{
				if (speechList[i].GetSpeakingCharacter () == _speaker)
				{
					EndSpeech (i);
					i=0;
				}
			}
			
			Speech speech = new Speech (_speaker, _text, lineID, _language, isBackground, noAnimation);
			speechList.Add (speech);

			KickStarter.runtimeVariables.AddToSpeechLog (speech.log);
			KickStarter.playerMenus.AssignSpeechToMenu (speech);

			if (speech.hasAudio)
			{
				if (KickStarter.speechManager.relegateBackgroundSpeechAudio)
				{
					EndBackgroundSpeechAudio (speech);
				}

				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}

			return speech;
		}


		/**
		 * <summary>Gets the default AudioSource, as set in the Scene Manager.</summary>
		 * <returns>The default AudioSource</returns>
		 */
		public AudioSource GetDefaultAudioSource ()
		{
			return defaultAudioSource;
		}


		/**
		 * <summary>Plays text-scoll audio for a given character. If no character is speaking, narration text-scroll audio will be played instead.</summary>
		 * <param name = "_speaker">The speaking character</param>
		 */
		public void PlayScrollAudio (AC.Char _speaker)
		{
			AudioClip textScrollClip = KickStarter.speechManager.textScrollCLip;

			if (_speaker == null)
			{
				textScrollClip = KickStarter.speechManager.narrationTextScrollCLip;
			}
			else if (_speaker != null && _speaker.textScrollClip != null)
			{
				textScrollClip = _speaker.textScrollClip;
			}

			if (textScrollClip != null)
			{
				if (defaultAudioSource)
				{
					if (KickStarter.speechManager.playScrollAudioEveryCharacter || !defaultAudioSource.isPlaying)
					{
						defaultAudioSource.PlayOneShot (textScrollClip);
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot play text scroll audio clip as no 'Default' sound prefab has been defined in the Scene Manager");
				}
			}
		}


		/**
		 * <summary>Gets the last Speech line to be played.</summary>
		 * <returns>The last item in the speechList List</returns>
		 */
		public Speech GetLatestSpeech ()
		{
			if (speechList.Count > 0)
			{
				return speechList [speechList.Count - 1];
			}
			return null;
		}


		/**
		 * <summary>Checks if any of the active Speech lines have audio.</summary>
		 * <returns>True if any active Speech lines have audio</returns>
		 */
		public bool FoundAudio ()
		{
			foreach (Speech speech in speechList)
			{
				if (speech.hasAudio)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Gets the display name of the most recently-speaking character.</summary>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The character's display name</returns>
		 */
		public string GetSpeaker (int languageNumber = 0)
		{
			if (speechList.Count > 0)
			{
				return GetLatestSpeech ().GetSpeaker (languageNumber);
			}			
			return "";
		}


		/**
		 * <summary>Checks if a given character is speaking.</summary>
		 * <param name = "_char".The character to check</param>
		 * <returns>True if the character is speaking</returns>
		 */
		public bool CharacterIsSpeaking (Char _char)
		{
			foreach (Speech speech in speechList)
			{
				if (speech.GetSpeakingCharacter () == _char)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Gets the most recently-speaking character.</summary>
		 * <returns>The character</returns>
		 */
		public AC.Char GetSpeakingCharacter ()
		{
			if (speechList.Count > 0)
			{
				return GetLatestSpeech ().GetSpeakingCharacter ();
			}
			return null;
		}


		/**
		 * <summary>Checks if any of the active Speech lines have audio and are playing it.</summary>
		 * <returns>True if any active Speech lines have audio and are playing it</returns>
		 */
		public bool AudioIsPlaying ()
		{
			if (Options.optionsData != null && Options.optionsData.speechVolume > 0f)
			{
				foreach (Speech speech in speechList)
				{
					if (speech.hasAudio && speech.isAlive)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Kills all active Speech lines.</summary>
		 * <param name = "stopCharacter">If True, then all characters speaking will cease their talking animation</param>
		 * <param name = "forceMenusOff">True if subtitles should be turned off immediately</param>
		 */
		public void KillDialog (bool stopCharacter, bool forceMenusOff)
		{
			for (int i=0; i<speechList.Count; i++)
			{
				EndSpeech (i, stopCharacter);
				i=0;
			}
			
			KickStarter.stateHandler.UpdateAllMaxVolumes ();
			if (forceMenusOff)
			{
				KickStarter.playerMenus.ForceOffSubtitles ();
			}
		}


		/**
		 * <summary>Generates the animation data for lipsyncing a given Speech line.</summary>
		 * <param name = "_lipSyncMode">The chosen method of lipsyncing (Off, FromSpeechText, ReadPamelaFile, ReadSapiFile, ReadPapagayoFile, FaceFX, Salsa2D)</param>
		 * <param name = "lineNumber">The speech line's ID number</param>
		 * <param name = "speakerName">The filename of the speaking character</param>
		 * <param name = "language">The name of the current language</param>
		 * <param name = "_message">The speech text</param<
		 * <returns>A List of LipSyncShape structs that contain the lipsync animation data</returns>
		 */
		public List<LipSyncShape> GenerateLipSyncShapes (LipSyncMode _lipSyncMode, int lineNumber, string speakerName, string language = "", string _message = "")
		{
			List<LipSyncShape> lipSyncShapes = new List<LipSyncShape>();
			lipSyncShapes.Add (new LipSyncShape (0, 0f, KickStarter.speechManager.lipSyncSpeed));
			TextAsset textFile = null;

			if (_lipSyncMode == LipSyncMode.Salsa2D)
			{
				return lipSyncShapes;
			}
			
			if (lineNumber > -1 && speakerName != "" && KickStarter.speechManager.searchAudioFiles && KickStarter.speechManager.UseFileBasedLipSyncing ())
			{
				if (KickStarter.speechManager.autoNameSpeechFiles)
				{
					string filename = "Lipsync/";
					if (language != "" && KickStarter.speechManager.translateAudio)
					{
						// Not in original language
						filename += language + "/";
					}
					if (KickStarter.speechManager.placeAudioInSubfolders)
					{
						filename += speakerName + "/";
					}
					filename += speakerName + lineNumber;

					textFile = Resources.Load (filename) as TextAsset;

					if (textFile == null && KickStarter.speechManager.fallbackAudio && Options.GetLanguage () > 0)
					{
						filename = "Lipsync/";
						if (KickStarter.speechManager.placeAudioInSubfolders)
						{
							filename += "/" + speakerName + "/";
						}
						filename += speakerName + lineNumber;
						textFile = Resources.Load (filename) as TextAsset;
					}

					if (textFile == null)
					{
						ACDebug.LogWarning ("Lipsync file '/Resources/" + filename + ".txt' not found.");
					}
				}
				else
				{
					UnityEngine.Object _object = KickStarter.speechManager.GetLineCustomLipsyncFile (lineNumber, Options.GetLanguage ());

					if (_object == null && KickStarter.speechManager.fallbackAudio && Options.GetLanguage () > 0)
					{
						_object = KickStarter.speechManager.GetLineCustomLipsyncFile (lineNumber, 0);
					}

					if (_object is TextAsset)
					{
						textFile = (TextAsset) KickStarter.speechManager.GetLineCustomLipsyncFile (lineNumber, Options.GetLanguage ());
					}
				}
			}
			
			if (_lipSyncMode == LipSyncMode.ReadPamelaFile && textFile != null)
			{
				string[] pamLines = textFile.text.Split('\n');
				bool foundSpeech = false;
				float fps = 24f;
				foreach (string pamLine in pamLines)
				{
					if (!foundSpeech)
					{
						if (pamLine.Contains ("framespersecond:"))
						{
							string[] pamLineArray = pamLine.Split(':');
							float.TryParse (pamLineArray[1], out fps);
						}
						else if (pamLine.Contains ("[Speech]"))
						{
							foundSpeech = true;
						}
					}
					else if (pamLine.Contains (":"))
					{
						string[] pamLineArray = pamLine.Split(':');
						
						float timeIndex = 0f;
						float.TryParse (pamLineArray[0], out timeIndex);
						string searchText = pamLineArray[1].ToLower ().Substring (0, pamLineArray[1].Length-1);
						
						bool found = false;
						foreach (string phoneme in KickStarter.speechManager.phonemes)
						{
							string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
							if (!found)
							{
								foreach (string shape in shapesArray)
								{
									//if (shape == searchText)
									if (searchText.Contains (shape) && searchText.Length == shape.Length)
									{
										int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
										lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, fps));
										found = true;
									}
								}
							}
						}
						if (!found)
						{
							lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, fps));
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.ReadSapiFile && textFile != null)
			{
				string[] sapiLines = textFile.text.Split('\n');
				foreach (string sapiLine in sapiLines)
				{
					if (sapiLine.StartsWith ("phn "))
					{
						string[] sapiLineArray = sapiLine.Split(' ');
						
						float timeIndex = 0f;
						float.TryParse (sapiLineArray[1], out timeIndex);
						string searchText = sapiLineArray[4].ToLower ().Substring (0, sapiLineArray[4].Length-1);
						bool found = false;
						foreach (string phoneme in KickStarter.speechManager.phonemes)
						{
							string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
							if (!found)
							{
								foreach (string shape in shapesArray)
								{
									if (shape == searchText)
									{
										int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
										lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, 60f));
										found = true;
									}
								}
							}
						}
						if (!found)
						{
							lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, 60f));
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.ReadPapagayoFile && textFile != null)
			{
				string[] papagoyoLines = textFile.text.Split('\n');
				foreach (string papagoyoLine in papagoyoLines)
				{
					if (papagoyoLine != "" && !papagoyoLine.Contains ("MohoSwitch"))
					{
						string[] papagoyoLineArray = papagoyoLine.Split(' ');
						if (papagoyoLineArray.Length == 2)
						{
							float timeIndex = 0f;
							if (float.TryParse (papagoyoLineArray[0], out timeIndex))
							{
								string searchText = papagoyoLineArray[1].ToLower ().Substring (0, papagoyoLineArray[1].Length);
								
								bool found = false;
								if (!searchText.Contains ("rest") && !searchText.Contains ("etc"))
								{
									foreach (string phoneme in KickStarter.speechManager.phonemes)
									{
										string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
										if (!found)
										{
											foreach (string shape in shapesArray)
											{
												if (shape == searchText)
												{
													int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
													lipSyncShapes.Add (new LipSyncShape (frame, timeIndex, KickStarter.speechManager.lipSyncSpeed, 24f));
													found = true;
												}
											}
										}
									}
									if (!found)
									{
										lipSyncShapes.Add (new LipSyncShape (0, timeIndex, KickStarter.speechManager.lipSyncSpeed, 24f)); // was 240
									}
								}
							}
						}
					}
				}
			}
			else if (_lipSyncMode == LipSyncMode.FromSpeechText)
			{
				for (int i=0; i<_message.Length; i++)
				{
					int maxSearch = Mathf.Min (5, _message.Length - i);
					for (int n=maxSearch; n>0; n--)
					{
						string searchText = _message.Substring (i, n);
						searchText = searchText.ToLower ();
						
						foreach (string phoneme in KickStarter.speechManager.phonemes)
						{
							string[] shapesArray = phoneme.ToLower ().Split ("/"[0]);
							foreach (string shape in shapesArray)
							{
								if (shape == searchText)
								{
									int frame = KickStarter.speechManager.phonemes.IndexOf (phoneme);
									lipSyncShapes.Add (new LipSyncShape (frame, (float) i, KickStarter.speechManager.lipSyncSpeed));
									i += n;
									n = Mathf.Min (5, _message.Length - i);
									break;
								}
							}
						}
						
					}
					lipSyncShapes.Add (new LipSyncShape (0, (float) i, KickStarter.speechManager.lipSyncSpeed));
				}
			}
			
			if (lipSyncShapes.Count > 1)
			{
				lipSyncShapes.Sort (delegate (LipSyncShape a, LipSyncShape b) {return a.timeIndex.CompareTo (b.timeIndex);});
			}
			
			return lipSyncShapes;
		}


		private void EndBackgroundSpeechAudio (Speech speech)
		{
			foreach (Speech _speech in speechList)
			{
				if (_speech != speech)
				{
					_speech.EndBackgroundSpeechAudio (speech.GetSpeakingCharacter ());
				}
			}
		}
		
		
		private void EndSpeech (int i, bool stopCharacter = false)
		{
			Speech oldSpeech = speechList[i];
			KickStarter.playerMenus.RemoveSpeechFromMenu (oldSpeech);
			if (stopCharacter && oldSpeech.GetSpeakingCharacter ())
			{
				oldSpeech.GetSpeakingCharacter ().StopSpeaking ();
			}
			speechList.RemoveAt (i);
			
			if (oldSpeech.hasAudio)
			{
				KickStarter.stateHandler.UpdateAllMaxVolumes ();
			}

			// Call event
			KickStarter.eventManager.Call_OnStopSpeech (oldSpeech.GetSpeakingCharacter ());
		}


		private void OnDestroy ()
		{
			defaultAudioSource = null;
		}

	}
	

	/**
	 * A data struct for any pauses, delays or Expression changes within a speech line.
	 */
	public struct SpeechGap
	{

		/** The character index of the gap */
		public int characterIndex;
		/** The time delay of the gap */
		public float waitTime;
		/** If True, there is no time delay - the gap will pause indefinitely until the player clicks */
		public bool pauseIsIndefinite;
		/** The ID number of the Expression */
		public int expressionID;


		/**
		 * The default Constructor.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_waitTime</param>The time delay of the gap</param>
		 */
		public SpeechGap (int _characterIndex, float _waitTime)
		{
			characterIndex = _characterIndex;
			waitTime = _waitTime;
			expressionID = -1;
			pauseIsIndefinite = false;
		}


		/**
		 * A Constructor for an indefinite pauses.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_expressionID</param>The ID number of the Expression</param>
		 */
		public SpeechGap (int _characterIndex, bool _pauseIsIndefinite)
		{
			characterIndex = _characterIndex;
			waitTime = -1f;
			expressionID = -1;
			pauseIsIndefinite = _pauseIsIndefinite;
		}


		/**
		 * A Constructor for an expression change.</summary>
		 * <param name = "_characterIndex</param>The character index of the gap</param>
		 * <param name = "_expressionID</param>The ID number of the Expression</param>
		 */
		public SpeechGap (int _characterIndex, int _expressionID)
		{
			characterIndex = _characterIndex;
			waitTime = -1f;
			expressionID = _expressionID;
			pauseIsIndefinite = false;
		}
		
	}


	/**
	 * A data struct of lipsync animation
	 */
	public struct LipSyncShape
	{

		/** The animation frame to play */
		public int frame;
		/** The time index that the animation correlates to */
		public float timeIndex;
		

		/**
		 * The default Constructor.
		 * <param name = "_frame">The animation frame to play</param>
		 * <param name = "_timeIndex">The time index that the animation correlates to</param>
		 * <param name = "speed">The playback speed set by the player</param>
		 * <param name = "fps">The FPS rate set by the third-party LipSync tool</param>
		 */
		public LipSyncShape (int _frame, float _timeIndex, float speed, float fps = 1f)
		{
			// Pamela / Sapi
			frame = _frame;
			timeIndex = (_timeIndex / 15f / speed / fps) + Time.time;
		}

	}
	
}