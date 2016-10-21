/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"AssetLoader.cs"
 * 
 *	This handles the management and retrieval of "Resources"
 *	assets when loading saved games.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * A class that handles the retrieves of Resources assets stored within save game files.
	 */
	public static class AssetLoader
	{

		private static Object[] textureAssets;
		private static Object[] audioAssets;
		private static Object[] animationAssets;
		private static Object[] materialAssets;
		private static Object[] actionListAssets;


		/**
		 * <summary>Gets a unique name for an asset file that can be used to find it later.</summary>
		 * <param name = "originalFile">The asset file</param>
		 * <returns>A unique identifier for the asset file</returns>
		 */
		public static string GetAssetInstanceID <T> (T originalFile) where T : Object
		{
			if (originalFile != null)
			{
				string name = originalFile.GetType () + originalFile.name;
				name = name.Replace (" (Instance)", "");
				return name;
			}
			return "";
		}


		/**
		 * <summary>Retrieves an asset file.</summary>
		 * <param name = "originalFile">The current asset used in the scene already</param>
		 * <param name = "_name">A unique identifier for the asset file</param>
		 * <returns>The asset file, or the current asset if it wasn't found</returns>
		 */
		public static T RetrieveAsset <T> (T originalFile, string _name) where T : Object
		{
			if (_name == "")
			{
				return originalFile;
			}

			if (originalFile == null)
			{
				return null;
			}

			Object[] assetFiles = null;

			if (originalFile is Texture2D)
			{
				if (textureAssets == null)
				{
					textureAssets = Resources.LoadAll ("", typeof (T));
				}
				assetFiles = textureAssets;
			}
			else if (originalFile is AudioClip)
			{
				if (audioAssets == null)
				{
					audioAssets = Resources.LoadAll ("", typeof (T));
				}
				assetFiles = audioAssets;
			}
			else if (originalFile is AnimationClip)
			{
				if (animationAssets == null)
				{
					animationAssets = Resources.LoadAll ("", typeof (T));
				}
				assetFiles = animationAssets;
			}
			else if (originalFile is Material)
			{
				if (materialAssets == null)
				{
					materialAssets = Resources.LoadAll ("", typeof (T));
				}
				assetFiles = materialAssets;
			}
			else if (originalFile is ActionListAsset)
			{
				if (actionListAssets == null)
				{
					actionListAssets = Resources.LoadAll ("", typeof (T));
				}
				assetFiles = actionListAssets;
			}

			if (assetFiles != null && _name != null)
			{
				_name = _name.Replace (" (Instance)", "");
				foreach (Object assetFile in assetFiles)
				{
					if (assetFile != null && (_name == (assetFile.GetType () + assetFile.name) || _name == assetFile.name))
					{
						return (T) assetFile;
					}
				}
			}
			
			return originalFile;
		}


		/**
		 * Clears the cache of stored assets from memory.
		 */
		public static void UnloadAssets ()
		{
			textureAssets = null;
			audioAssets = null;
			animationAssets = null;
			materialAssets = null;
			actionListAssets = null;
			Resources.UnloadUnusedAssets ();
		}

	}

}