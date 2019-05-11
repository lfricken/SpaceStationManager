using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasFlow
{
	public class Board<TileType> where TileType : Tile, new()
	{
		public List<List<Tile>> Tiles { get; set; }

		public Vector2Int Size { get; protected set; }

		public Vector2Int TrueSize { get; set; }

		public Board(Vector2Int size)
		{
			Size = size;
			TrueSize = new Vector2Int(size.x + 2, size.y + 2);
			Debug.Assert(Size.x > 0);
			Debug.Assert(Size.y > 0);

			Tiles = new List<List<Tile>>();
			for (int x = 0; x < TrueSize.x; x++)
			{
				var column = new List<Tile>();
				for (int y = 0; y < TrueSize.y; y++)
				{
					column.Add(new TileType());
				}
				Tiles.Add(column);
			}
		}

		public void SetEdgesBlocked(bool edgeBlocked)
		{
			foreach(Tile tile in GetEdgeTiles())
			{
				tile.Blocked = edgeBlocked;
			}
		}

		public void ClearEdgeTiles()
		{
			foreach (Tile tile in GetEdgeTiles())
			{
				tile.TotalPressure = 0f;
			}
		}

		private IEnumerable<Tile> GetEdgeTiles()
		{
			int leftX = 0;
			int rightX = TrueSize.x - 1;
			for (int y = 0; y < TrueSize.y; ++y)
			{
				yield return GetTrueTile(leftX, y);
				yield return GetTrueTile(rightX, y);
			}

			int topY = TrueSize.y - 1;
			int bottomY = 0;
			for (int x = 1; x < (TrueSize.x - 1); ++x)
			{
				yield return GetTrueTile(x, topY);
				yield return GetTrueTile(x, bottomY);
			}
		}

		public List<Tile> GetTileSet(Vector2Int center)
		{
			int x = center.x + 1;
			int y = center.y + 1;

			List<Tile> list = new List<Tile>();

			list.Add(GetTrueTile(x - 1, y - 1));
			list.Add(GetTrueTile(x + 0, y - 1));
			list.Add(GetTrueTile(x + 1, y - 1));

			list.Add(GetTrueTile(x - 1, y + 0));
			list.Add(GetTrueTile(x + 0, y + 0));
			list.Add(GetTrueTile(x + 1, y + 0));

			list.Add(GetTrueTile(x - 1, y + 1));
			list.Add(GetTrueTile(x + 0, y + 1));
			list.Add(GetTrueTile(x + 1, y + 1));

			return list;
		}

		public Tile GetTrueTile(int x, int y)
		{
			return Tiles[x][y];
		}

		public Tile GetTile(int x, int y)
		{
#if DEBUG
			if (!((x >= 0) && (x <= Size.x - 1) && (y >= 0) && (y <= Size.y - 1)))
				throw new ArgumentException($"{nameof(Vector2Int)} ({x},{y}) indexing {nameof(Board<TileType>)} of size ({Size.x},{Size.y})");
#endif
			return Tiles[x + 1][y + 1];
		}

		public Tile GetTile(Vector2Int position)
		{
			return GetTile(position.x, position.y);
		}

		public IEnumerable<Tile> GetTiles()
		{
			foreach (Vector2Int pos in GetTilePositions())
			{
				yield return GetTile(pos);
			}
		}

		public IEnumerable<Vector2Int> GetTilePositions()
		{
			int realX = Size.x;
			int realY = Size.y;
			for (int x = 0; x < realX; ++x)
			{
				for (int y = 0; y < realY; ++y)
				{
					yield return new Vector2Int(x, y);
				}
			}
		}
	}
}
