using System.Collections.Generic;
using UnityEngine;

namespace GasFlow
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TileType"></typeparam>
	public class Board<TileType> where TileType : Tile, new()
	{
		/// <summary>
		/// 
		/// </summary>
		private List<List<Tile>> Tiles { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public Vector2Int Size { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		private Vector2Int TrueSize { get; set; }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		public Board(Vector2Int size)
		{
			Size = size;
			TrueSize = new Vector2Int(size.x + 2, size.y + 2);
			Debug.Assert(Size.x > 0);
			Debug.Assert(Size.y > 0);

			Tiles = new List<List<Tile>>();
			for (int x = 0; x < size.x; x++)
			{
				var column = new List<Tile>();
				for (int y = 0; y < size.y; y++)
				{
					column.Add(new TileType());
				}
				Tiles.Add(column);
			}
		}

		public List<Tile> GetTileSet(Vector2Int center)
		{
			int x = center.x;
			int y = center.y;

			List<Tile> list = new List<Tile>();

			list.Add(GetTile(x - 1, y - 1));
			list.Add(GetTile(x + 0, y - 1));
			list.Add(GetTile(x + 1, y - 1));

			list.Add(GetTile(x - 1, y + 0));
			list.Add(GetTile(x + 0, y + 0));
			list.Add(GetTile(x + 1, y + 0));

			list.Add(GetTile(x - 1, y + 1));
			list.Add(GetTile(x + 0, y + 1));
			list.Add(GetTile(x + 1, y + 1));

			return list;
		}

		public Tile GetTile(int x, int y)
		{
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
