#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage the export of script-sheets
	 */
	public class ScriptSheetWindow : EditorWindow
	{
		
		private int languageIndex = 0;
		
		private bool limitToCharacter = false;
		private string characterName = "";
		
		private bool limitToTag = false;
		private int tagID = 0;
		
		
		/**
		 * <summary>Initialises the window.</summary>
		 * <param name = "_languageIndex">The index number of the language to select by default.</param>
		 */
		public static void Init (int _languageIndex = 0)
		{
			ScriptSheetWindow window = (ScriptSheetWindow) EditorWindow.GetWindow (typeof (ScriptSheetWindow));
			UnityVersionHandler.SetWindowTitle (window, "Script sheet exporter");
			window.position = new Rect (300, 200, 350, 185);
			window.languageIndex = _languageIndex;
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}
			
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			
			EditorGUILayout.HelpBox ("Check the settings below and click 'Create' to save a new script sheet.", MessageType.Info);
			EditorGUILayout.Space ();
			
			if (speechManager.languages.Count > 1)
			{
				languageIndex = EditorGUILayout.Popup ("Language:", languageIndex, speechManager.languages.ToArray ());
			}
			else
			{
				languageIndex = 0;
			}
			
			limitToCharacter = EditorGUILayout.Toggle ("Limit to character?", limitToCharacter);
			if (limitToCharacter)
			{
				characterName = EditorGUILayout.TextField ("Character name:", characterName);
			}
			
			limitToTag = EditorGUILayout.Toggle ("Limit by tag?", limitToTag);
			if (limitToTag)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				int i = 0;
				int tagNumber = -1;
				
				if (speechManager.speechTags.Count > 0)
				{
					foreach (SpeechTag speechTag in speechManager.speechTags)
					{
						labelList.Add (speechTag.label);
						if (speechTag.ID == tagID)
						{
							tagNumber = i;
						}
						i++;
					}
					
					if (tagNumber == -1)
					{
						ACDebug.LogWarning ("Previously chosen speech tag no longer exists!");
						tagNumber = 0;
						tagID = 0;
					}
					
					tagNumber = EditorGUILayout.Popup ("Speech tag:", tagNumber, labelList.ToArray());
					tagID = speechManager.speechTags [tagNumber].ID;
				}
				else
				{
					EditorGUILayout.HelpBox ("No speech tags!", MessageType.Info);
				}
			}
			
			if (GUILayout.Button ("Create"))
			{
				CreateScript ();
			}
		}
		
		
		private void CreateScript ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().speechManager == null)
			{
				ACDebug.LogError ("Cannot create script sheet - no Speech Manager is assigned!");
				return;
			}
			
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			languageIndex = Mathf.Max (languageIndex, 0);
			
			string suggestedFilename = "Adventure Creator";
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName;
			}
			if (limitToCharacter && characterName != "")
			{
				suggestedFilename += " (" + characterName + ")";
			}
			if (limitToTag && tagID >= 0)
			{
				SpeechTag speechTag = speechManager.GetSpeechTag (tagID);
				if (speechTag != null && speechTag.label.Length > 0)
				{
					suggestedFilename += " (" + speechTag.label + ")";
				}
			}
			suggestedFilename += " - ";
			if (languageIndex > 0)
			{
				suggestedFilename += speechManager.languages[languageIndex] + " ";
			}
			suggestedFilename += "script.html";
			
			string fileName = EditorUtility.SaveFilePanel ("Save script file", "Assets", suggestedFilename, "html");
			if (fileName.Length == 0)
			{
				return;
			}
			
			string gameName = "Adventure Creator";
			if (AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.saveFileName.Length > 0)
			{
				gameName = AdvGame.GetReferences ().settingsManager.saveFileName;
				if (languageIndex > 0)
				{
					gameName += " (" + speechManager.languages[languageIndex] + ")";
				}
			}
			
			System.Text.StringBuilder script = new System.Text.StringBuilder ();
			script.Append ("<html>\n<head>\n");
			script.Append ("<meta http-equiv='Content-Type' content='text/html;charset=ISO-8859-1' charset='UTF-8'>\n");
			script.Append ("<title>" + gameName + "</title>\n");
			script.Append ("<style> body, table, div, p, dl { font: 400 14px/22px Roboto,sans-serif; } footer { text-align: center; padding-top: 20px; font-size: 12px;} footer a { color: blue; text-decoration: none} </style>\n</head>\n");
			script.Append ("<body>\n");
			
			script.Append ("<h1>" + gameName + " - script sheet");
			if (limitToCharacter && characterName != "")
			{
				script.Append (" (" + characterName + ")");
			}
			script.Append ("</h1>\n");
			script.Append ("<h2>Created: " + DateTime.UtcNow.ToString("HH:mm dd MMMM, yyyy") + "</h2>\n");
			
			// By scene
			foreach (string sceneFile in speechManager.sceneFiles)
			{
				bool foundLinesInScene = false;
				
				foreach (SpeechLine line in speechManager.lines)
				{
					int slashPoint = sceneFile.LastIndexOf ("/") + 1;
					string sceneName = sceneFile.Substring (slashPoint);
					
					if (line.textType == AC_TextType.Speech &&
					    (line.scene == sceneFile || sceneName == (line.scene + ".unity")) &&
					    (!limitToCharacter || characterName == "" || line.owner == characterName || (line.isPlayer && characterName == "Player")) &&
					    (!limitToTag || line.tagID == tagID))
					{
						if (!foundLinesInScene)
						{
							script.Append ("<hr/>\n<h3><b>Scene:</b> " + sceneName + "</h3>\n");
							foundLinesInScene = true;
						}
						
						script.Append (line.Print (languageIndex));
					}
				}
			}
			
			// No scene
			bool foundLinesInInventory = false;
			
			foreach (SpeechLine line in speechManager.lines)
			{
				if (line.scene == "" &&
				    line.textType == AC_TextType.Speech &&
				    (!limitToCharacter || characterName == "" || line.owner == characterName || (line.isPlayer && characterName == "Player")) &&
				    (!limitToTag || line.tagID == tagID))
				{
					if (!foundLinesInInventory)
					{
						script.Append ("<hr/>\n<h3>Scene-independent lines:</h3>\n");
						foundLinesInInventory = true;
					}
					
					script.Append (line.Print (languageIndex));
				}
			}
			
			script.Append ("<footer>Generated by <a href='http://adventurecreator.org' target=blank>Adventure Creator</a>, by Chris Burton</footer>\n");
			script.Append ("</body>\n</html>");
			
			Serializer.CreateSaveFile (fileName, script.ToString ());
			
			#endif
			
			this.Close ();
		}
		
		
	}
	
}

#endif