using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts
{
	struct PressureTile
	{
		public float pressure;
		public int blocked;
	}

	class DataBuffer<Data>
	{
		#region Buffer Data
		ComputeBuffer gpuBuffer;
		public Data[] cpuData; // todo: add public accessors to modify data
		readonly string BufferName;
		#endregion

		public DataBuffer(ComputeShader shader, int handle, string bufferName, Vector3Int resolution)
		{
			BufferName = bufferName;

			cpuData = new Data[resolution.x * resolution.y];
			gpuBuffer = new ComputeBuffer(cpuData.Length, Marshal.SizeOf(typeof(Data)));
			shader.SetBuffer(handle, BufferName, gpuBuffer);
		}

		public void SendUpdatesToGpu()
		{
			gpuBuffer.SetData(cpuData);
		}
	}

	public class GasFlowGpu
	{
		public Vector3Int Resolution;

		DataBuffer<PressureTile> tiles;

		#region Shader
		public RenderTexture RenderTexture;
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

			SetupShaders(resolution);
		}

		void SetupShaders(Vector3Int resolution)
		{
			shader = Resources.Load<ComputeShader>("gas");

			// render
			render = shader.FindKernel(nameof(render));

			shader.SetTexture(render, nameof(RenderTexture), RenderTexture);

			tiles = new DataBuffer<PressureTile>(shader, render, "PressureTiles", resolution);
			tiles.cpuData[1].blocked = 1;
			tiles.cpuData[0].pressure = 1;
			tiles.cpuData[2].pressure = 1;
			tiles.cpuData[3].pressure = 1;
			tiles.SendUpdatesToGpu();
		}

		public void Tick()
		{
			shader.Dispatch(render, Resolution.x / numThreads, Resolution.y / numThreads, 1);
		}
	}
}
