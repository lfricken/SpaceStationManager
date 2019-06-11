using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
	/// <summary>
	/// 
	/// </summary>
	public class Tile : TileBase
	{
		public Sprite Sprite;
		public LayerType LayerType;
		public TileType TileType;
		public GameObject Object;

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			tileData.sprite = Sprite;
			tileData.color = Color.white;
			tileData.gameObject = Object;
			tileData.flags = TileFlags.None;
			tileData.transform = tilemap.GetTransformMatrix(position);
		}

#if UNITY_EDITOR
		[MenuItem("Assets/Create/Tiles/Tile", false, 0)]
		public static void MenuCreate()
		{
			MenuItemBuilder.CreateAsset<Tile>(nameof(Tile));
		}
#endif
	}
}
