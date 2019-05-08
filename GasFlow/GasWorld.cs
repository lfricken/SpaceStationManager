using UnityEngine;

namespace GasFlow
{
	public class GasWorld<TileType> where TileType : Tile, new()
	{
		Board<TileType> ReadBoard;
		Board<TileType> WriteBoard;

		public GasWorld(Vector2Int size)
		{
			ReadBoard = new Board<TileType>(size);
			WriteBoard = new Board<TileType>(size);
		}

		public float MomentumLoss { get; set; }
		public float MomentumDiffusion { get; set; }
		public float MomentumBounceLoss { get; set; }
		public float MomentumBounceDiffusion { get; set; }

		public bool EdgeIsBlocked { get; set; }

		public void Update(int numTicks)
		{
			foreach(Vector2Int pos in WriteBoard.GetTilePositions())
			{
				Tile tile = WriteBoard.GetTile(pos);
				tile.Update(ReadBoard.GetTileSet(pos));
			}
		}

	}
}
