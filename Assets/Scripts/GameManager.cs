using Assets.Scripts;
//using GasFlow;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	GasFlowGpu gas;
	//GasWorld<Tile> gas2;

	void InitCanvas()
	{
		// this resolution must match that in gas.compute.index
		Vector3Int resolution = new Vector3Int(64, 64, 32);
		gas = new GasFlowGpu(resolution);
		//gas2 = new GasWorld<Tile>(new Vector2Int(resolution.x, resolution.y));

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = gas.RenderTexture;
	}

	int at(int i, int j)
	{
		return 1;
	}

	float clamp(float val, int dim)
	{
		if (val < 0.5)
			val = 0.5f;
		if (val > dim + 0.5)
			val = dim + 0.5f;
		return val;
	}

	void advect(int dim, float[] newPressure, float[] oldPressure, float[] velX, float[] velY, float dt)
	{
		int x, y;
		int x0, y0, x1, y1;
		float xStart, yStart, s0, t0, s1, t1;
		float deltaTime = dt * dim;
		for (x = 1; x <= dim; x++)
		{
			for (y = 1; y <= dim; y++)
			{
				xStart = x - deltaTime * velX[at(x, y)];
				yStart = y - deltaTime * velY[at(x, y)];

				xStart = clamp(xStart, dim);
				x0 = (int)xStart;
				x1 = x0 + 1;

				yStart = clamp(yStart, dim);
				y0 = (int)yStart;
				y1 = y0 + 1;

				s1 = xStart - x0;
				s0 = 1 - s1;
				t1 = yStart - y0;
				t0 = 1 - t1;

				newPressure[at(x, y)] = s0 * (t0 * oldPressure[at(x0, y0)] + t1 * oldPressure[at(x0, y1)]) + s1 * (t0 * oldPressure[at(x1, y0)] + t1 * oldPressure[at(x1, y1)]);
			}
		}
	}

	void Start()
	{
		InitCanvas();
		StartCoroutine(Tick());
	}

	IEnumerator Tick()
	{
		for (int i = 0; i < 1000; i++)
		{
			yield return new WaitForSeconds(0.05f);
			gas.Tick();
		}
	}
}
