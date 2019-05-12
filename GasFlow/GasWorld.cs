using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GasFlow
{
	public class GasWorld<TileType> where TileType : Tile, new()
	{
		BoardSet boards;

		public GasWorld(Vector2Int size)
		{
			boards.ReadBoard = new Board<TileType>(size);
			boards.WriteBoard = new Board<TileType>(size);
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

		public async Task Update(int numTicks)
		{
			int size = boards.WriteBoard.Size.x * boards.WriteBoard.Size.y;


			int increments = size / Environment.ProcessorCount;
			CountdownEvent threadCounter = new CountdownEvent(1);

			int counter = 0;
			List<Vector2Int> positions = new List<Vector2Int>();
			foreach (Vector2Int pos in boards.WriteBoard.GetTilePositions())
			{
				if(counter == increments)
				{
					Worker worker = new Worker(positions, boards, threadCounter);
					ThreadPool.QueueUserWorkItem(StartThread, worker);
					counter = 0;
					positions = new List<Vector2Int>();
				}
				++counter;
				positions.Add(pos);
			}

			if (EdgeIsBlocked)
			{
				boards.WriteBoard.ClearEdgeTiles();
			}

			boards.SwitchBoards();
			threadCounter.Signal();
			return threadCounter;
		}


		void StartThread(object data)
		{
			Worker w = (Worker)data;
			w.Work();
		}

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

		class Worker
		{
			public Worker(IEnumerable<Vector2Int> positions, BoardSet boards, CountdownEvent threadCounter)
			{
				Positions = positions;
				Boards = boards;
				ThreadCounter = threadCounter;

				ThreadCounter.AddCount(1);
			}

			IEnumerable<Vector2Int> Positions { get; set; }

			CountdownEvent ThreadCounter { get; set; }
			BoardSet Boards { get; set; }

			public void Work()
			{
				foreach (Vector2Int pos in Positions)
				{
					Tile tile = WriteBoard.GetTile(pos);
					tile.Update(ReadBoard.GetTileSet(pos));
				}
				ThreadCounter.Signal();
			}
		}
	}
}
