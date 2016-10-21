/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"Resource.cs"
 * 
 *	This script contains variables for Resource prefabs.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class Resource
	{

		// Main Reference resource
		public const string references = "References";
		public const string managersDirectory = "AdventureCreator/Managers";
		
		// Created by Kickstarter on Awake
		public const string persistentEngine = "PersistentEngine";

		// Created by AdvGame when an ActionList asset is run
		public const string runtimeActionList = "RuntimeActionList";

		// Created by StateHandler on Awake
		public const string musicEngine = "MusicEngine";

		// External links
		public const string manualLink = "http://www.adventurecreator.org/files/Manual.pdf";
		public const string assetLink = "https://www.assetstore.unity3d.com/en/#!/content/11896";
		public const string websiteLink = "http://adventurecreator.org/";
		public const string tutorialsLink = "http://www.adventurecreator.org/tutorials/";
		public const string downloadsLink = "http://www.adventurecreator.org/downloads/";
		public const string forumLink = "http://www.adventurecreator.org/forum/";
		public const string scriptingGuideLink = "http://www.adventurecreator.org/scripting-guide/";


		#if UNITY_EDITOR

		private static Texture2D acLogo;
		private static Texture2D cogIcon;
		private static GUISkin nodeSkin;


		public static Texture2D ACLogo
		{
			get
			{
				if (acLogo == null)
				{
					acLogo = (Texture2D) AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Graphics/Textures/logo.png", typeof (Texture2D));
					if (acLogo == null)
					{
						ACDebug.LogWarning ("Cannot find Texture asset file 'Assets/AdventureCreator/Graphics/Textures/logo.png'");
					}
				}
				return acLogo;
			}
		}


		public static Texture2D CogIcon
		{
			get
			{
				if (cogIcon == null)
				{
					cogIcon = (Texture2D) AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Graphics/Textures/inspector-use.png", typeof (Texture2D));
					if (cogIcon == null)
					{
						ACDebug.LogWarning ("Cannot find Texture asset file 'Assets/AdventureCreator/Graphics/Textures/inspector-use.png'");
					}
				}
				return cogIcon;
			}
		}


		public static GUISkin NodeSkin
		{
			get
			{
				if (nodeSkin == null)
				{
					nodeSkin = (GUISkin) AssetDatabase.LoadAssetAtPath ("Assets/AdventureCreator/Graphics/Skins/ACNodeSkin.guiskin", typeof (GUISkin));
					if (nodeSkin == null)
					{
						ACDebug.LogWarning ("Cannot find GUISkin asset file 'Assets/AdventureCreator/Graphics/Skins/ACNodeSkin.guiskin'");
					}
				}
				return nodeSkin;
			}
		}

		#endif

	}

}