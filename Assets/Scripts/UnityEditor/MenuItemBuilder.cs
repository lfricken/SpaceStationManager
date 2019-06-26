using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game
{
#if UNITY_EDITOR
	public class MenuItemBuilder : MonoBehaviour
	{
		/// <summary>
		/// Creates the asset with the given name at the given path.
		/// Automatically increments the number if it already exists.
		/// </summary>
		public static void CreateAsset<T>(string name, string newPath = null) where T : ScriptableObject
		{
			string path = newPath ?? GetPath();
			string assetPath = Path.Combine(path, name + ".asset");

			int i = 1;
			while (File.Exists(assetPath))
				assetPath = Path.Combine(path, name + "_" + i++ + ".asset");

			string folder = Path.GetDirectoryName(assetPath);

			if (folder != null)
			{
				Directory.CreateDirectory(folder);
				AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), assetPath);
			}
		}

		private static string GetPath()
		{
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (string.IsNullOrEmpty(path))
			{
				path = "Assets";
			}
			else if (Path.GetExtension(path) != "")
			{
				path = path.Replace(Path.GetFileName(path), "");
			}

			return path;
		}
	}
#endif
}