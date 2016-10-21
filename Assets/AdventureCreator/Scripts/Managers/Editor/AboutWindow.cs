using UnityEngine;
using UnityEditor;

namespace AC
{

	public class AboutWindow : EditorWindow
	{

		private static AboutWindow window;

		[MenuItem ("Adventure Creator/About", false, 20)]
		static void Init ()
		{
			if (window != null)
			{
				return;
			}

			window = EditorWindow.GetWindowWithRect <AboutWindow> (new Rect (0, 0, 420, 360), true, "About AC", true);
			UnityVersionHandler.SetWindowTitle (window, "About AC");
		}


		private void OnGUI ()
		{
			GUILayout.BeginVertical ( CustomStyles.thinBox, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			
			GUILayout.BeginVertical ();
			GUILayout.Space (20f);

			if (Resource.ACLogo)
			{
				GUILayout.Label (Resource.ACLogo);
			}
			else
			{
				GUILayout.Label ("Adventure Creator",  CustomStyles.managerHeader);
			}

			GUILayout.Label ("By Chris Burton, ICEBOX Studios",  CustomStyles.managerHeader);

			GUILayout.Label ("<b>" + AdventureCreator.version + "</b>",  CustomStyles.smallCentre);
			GUILayout.Space (12f);

			if (GUILayout.Button ("Documentation"))
			{
				Application.OpenURL (Resource.manualLink);
			}

			if (GUILayout.Button ("Website"))
			{
				Application.OpenURL (Resource.websiteLink);
			}

			if (GUILayout.Button ("Asset Store page"))
			{
				Application.OpenURL (Resource.assetLink);
			}

			if (!ACInstaller.IsInstalled ())
			{
				if (GUILayout.Button ("Auto-configure Unity project settings"))
				{
					ACInstaller.DoInstall ();
				}
			}
			else
			{
				if (GUILayout.Button ("New Game Wizard"))
				{
					NewGameWizardWindow.Init ();
				}
			}

			GUILayout.EndVertical();
			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}

	}

}