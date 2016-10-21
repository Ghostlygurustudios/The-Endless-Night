using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace AC
{

	public class AdventureCreator : EditorWindow
	{
		
		public References references;
		
		public static string version = "1.54a";
	 
		private bool showScene = true;
		private bool showSettings = false;
		private bool showActions = false;
		private bool showGVars = false;
		private bool showInvItems = false;
		private bool showSpeech = false;
		private bool showCursor = false;
		private bool showMenu = false;
		
		private Vector2 scroll;
		
		private static GUILayoutOption tabWidth = GUILayout.MinWidth (60f);


		[MenuItem ("Adventure Creator/Editors/Game Editor")]
		public static void Init ()
		{
			// Get existing open window or if none, make a new one:
			AdventureCreator window = (AdventureCreator) EditorWindow.GetWindow (typeof (AdventureCreator));
			window.GetReferences ();
			UnityVersionHandler.SetWindowTitle (window, "AC Game Editor");
		}
		
		
		private void GetReferences ()
		{
			references = (References) Resources.Load (Resource.references);
		}


		private void OnEnable ()
		{
			RefreshActions ();
		}
		
		
		private void OnInspectorUpdate ()
		{
			Repaint ();
		}
		
		
		private void OnGUI ()
		{
			if (!references)
			{
				GetReferences ();
			}
			
			if (references)
			{
				GUILayout.Space (10);
		
				GUILayout.BeginHorizontal ();
				
					if (GUILayout.Toggle (showScene, "Scene", "toolbarbutton", tabWidth))
					{
						SetTab (0);
					}
					if (GUILayout.Toggle (showSettings, "Settings", "toolbarbutton", tabWidth)) 
					{
						SetTab (1);
					}
					if (GUILayout.Toggle (showActions, "Actions", "toolbarbutton", tabWidth))
					{
						SetTab (2);
					}
					if (GUILayout.Toggle (showGVars, "Variables", "toolbarbutton", tabWidth))
					{
						SetTab (3);
					}
				
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				
					if (GUILayout.Toggle (showInvItems, "Inventory", "toolbarbutton", tabWidth))
					{
						SetTab (4);
					}
					if (GUILayout.Toggle (showSpeech, "Speech", "toolbarbutton", tabWidth))
					{
						SetTab (5);
					}
					if (GUILayout.Toggle (showCursor, "Cursor", "toolbarbutton", tabWidth))
					{
						SetTab (6);
					}
					if (GUILayout.Toggle (showMenu, "Menu", "toolbarbutton", tabWidth))
					{
						SetTab (7);
					}
		
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);
				
				scroll = GUILayout.BeginScrollView (scroll);

				if (showScene)
				{
					GUILayout.Label ("Scene manager",  CustomStyles.managerHeader);
					references.sceneManager = (SceneManager) EditorGUILayout.ObjectField ("Asset file: ", references.sceneManager, typeof (SceneManager), false);
					DrawManagerSpace ();

					if (!references.sceneManager)
					{
						AskToCreate <SceneManager> ("SceneManager");
					}
					else
					{
						references.sceneManager.ShowGUI ();
					}
				}
				
				else if (showSettings)
				{
					GUILayout.Label ("Settings manager",  CustomStyles.managerHeader);
					references.settingsManager = (SettingsManager) EditorGUILayout.ObjectField ("Asset file: ", references.settingsManager, typeof (SettingsManager), false);
					DrawManagerSpace ();

					if (!references.settingsManager)
					{
						AskToCreate <SettingsManager> ("SettingsManager");
					}
					else
					{
						references.settingsManager.ShowGUI ();
					}
				}
				
				else if (showActions)
				{
					GUILayout.Label ("Actions manager",  CustomStyles.managerHeader);
					references.actionsManager = (ActionsManager) EditorGUILayout.ObjectField ("Asset file: ", references.actionsManager, typeof (ActionsManager), false);
					DrawManagerSpace ();

					if (!references.actionsManager)
					{
						AskToCreate <ActionsManager> ("ActionsManager");
					}
					else
					{
						references.actionsManager.ShowGUI ();
					}
				}
				
				else if (showGVars)
				{
					GUILayout.Label ("Variables manager",  CustomStyles.managerHeader);
					references.variablesManager = (VariablesManager) EditorGUILayout.ObjectField ("Asset file: ", references.variablesManager, typeof (VariablesManager), false);
					DrawManagerSpace ();
					
					if (!references.variablesManager)
					{
						AskToCreate <VariablesManager> ("VariablesManager");
					}
					else
					{
						references.variablesManager.ShowGUI ();
					}
				}
				
				else if (showInvItems)
				{
					GUILayout.Label ("Inventory manager",  CustomStyles.managerHeader);
					references.inventoryManager = (InventoryManager) EditorGUILayout.ObjectField ("Asset file: ", references.inventoryManager, typeof (InventoryManager), false);
					DrawManagerSpace ();

					if (!references.inventoryManager)
					{
						AskToCreate <InventoryManager> ("InventoryManager");
					}
					else
					{
						references.inventoryManager.ShowGUI ();
					}
				}
				
				else if (showSpeech)
				{
					GUILayout.Label ("Speech manager",  CustomStyles.managerHeader);
					references.speechManager = (SpeechManager) EditorGUILayout.ObjectField ("Asset file: ", references.speechManager, typeof (SpeechManager), false);
					DrawManagerSpace ();

					if (!references.speechManager)
					{
						AskToCreate <SpeechManager> ("SpeechManager");
					}
					else
					{
						references.speechManager.ShowGUI ();
					}
				}
				
				else if (showCursor)
				{
					GUILayout.Label ("Cursor manager",  CustomStyles.managerHeader);
					references.cursorManager = (CursorManager) EditorGUILayout.ObjectField ("Asset file: ", references.cursorManager, typeof (CursorManager), false);
					DrawManagerSpace ();

					if (!references.cursorManager)
					{
						AskToCreate <CursorManager> ("CursorManager");
					}
					else
					{
						references.cursorManager.ShowGUI ();
					}
				}
				
				else if (showMenu)
				{
					GUILayout.Label ("Menu manager",  CustomStyles.managerHeader);
					references.menuManager = (MenuManager) EditorGUILayout.ObjectField ("Asset file: ", references.menuManager, typeof (MenuManager), false);
					DrawManagerSpace ();

					if (!references.menuManager)
					{
						AskToCreate <MenuManager> ("MenuManager");
					}
					else
					{
						references.menuManager.ShowGUI ();
					}
				}

				references.viewingMenuManager = showMenu;

				EditorGUILayout.Separator ();
				GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height(1));
				GUILayout.Label ("Adventure Creator - Version " + AdventureCreator.version, EditorStyles.miniLabel);

				GUILayout.EndScrollView ();
			}
			else
			{
				EditorStyles.label.wordWrap = true;
				GUILayout.Label ("No 'References' asset found in the resources folder. Please click to create one.", EditorStyles.label);
				
				if (GUILayout.Button ("Create 'References' file"))
				{
					references = CustomAssetUtility.CreateAsset<References> ("References", "AdventureCreator" + Path.DirectorySeparatorChar.ToString () + "Resources");
				}
			}
			
			if (GUI.changed)
			{
				if (showActions)
				{
					RefreshActions ();
				}

				EditorUtility.SetDirty (this);
				EditorUtility.SetDirty (references);
			}
		}


		private void DrawManagerSpace ()
		{
			EditorGUILayout.Space ();
			EditorGUILayout.Separator ();
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			EditorGUILayout.Space ();
		}
		
		
		private void SetTab (int tab)
		{
			showScene = false;
			showSettings = false;
			showActions = false;
			showGVars = false;
			showInvItems = false;
			showSpeech = false;
			showCursor = false;
			showMenu = false;
			
			if (tab == 0)
			{
				showScene = true;
			}
			else if (tab == 1)
			{
				showSettings = true;
			}
			else if (tab == 2)
			{
				showActions = true;
			}
			else if (tab == 3)
			{
				showGVars = true;
			}
			else if (tab == 4)
			{
				showInvItems = true;
			}
			else if (tab == 5)
			{
				showSpeech = true;
			}
			else if (tab == 6)
			{
				showCursor = true;
			}
			else if (tab == 7)
			{
				showMenu = true;
			}
		}
		
		
		private void AskToCreate<T> (string obName) where T : ScriptableObject
		{
			EditorStyles.label.wordWrap = true;
			GUILayout.Label ("A '" + obName + "' asset is required for the game to run correctly.", EditorStyles.label);
			
			if (GUILayout.Button ("Create new " + obName + " file"))
			{
				try {
					ScriptableObject t = CustomAssetUtility.CreateAsset<T> (obName, Resource.managersDirectory);
					
					Undo.RecordObject (references, "Assign " + obName);
					
					if (t is SceneManager)
					{
						references.sceneManager = (SceneManager) t;
					}
					else if (t is SettingsManager)
					{
						references.settingsManager = (SettingsManager) t;
					}
					else if (t is ActionsManager)
					{
						references.actionsManager = (ActionsManager) t;
						RefreshActions ();
					}
					else if (t is VariablesManager)
					{
						references.variablesManager = (VariablesManager) t;
					}
					else if (t is InventoryManager)
					{
						references.inventoryManager = (InventoryManager) t;
					}
					else if (t is SpeechManager)
					{
						references.speechManager = (SpeechManager) t;
					}
					else if (t is CursorManager)
					{
						references.cursorManager = (CursorManager) t;
					}
					else if (t is MenuManager)
					{
						references.menuManager = (MenuManager) t;
					}
				}
				catch
				{
					ACDebug.LogWarning ("Could not create " + obName + ". Does the subdirectory " + Resource.managersDirectory + " exist?");
				}
			}
		}


		public static void RefreshActions ()
		{
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().actionsManager == null)
			{
				return;
			}

			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;

			// Collect data to transfer
			List<ActionType> oldActionTypes = new List<ActionType>();
			foreach (ActionType actionType in actionsManager.AllActions)
			{
				oldActionTypes.Add (actionType);
			}

			// Load default Actions
			DirectoryInfo dir = new DirectoryInfo ("Assets/" + actionsManager.folderPath);
			FileInfo[] info = dir.GetFiles ("*.cs");
			
			actionsManager.AllActions.Clear ();
			foreach (FileInfo f in info) 
			{
				try
				{
					int extentionPosition = f.Name.IndexOf (".cs");
					string className = f.Name.Substring (0, extentionPosition);

					StreamReader streamReader = new StreamReader (f.FullName);
					string fileContents = streamReader.ReadToEnd ();
					streamReader.Close ();
					
					fileContents = fileContents.Replace (" ", "");
					
					if (fileContents.Contains ("class" + className + ":Action") ||
					    fileContents.Contains ("class" + className + ":AC.Action"))
					{
						Action tempAction = (Action) CreateInstance (className);
						if (tempAction is Action)
						{
							ActionType newActionType = new ActionType (className, tempAction);
							
							// Transfer back data
							foreach (ActionType oldActionType in oldActionTypes)
							{
								if (newActionType.IsMatch (oldActionType))
								{
									newActionType.color = oldActionType.color;
									if (newActionType.color == new Color (0f, 0f, 0f, 0f)) newActionType.color = Color.white;
									if (newActionType.color.a < 1f) newActionType.color = new Color (newActionType.color.r, newActionType.color.g, newActionType.color.b, 1f);
								}
							}
							
							actionsManager.AllActions.Add (newActionType);
						}
					}
					else
					{
						ACDebug.LogError ("The script '" + f.FullName + "' must derive from AC's Action class in order to be available as an Action.");
					}
				}
				catch {}
			}

			// Load custom Actions
			if (actionsManager.customFolderPath != actionsManager.folderPath)
			{
				dir = new DirectoryInfo ("Assets/" + actionsManager.customFolderPath);
				info = dir.GetFiles ("*.cs");
				
				foreach (FileInfo f in info) 
				{
					try
					{
						int extentionPosition = f.Name.IndexOf (".cs");
						string className = f.Name.Substring (0, extentionPosition);

						StreamReader streamReader = new StreamReader (f.FullName);
						string fileContents = streamReader.ReadToEnd ();
						streamReader.Close ();

						fileContents = fileContents.Replace (" ", "");

						if (fileContents.Contains ("class" + className + ":Action") ||
						    fileContents.Contains ("class" + className + ":AC.Action"))
						{
							Action tempAction = (Action) CreateInstance (className);
							if (tempAction is Action)
							{
								actionsManager.AllActions.Add (new ActionType (className, tempAction));
							}
						}
						else
						{
							ACDebug.LogError ("The script '" + f.FullName + "' must derive from AC's Action class in order to be available as an Action.");
						}
					}
					catch {}
				}
			}
			
			actionsManager.AllActions.Sort (delegate(ActionType i1, ActionType i2) { return i1.GetFullTitle (true).CompareTo(i2.GetFullTitle (true)); });
			actionsManager.SetEnabled ();
		}
		
	}

}