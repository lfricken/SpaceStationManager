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

	void project(float dims, float[] velX, float[] velY, float[] velX0, float[] velY0)
	{
		int posx, posy, k;
		float h;
		h = 1.0f / dims;
		for (posx = 1; posx <= dims; posx++)
		{
			for (posy = 1; posy <= dims; posy++)
			{
				velY0[at(posx, posy)] = -0.5f * h * (velX[at(posx + 1, posy)] - velX[at(posx - 1, posy)] +
				velY[at(posx, posy + 1)] - velY[at(posx, posy - 1)]);
				velX0[at(posx, posy)] = 0;
			}
		}
		set_bnd(dims, 0, velY0); set_bnd(dims, 0, velX0);
		for (k = 0; k < 20; k++)
		{
			for (posx = 1; posx <= dims; posx++)
			{
				for (posy = 1; posy <= dims; posy++)
				{
					velX0[at(posx, posy)] = (velY0[at(posx, posy)] + velX0[at(posx - 1, posy)] + velX0[at(posx + 1, posy)] +
					 velX0[at(posx, posy - 1)] + velX0[at(posx, posy + 1)]) / 4;
				}
			}
			set_bnd(dims, 0, velX0);
		}
		for (posx = 1; posx <= dims; posx++)
		{
			for (posy = 1; posy <= dims; posy++)
			{
				velX[at(posx, posy)] -= 0.5f * (velX0[at(posx + 1, posy)] - velX0[at(posx - 1, posy)]) / h;
				velY[at(posx, posy)] -= 0.5f * (velX0[at(posx, posy + 1)] - velX0[at(posx, posy - 1)]) / h;
			}
		}
		set_bnd(dims, 1, velX); set_bnd(dims, 2, velY);
	}

	void set_bnd(float _dims, int boundary, float[] tiles)
	{
		int dims = (int)_dims;
		int i;
		for (i = 1; i <= dims; i++)
		{
			tiles[at(0, i)] = (boundary == 1) ? -tiles[at(1, i)] : tiles[at(1, i)];
			tiles[at(dims + 1, i)] = boundary == 1 ? -tiles[at(dims, i)] : tiles[at(dims, i)];
			tiles[at(i, 0)] = boundary == 2 ? -tiles[at(i, 1)] : tiles[at(i, 1)];
			tiles[at(i, dims + 1)] = boundary == 2 ? -tiles[at(i, dims)] : tiles[at(i, dims)];
		}
		tiles[at(0, 0)] = 0.5f * (tiles[at(1, 0)] + tiles[at(0, 1)]);
		tiles[at(0, dims + 1)] = 0.5f * (tiles[at(1, dims + 1)] + tiles[at(0, dims)]);
		tiles[at(dims + 1, 0)] = 0.5f * (tiles[at(dims, 0)] + tiles[at(dims + 1, 1)]);
		tiles[at(dims + 1, dims + 1)] = 0.5f * (tiles[at(dims, dims + 1)] + tiles[at(dims + 1, dims)]);
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
			yield return new WaitForSeconds(0.001f);
			gas.Tick();
		}
	}
}
