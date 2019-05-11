using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine;

namespace GasFlow.UnitTests
{
	[TestClass]
	public class GasWorldTest
	{
		[TestMethod]
		public void Wall_Unblocked_GasSpreadsOut()
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
		public void Wall_Blocked_RetainsGas()
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

		[TestMethod]
		public void Edge_Blocked_DoesNotBlockCenter()
		{
			GasWorld<Tile> world = new GasWorld<Tile>(new Vector2Int(3, 3));
			world.EdgeIsBlocked = true;

			foreach (Tile tile in world.GetTiles())
			{
				Assert.IsFalse(tile.Blocked);
			}
		}

		[TestMethod]
		public void Edge_Blocked_BlockGas()
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

		[TestMethod]
		public void Edge_Unblocked_DeleteGas()
		{
			Tile tile;
			float pressure = 81f;
			GasWorld<Tile> world = new GasWorld<Tile>(new Vector2Int(1, 1));
			world.EdgeIsBlocked = false;

			tile = world.GetTile(new Vector2Int(0, 0));
			tile.TotalPressure = pressure;

			world.Update(1);
			tile = world.GetTile(new Vector2Int(0, 0));
			pressure /= 9f;
			Assert.AreEqual(pressure, tile.TotalPressure);

			world.Update(1);
			tile = world.GetTile(new Vector2Int(0, 0));
			pressure /= 9f;
			Assert.AreEqual(pressure, tile.TotalPressure);
		}
	}
}
