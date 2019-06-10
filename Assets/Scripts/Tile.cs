using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SSM
{
	public class Tile : TileBase
	{
		public virtual Sprite PreviewSprite { get; protected set; }

		public LayerType LayerType;
		public TileType TileType;
		public GameObject Object;

		public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
		{
			tileData.sprite = Object.GetComponent<SpriteRenderer>().sprite;
			tileData.color = Color.red;
			tileData.flags = TileFlags.None;
			var m = tileData.transform;
			m.SetTRS(Vector3.zero, GetRotation(), Vector3.one);
			tileData.transform = m;
		}

		private Quaternion GetRotation()
		{
			return Quaternion.Euler(0f, 0f, 0f);
		}

		[MenuItem("Assets/Create/Tiles/Simple Tile", false, 0)]
		public static void CreateSimpleTile()
		{
			TileBuilder.CreateTile<Tile>(LayerType.None, "SimpleTile");
		}
	}
}
