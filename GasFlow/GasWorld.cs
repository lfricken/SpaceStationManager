using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GasFlow
{
	public class GasWorld<TileType> where TileType : Tile, new()
	{
		class BoardSet
		{
			public Board<TileType> ReadBoard { get; set; }
			public Board<TileType> WriteBoard { get; set; }

			public void SwitchBoards()
			{
				var temp = WriteBoard;
				WriteBoard = ReadBoard;
				ReadBoard = temp;
			}
		}

		BoardSet boards;
		int maxThreads;

		public GasWorld(Vector2Int size)
		{
			maxThreads = Environment.ProcessorCount * 8;
			boards = new BoardSet();
			boards.ReadBoard = new Board<TileType>(size);
			boards.WriteBoard = new Board<TileType>(size);
		}

		public float MomentumLoss { get; set; }
		public float MomentumDiffusion { get; set; }
		public float MomentumBounceLoss { get; set; }
		public float MomentumBounceDiffusion { get; set; }

		#region Modify
		private bool _edgeIsBlocked;
		public bool EdgeIsBlocked
		{
			get { return _edgeIsBlocked; }
			set
			{
				_edgeIsBlocked = value;
				boards.ReadBoard.SetEdgesBlocked(_edgeIsBlocked);
				boards.WriteBoard.SetEdgesBlocked(_edgeIsBlocked);
			}
		}

		public Tile GetTile(int x, int y)
		{
			return boards.ReadBoard.GetTile(x, y);
		}

		public Tile GetTile(Vector2Int pos)
		{
			return boards.ReadBoard.GetTile(pos);
		}

		public void SetBlocked(Vector2Int pos, bool isBlocked)
		{
			boards.ReadBoard.GetTile(pos).Blocked = isBlocked;
			boards.WriteBoard.GetTile(pos).Blocked = isBlocked;
		}

		public IEnumerable<Tile> GetTiles()
		{
			return boards.ReadBoard.GetTiles();
		}
		#endregion

		#region Multithreading
		public async Task Update(int numTicks)
		{
			var tasks = StartThreads();
			await Task.WhenAll(tasks);
			if (EdgeIsBlocked)
			{
				boards.WriteBoard.ClearEdgeTiles();
			}

			boards.SwitchBoards();
		}

		public IList<Task> StartThreads()
		{
			IList<Task> tasks = new List<Task>();
			int size = boards.WriteBoard.Size.x * boards.WriteBoard.Size.y;
			int workPerThread = size / maxThreads;

			List<Vector2Int> positions = new List<Vector2Int>();
			foreach (Vector2Int pos in boards.WriteBoard.GetTilePositions())
			{
				if (positions.Count == workPerThread)
				{
					tasks.Add(StartThread(positions));
					positions = new List<Vector2Int>();
				}
				positions.Add(pos);
			}
			if (positions.Count > 0)
			{
				tasks.Add(StartThread(positions));
			}

			return tasks;
		}

		Task StartThread(IList<Vector2Int> positions)
		{
			Worker worker = new Worker(positions, boards);
			Task t = new Task(() => worker.Work());
			t.Start();
			return t;
		}

		class Worker
		{
			public Worker(IEnumerable<Vector2Int> positions, BoardSet boards)
			{
				Positions = positions;
				Boards = boards;
			}

			IEnumerable<Vector2Int> Positions { get; set; }

			CountdownEvent ThreadCounter { get; set; }
			BoardSet Boards { get; set; }

			public void Work()
			{
				foreach (Vector2Int pos in Positions)
				{
					Tile tile = Boards.WriteBoard.GetTile(pos);
					tile.Update(Boards.ReadBoard.GetTileSet(pos));
				}
			}
		}
		#endregion
	}
}
