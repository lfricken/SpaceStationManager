using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;

namespace GasFlow.UnitTests
{
	[TestClass]
	public class GasWorldTest
	{
		[TestMethod]
		public void GasDistrubutesOverNineTiles()
		{
			GasWorld<Tile> world = new GasWorld<Tile>(new Vector2Int(3, 3));
			Tile center;

			center = world.GetTile(new Vector2Int(1, 1));
			center.TotalPressure = 36f;
			world.Update(1);

			center = world.GetTile(new Vector2Int(1, 1));
			Assert.AreEqual(36f / 9f, center.TotalPressure);
		}

		[TestMethod]
		public void BlockingWallDoesNotBlockCenter()
		{
			GasWorld<Tile> world = new GasWorld<Tile>(new Vector2Int(1, 1));
			world.EdgeIsBlocked = true;

			foreach (Tile tile in world.GetTiles())
			{
				Assert.IsFalse(tile.Blocked);
			}
		}

		[TestMethod]
		public void BlockingWallRetainsPressure()
		{
			GasWorld<Tile> world = new GasWorld<Tile>(new Vector2Int(1, 1));
			world.EdgeIsBlocked = true;
			Tile center;

			center = world.GetTile(new Vector2Int(0, 0));
			center.TotalPressure = 90f;
			world.Update(1);

			center = world.GetTile(new Vector2Int(0, 0));
			Assert.AreEqual(90f, center.TotalPressure);
		}
	}
}
