using UnityEngine;

namespace Assets.Scripts
{
	struct PressureTile
	{
		public float pressure;
		public int blocked;
	}

	public class GasFlowGpu
	{
		#region Buffer Data
		ComputeBuffer tileBuffer;
		PressureTile[] tileArray;
		#endregion

		#region Shader
		public RenderTexture RenderTexture;
		public Vector3Int Resolution;
		readonly int numThreads = 8;

		ComputeShader shader;
		int render;
		#endregion

		public GasFlowGpu(Vector3Int resolution)
		{
			Resolution = resolution;

			RenderTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
			RenderTexture.enableRandomWrite = true;
			RenderTexture.Create();
			RenderTexture.filterMode = FilterMode.Point;

			SetupTiles(resolution);
			SetupShaders();
		}

		void SetupTiles(Vector3Int resolution)
		{
			// array
			tileArray = new PressureTile[resolution.x * resolution.y];
			for (int i = 0; i < tileArray.Length; ++i)
			{
				tileArray[i].pressure = 0;
				tileArray[i].blocked = 0;
			}

			// buffer
			tileBuffer = new ComputeBuffer(tileArray.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PressureTile)));
			tileBuffer.SetData(tileArray);
		}

		void SetupShaders()
		{
			shader = Resources.Load<ComputeShader>("gas");
			render = shader.FindKernel(nameof(render));
			shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
		}

		public void Tick()
		{
			shader.Dispatch(render, Resolution.x / numThreads, Resolution.y / numThreads, 1);
		}
	}
}
