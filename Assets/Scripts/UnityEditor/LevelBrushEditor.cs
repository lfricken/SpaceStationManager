using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


#if UNITY_EDITOR
[CustomEditor(typeof(LevelBrush))]
public class LevelBrushEditor : GridBrushEditor
{

}
#endif
