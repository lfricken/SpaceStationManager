using System;
using Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Game.PhysicsShader;

namespace PhysicsTests
{
	[TestClass]
	public class FinalDeltaQTest
	{
		#region Equilibrium

		[TestMethod]
		public void Equilibrium_EqualBodies_ZeroQ()
		{
			HotMass a = new HotMass(100, 1, 1, 1);
			HotMass b = new HotMass(100, 1, 1, 1);

			Assert.AreEqual(0f, finalDeltaQ(a, b));
		}

		[TestMethod]
		public void Equilibrium_EnergyDelta_TwoTiles_NeverIncreases()
		{
			HotMass a = new HotMass(100, 1, 1, 1);
			HotMass b = new HotMass(200, 1, 1, 1);

			int numTests = 500;
			float lastDeltaQ = float.MaxValue;
			for (int i = 0; i < numTests; ++i)
			{
				float dQ = finalDeltaQ(a, b);
				Assert.IsTrue(dQ <= lastDeltaQ, $"EnergyDelta increased! {nameof(lastDeltaQ)}: {lastDeltaQ}, {nameof(dQ)}: {dQ}");
				lastDeltaQ = dQ;
				a.Energy += dQ;
				b.Energy -= dQ;
			}
		}
		#endregion

		#region Energy
		[TestMethod]
		public void Energy_RightHotter_FlowLeft()
		{
			HotMass a = new HotMass(1, 1, 1, 1);
			HotMass b = new HotMass(2, 1, 1, 1);
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ > 0, "Positive energy should imply energy is flowing right to left.");
		}
		[TestMethod]
		public void Energy_LeftHotter_FlowRight()
		{
			HotMass a = new HotMass(2, 1, 1, 1);
			HotMass b = new HotMass(1, 1, 1, 1);
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ < 0, "Negative energy should imply energy is flowing left to right.");
		}
		#endregion

		#region Heat Capacity
		[TestMethod]
		public void Capacity_LeftHigherCapacity_FlowLeft()
		{
			HotMass a = new HotMass(1, 2, 1, 1);
			HotMass b = new HotMass(1, 1, 1, 1);
			float dQ = finalDeltaQ(a, b);

			Assert.IsTrue(dQ > 0);
		}
		[TestMethod]
		public void Capacity_RightHigherCapacity_FlowRight()
		{
			HotMass a = new HotMass(1, 1, 1, 1);
			HotMass b = new HotMass(1, 2, 1, 1);
			float dQ = finalDeltaQ(a, b);

			Assert.IsTrue(dQ < 0);
		}
		[TestMethod]
		public void Capacity_RightNoCap_ZeroQ()
		{
			HotMass a = new HotMass(1, 1, 1, 1);
			HotMass b = new HotMass(1, 0, 1, 1);
			float dQ = finalDeltaQ(a, b);

			Assert.IsTrue(dQ == 0);
		}
		[TestMethod]
		public void Capacity_LeftNoCap_ZeroQ()
		{
			HotMass a = new HotMass(1, 0, 1, 1);
			HotMass b = new HotMass(1, 1, 1, 1);
			float dQ = finalDeltaQ(a, b);

			Assert.IsTrue(dQ == 0);
		}
		#endregion

		#region Mass
		[TestMethod]
		public void Mass_RightMoreMass_FlowRight()
		{
			HotMass a = new HotMass(1, 1, 1, 1);
			HotMass b = new HotMass(1, 1, 1, 2);
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ < 0, "");
		}
		[TestMethod]
		public void Mass_LeftMoreMass_FlowLeft()
		{
			HotMass a = new HotMass(1, 1, 1, 2);
			HotMass b = new HotMass(1, 1, 1, 1);
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ > 0, "");
		}

		[TestMethod]
		public void Mass_LeftCell_NoMass_ZeroQ()
		{
			HotMass a = new HotMass(2, 2, 2, 0);
			HotMass b = new HotMass(1, 1, 1, 1);

			Assert.AreEqual(0f, finalDeltaQ(a, b));
		}

		[TestMethod]
		public void Mass_RightCell_NoMass_ZeroQ()
		{
			HotMass a = new HotMass(2, 2, 2, 2);
			HotMass b = new HotMass(1, 1, 1, 0);

			Assert.AreEqual(0f, finalDeltaQ(a, b));
		}
		#endregion

		#region Conductivity
		[TestMethod]
		public void Conductivity_LeftCell_NoConduct_ZeroQ()
		{
			HotMass a = new HotMass(2, 2, 0, 2);
			HotMass b = new HotMass(1, 1, 1, 1);

			Assert.AreEqual(0f, finalDeltaQ(a, b));
		}

		[TestMethod]
		public void Conductivity_RightCell_NoConduct_ZeroQ()
		{
			HotMass a = new HotMass(2, 2, 2, 2);
			HotMass b = new HotMass(1, 1, 0, 1);

			Assert.AreEqual(0f, finalDeltaQ(a, b));
		}

		[TestMethod]
		public void Conductivity_HigherConductivity_MoreHeatExchange()
		{
			HotMass a;
			HotMass b;

			a = new HotMass(1, 1, 1, 1);
			b = new HotMass(2, 1, 1, 1);
			float lowConductDeltaQ = finalDeltaQ(a, b);

			a = new HotMass(1, 1, 1, 1);
			b = new HotMass(2, 1, 2, 1);
			float highConductDeltaQ = finalDeltaQ(a, b);

			Assert.IsTrue(highConductDeltaQ > lowConductDeltaQ, "");
		}
		#endregion
	}
}
