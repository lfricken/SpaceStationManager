using Microsoft.VisualStudio.TestTools.UnitTesting;

using static Game.PhysicsShader;

[TestClass]
public class Gas
{
	static int steps = 10000;

	#region Heat

	[TestMethod]
	public void Heat_HotRight_HeatFlowLeft()
	{
		GasMass l = new GasMass(new float[] { 0, 0, });
		float lE = 0;

		GasMass r = new GasMass(new float[] { 1, 1, });
		float rE = 10;
		finalGasDelta(l, r, lE, rE, out float dQ);

		Assert.IsTrue(dQ > 0);
	}

	[TestMethod]
	public void Heat_HotLeft_HeatFlowRight()
	{
		GasMass l = new GasMass(new float[] { 1, 1, });
		float lE = 10;

		GasMass r = new GasMass(new float[] { 0, 0, });
		float rE = 0;
		finalGasDelta(l, r, lE, rE, out float dQ);

		Assert.IsTrue(dQ < 0);
	}

	[TestMethod]
	public void Heat_EqualTiles_HeatFlowZero()
	{
		GasMass l = new GasMass(new float[] { 1, 1, });
		float lE = 10;

		GasMass r = new GasMass(new float[] { 1, 1, });
		float rE = 10;
		finalGasDelta(l, r, lE, rE, out float dQ);

		Assert.IsTrue(dQ == 0);
	}

	#endregion

	#region Different Gasses
	[TestMethod]
	public void Gas_EqualPressure_DifferentGas_Mixes()
	{
		float totalMass = 2;

		GasMass l = new GasMass(new float[] { 1, 0, });
		float lE = 10;

		GasMass r = new GasMass(new float[] { 0, 1, });
		float rE = 10;
		GasTileDelta gDelta = finalGasDelta(l, r, lE, rE, out float dQ);

		l = sub(l, gDelta.fromLeft);
		l = add(l, gDelta.fromRight);

		r = sub(r, gDelta.fromRight);
		r = add(r, gDelta.fromLeft);

		Assert.IsTrue(l.Masses[1] > 0);
		Assert.IsTrue(r.Masses[0] > 0);
	}

	[TestMethod]
	public void Gas_EqualPressure_DifferentGas_ConservesGas()
	{
		float totalMass = 2;

		GasMass l = new GasMass(new float[] { 1, 0, });
		float lE = 10;

		GasMass r = new GasMass(new float[] { 0, 1, });
		float rE = 10;
		GasTileDelta gDelta = finalGasDelta(l, r, lE, rE, out float dQ);

		l = sub(l, gDelta.fromLeft);
		l = add(l, gDelta.fromRight);

		r = sub(r, gDelta.fromRight);
		r = add(r, gDelta.fromLeft);

		GasMass total = new GasMass(new float[] { 0, 0, });
		total = add(total, l);
		total = add(total, r);

		Assert.IsTrue(total.Masses[0] + total.Masses[1] == 2);
	}

	[TestMethod]
	public void Equilibrium_DifferentPressure_NeverIncreasesDelta()
	{
		float totalMass = 2;

		GasMass l = new GasMass(new float[] { 1, 0.5f, });
		float lE = 10;

		GasMass r = new GasMass(new float[] { 0, 0, });
		float rE = 10;




		for (int i = 0; i < steps; ++i)
		{
			GasTileDelta gDelta = finalGasDelta(l, r, lE, rE, out float dQ);

			l = sub(l, gDelta.fromLeft);
			l = add(l, gDelta.fromRight);

			r = sub(r, gDelta.fromRight);
			r = add(r, gDelta.fromLeft);

			Assert.IsTrue(l.Masses[0] >= r.Masses[0]);
			Assert.IsTrue(l.Masses[1] >= r.Masses[1]);
		}

		GasMass total = new GasMass(new float[] { 0, 0, });
		total = add(total, l);
		total = add(total, r);

	}

	#endregion
}
