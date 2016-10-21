/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"CustomAssetUtility.cs"
 * 
 *	This script allows assets to be created based on a supplied script.
 *	It is based on code written by Jacob Pennock (http://www.jacobpennock.com/Blog/?p=670)
 *  and code by Steven van Rossum (https://gist.github.com/JvetS/5208916)
 * 
 */

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

namespace AC
{

	/**
	 * A classs that assists with asset file creation.
	 */
	public static class CustomAssetUtility
	{
		
		private static string GetUniqueAssetPathNameOrFallback (string filename)
		{
			string path;
			try
			{
				System.Type assetdatabase = typeof (UnityEditor.AssetDatabase);
				path = (string) assetdatabase.GetMethod ("GetUniquePathNameAtSelectedPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(assetdatabase, new object[] { filename });
			}
			catch
			{
				path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath ("Assets/" + filename);
			}
			return path;
		}
		

		/**
		 * <summary>Creates a ScriptableObject asset file.</summary>
		 * <param name = "filename">The filename of the new asset</param>
		 * <param name = "path">Where to save the new asset</param>
		 * <returns>The created asset</returns>
		 */
		public static T CreateAsset<T> (string filename, string path = "") where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T> ();

			string assetPathAndName = "";
			if (path == "")
			{
				assetPathAndName = GetUniqueAssetPathNameOrFallback (filename + ".asset");
			}
			else
			{
				assetPathAndName = AssetDatabase.GenerateUniqueAssetPath ("Assets" + Path.DirectorySeparatorChar.ToString () + path + Path.DirectorySeparatorChar.ToString () + filename + ".asset");
			}
			AssetDatabase.CreateAsset (asset, assetPathAndName);
			
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();

			return asset;
		}
		

		/**
		 * <summary>Creates a ScriptableObject asset file.</summary>
		 * <param name = "path">Where to save the new asset</param>
		 * <returns>The created asset</returns>
		 */
		public static T CreateAndReturnAsset<T> (string path) where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T> ();
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath ("Assets" + Path.DirectorySeparatorChar.ToString () + path + Path.DirectorySeparatorChar.ToString () + typeof(T).ToString() + ".asset");
			AssetDatabase.CreateAsset (asset, assetPathAndName);
			
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			
			return asset;
		}

	}
}

#endif