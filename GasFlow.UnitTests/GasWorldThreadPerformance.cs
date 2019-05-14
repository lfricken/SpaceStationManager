using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace GasFlow.UnitTests
{
	[TestClass]
	public class GasWorldThreadPerformance
	{
		Vector2Int WorldSize = new Vector2Int(500, 500); // 105 > 115 sees massive performance hit
		int NumWorlds = 1;

		private IList<GasWorld<Tile>> GenerateWorlds()
		{
			IList<GasWorld<Tile>> worlds = new List<GasWorld<Tile>>();
			for (int i = 0; i < NumWorlds; i++)
			{
				GasWorld<Tile> world = new GasWorld<Tile>(WorldSize);

				world.SetBlocked(new Vector2Int(WorldSize.x / 5, WorldSize.y / 5), true);

				Tile center = world.GetTile(new Vector2Int(WorldSize.x / 2, WorldSize.y / 2));
				center.TotalPressure = 900;
				worlds.Add(world);
			}
			return worlds;
		}

		[TestMethod]
		public void Threaded()
		{
			Assert.That(Time(GenerateWorlds(), true), Is.LessThanOrEqualTo(TimeSpan.FromSeconds(0f)));
		}

		[TestMethod]
		public void SingleThread()
		{
			Assert.That(Time(GenerateWorlds(), false), Is.LessThanOrEqualTo(TimeSpan.FromSeconds(0f)));
		}

		private TimeSpan Time(IList<GasWorld<Tile>> worlds, bool async)
		{
			var timer = Stopwatch.StartNew();

			if (async)
			{
				foreach (var world in worlds)
					world.UpdateAsync(1).Wait();
			}
			else
			{
				foreach (var world in worlds)
					world.Update(1);
			}

			timer.Stop();
			return timer.Elapsed;
		}
	}
}
