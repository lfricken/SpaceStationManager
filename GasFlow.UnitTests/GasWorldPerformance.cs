using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.Diagnostics;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace GasFlow.UnitTests
{
	[TestClass]
	public class GasWorldPerformance
	{
		[TestMethod]
		public void Performance_500x500()
		{

			int ticks = 1;
			Vector2Int size = new Vector2Int(500, 500);
			GasWorld<Tile> world = new GasWorld<Tile>(size);

			world.SetBlocked(new Vector2Int(size.x / 5, size.y / 5), true);

			Tile center = world.GetTile(new Vector2Int(size.x / 2, size.y / 2));
			center.TotalPressure = 900;

			Assert.AreEqual(0, Environment.ProcessorCount);

			Assert.That(Time(world, ticks), Is.LessThanOrEqualTo(TimeSpan.FromSeconds(0f)));
		}

		private TimeSpan Time(GasWorld<Tile> world, int ticks)
		{
			var timer = Stopwatch.StartNew();
			for (int i = 0; i < ticks; ++i)
			{
				world.Update(1);
			}
			timer.Stop();
			return timer.Elapsed;
		}
	}
}
