using System;
using Game;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Game.PhysicsShader;


[TestClass]
public class Heat
{
	static int steps = 10000;

	#region Equilibrium

	[TestMethod]
	public void Equilibrium_ZeroQ_EqualBodies()
	{
		HotMass a = new HotMass(100, 1, 1, 1);
		HotMass b = new HotMass(100, 1, 1, 1);

		Assert.AreEqual(0f, finalDeltaQ(a, b));
	}

	[TestMethod]
	public void Equilibrium_ApproachesZero_EnergyDelta()
	{
		HotMass a = new HotMass(100, 1, 1, 1);
		HotMass b = new HotMass(200, 1, 1, 1);

		float lastDeltaQ = float.MaxValue;
		for (int i = 0; i < steps; ++i)
		{
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ <= lastDeltaQ, $"EnergyDelta increased! step:{i} {nameof(lastDeltaQ)}: {lastDeltaQ}, {nameof(dQ)}: {dQ}");
			lastDeltaQ = dQ;
			a.Energy += dQ;
			b.Energy -= dQ;
		}
	}

	[TestMethod]
	public void Equilibrium_ApproachesZero_EnergyDeltaLarge()
	{
		HotMass a = new HotMass(1, 1, 1, 1);
		HotMass b = new HotMass(10000f * 10000f * 10000f, 10000, 10000, 10000);

		float lastDeltaQ = float.MaxValue;
		for (int i = 0; i < steps; ++i)
		{
			float dQ = finalDeltaQ(a, b);
			Assert.IsTrue(dQ <= lastDeltaQ, $"EnergyDelta increased! step:{i} {nameof(lastDeltaQ)}: {lastDeltaQ}, {nameof(dQ)}: {dQ}");
			lastDeltaQ = dQ;
			a.Energy += dQ;
			b.Energy -= dQ;
		}
	}

	[TestMethod]
	public void Equilibrium_EntropyNotViolated_Temperature()
	{
		HotMass a = new HotMass(100, 1, 1, 1);
		HotMass b = new HotMass(200, 1, 1, 1);

		for (int i = 0; i < steps; ++i)
		{
			float dQ = finalDeltaQ(a, b);
			a.Energy += dQ;
			b.Energy -= dQ;
			Assert.IsTrue(getTemp(a) < getTemp(b), $"step:{ i}");
		}
	}

	[TestMethod]
	public void Equilibrium_EntropyNotViolated_TemperatureLarge()
	{
		HotMass a = new HotMass(1, 1, 1, 1);
		HotMass b = new HotMass(0, 10000, 10000, 10000);
		float bTemp = 10000;
		b.Energy = bTemp * b.HeatCapacity * b.Mass;

		for (int i = 0; i < steps; ++i)
		{
			float dQ = finalDeltaQ(a, b);
			a.Energy += dQ;
			b.Energy -= dQ;
			Assert.IsTrue(getTemp(a) < getTemp(b), $"step:{ i}");
		}
	}

	[TestMethod]
	public void Equilibrium_MultiTest_FiveCell()
	{
		HotMass[] cells = new HotMass[4];
		cells[0] = new HotMass(0, 1, 1, 1);
		cells[1] = new HotMass(0, 1, 1, 1);
		cells[2] = new HotMass(0, 1, 1, 1);
		cells[3] = new HotMass(0, 1, 1, 1);

		HotMass centerCell = new HotMass(100, 1, 1, 1);

		float dQCenter = 0;

		for (int i = 0; i < steps; i++)
		{
			for (int c = 0; c < cells.Length; c++)
			{
				float dQ = finalDeltaQ(cells[c], centerCell);
				cells[c].Energy += dQ;
				dQCenter -= dQ; // 
			}
			centerCell.Energy += dQCenter; // add because we use negative above
			dQCenter = 0;

			for (int c = 0; c < cells.Length; c++)
			{
				float tempC = getTemp(cells[c]);
				float tempCenterCell = getTemp(centerCell);

				Assert.IsTrue((int)tempC <= (int)tempCenterCell, $"Temperatures: step:{i} {tempC}, {tempCenterCell}"); // temperature
			}
		}
	}

	[TestMethod]
	public void Equilibrium_MultiTestLargeConduct_FiveCell()
	{
		HotMass[] cells = new HotMass[4];
		cells[0] = new HotMass(0, 1, 1, 1);
		cells[1] = new HotMass(0, 1, 100, 1);
		cells[2] = new HotMass(0, 1, 1, 1);
		cells[3] = new HotMass(0, 1, 1, 1);

		HotMass centerCell = new HotMass(100, 1, 100, 1);

		float dQCenter = 0;

		for (int i = 0; i < steps; i++)
		{
			for (int c = 0; c < cells.Length; c++)
			{
				float dQ = finalDeltaQ(cells[c], centerCell);
				cells[c].Energy += dQ;
				dQCenter -= dQ; // 
			}
			centerCell.Energy += dQCenter; // add because we use negative above
			dQCenter = 0;

			for (int c = 0; c < cells.Length; c++)
			{
				float tempC = getTemp(cells[c]);
				float tempCenterCell = getTemp(centerCell);

				Assert.IsTrue((int)tempC <= (int)tempCenterCell, $"Temperatures: step:{i} {tempC}, {tempCenterCell}"); // temperature
			}
		}
	}

	[TestMethod]
	public void Equilibrium_MultiTestLargeHeatCap_FiveCell()
	{
		HotMass[] cells = new HotMass[4];
		cells[0] = new HotMass(0, 100, 1, 1);
		cells[1] = new HotMass(0, 1, 1, 1);
		cells[2] = new HotMass(0, 1, 1, 1);
		cells[3] = new HotMass(0, 1, 1, 1);

		HotMass centerCell = new HotMass(100000, 1000, 1, 1);

		float dQCenter = 0;

		for (int i = 0; i < steps; i++)
		{
			for (int c = 0; c < cells.Length; c++)
			{
				float dQ = finalDeltaQ(cells[c], centerCell);
				cells[c].Energy += dQ;
				dQCenter -= dQ; // 
			}
			centerCell.Energy += dQCenter; // add because we use negative above
			dQCenter = 0;

			float tempC = getTemp(cells[0]);
			float tempCenterCell = getTemp(centerCell);

			Assert.IsTrue(Math.Round(tempC) <= Math.Round(tempCenterCell), $"Temperatures: step:{i} {tempC}, {tempCenterCell}"); // temperature
		}
	}

	[TestMethod]
	public void Equilibrium_MultiTestLargeMass_FiveCell()
	{
		HotMass[] cells = new HotMass[4];
		cells[0] = new HotMass(0, 1, 1, 100);
		cells[1] = new HotMass(0, 1, 1, 1);
		cells[2] = new HotMass(0, 1, 1, 1);
		cells[3] = new HotMass(0, 1, 1, 1);

		HotMass centerCell = new HotMass(100000, 1, 1, 100);

		float dQCenter = 0;

		for (int i = 0; i < steps; i++)
		{
			for (int c = 0; c < cells.Length; c++)
			{
				float dQ = finalDeltaQ(cells[c], centerCell);
				cells[c].Energy += dQ;
				dQCenter -= dQ; // 
			}
			centerCell.Energy += dQCenter; // add because we use negative above
			dQCenter = 0;

			float tempC = getTemp(cells[0]);
			float tempCenterCell = getTemp(centerCell);

			Assert.IsTrue(Math.Round(tempC) <= Math.Round(tempCenterCell), $"Temperatures: step:{i} {tempC}, {tempCenterCell}"); // temperature
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
		Assert.IsTrue(dQ > 0);
	}
	[TestMethod]
	public void Energy_LeftHotter_FlowRight()
	{
		HotMass a = new HotMass(2, 1, 1, 1);
		HotMass b = new HotMass(1, 1, 1, 1);
		float dQ = finalDeltaQ(a, b);
		Assert.IsTrue(dQ < 0);
	}
	[TestMethod]
	public void Energy_ZeroEnergy_LeftCell_FlowLeft()
	{
		HotMass a = new HotMass(0, 1, 1, 1);
		HotMass b = new HotMass(2, 1, 1, 1);
		float dQ = finalDeltaQ(a, b);
		Assert.IsTrue(dQ > 0);
	}
	[TestMethod]
	public void Energy_ZeroEnergy_RightCell_FlowRight()
	{
		HotMass a = new HotMass(2, 1, 1, 1);
		HotMass b = new HotMass(0, 1, 1, 1);
		float dQ = finalDeltaQ(a, b);
		Assert.IsTrue(dQ < 0);
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
