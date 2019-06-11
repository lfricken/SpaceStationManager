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
			tileData.color = Color.white;
			tileData.flags = TileFlags.None;
			tileData.transform.SetTRS(Vector3.zero, GetRotation(), Vector3.one);
		}

		private Quaternion GetRotation()
		{
			return Quaternion.Euler(0f, 0f, 0f);
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
