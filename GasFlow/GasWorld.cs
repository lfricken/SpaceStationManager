using System.Collections.Generic;
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

		private bool _edgeIsBlocked;
		public bool EdgeIsBlocked
		{
			get { return _edgeIsBlocked; }
			set
			{
				_edgeIsBlocked = value;
				ReadBoard.SetEdgesBlocked(_edgeIsBlocked);
				WriteBoard.SetEdgesBlocked(_edgeIsBlocked);
			}
		}

		public Tile GetTile(int x, int y)
		{
			return ReadBoard.GetTile(x, y);
		}

		public Tile GetTile(Vector2Int pos)
		{
			return ReadBoard.GetTile(pos);
		}

		public void SetBlocked(Vector2Int pos, bool isBlocked)
		{
			ReadBoard.GetTile(pos).Blocked = isBlocked;
			WriteBoard.GetTile(pos).Blocked = isBlocked;
		}

		public IEnumerable<Tile> GetTiles()
		{
			return ReadBoard.GetTiles();
		}

		public void Update(int numTicks)
		{
			for (int i = 0; i < numTicks; ++i)
			{
				foreach (Vector2Int pos in WriteBoard.GetTilePositions())
				{
					Tile tile = WriteBoard.GetTile(pos);
					tile.Update(ReadBoard.GetTileSet(pos));
				}
				SwitchBoards();
			}

			if(EdgeIsBlocked)
			{
				WriteBoard.ClearEdgeTiles();
			}
		}

		private void SwitchBoards()
		{
			var temp = WriteBoard;
			WriteBoard = ReadBoard;
			ReadBoard = temp;
		}
	}
}
