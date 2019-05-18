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

		public DataBuffer(string bufferName, Vector3Int resolution)
		{
			BufferName = bufferName;

			cpuData = new Data[resolution.x * resolution.y];
			gpuBuffer = new ComputeBuffer(cpuData.Length, Marshal.SizeOf(typeof(Data)));
		}

		public void SendUpdatesToGpu()
		{
			gpuBuffer.SetData(cpuData);
		}

		public void SendTo(int handle, ComputeShader shader)
		{
			shader.SetBuffer(handle, BufferName, gpuBuffer);
		}
	}

	public class GasFlowGpu
	{
		public Vector3Int Resolution;

		DataBuffer<PressureTile> tiles;

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;

		ComputeShader shader;
		int disperse;
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
			tiles = new DataBuffer<PressureTile>("PressureTiles", resolution);
			tiles.cpuData[1].blocked = 1;
			tiles.cpuData[0].pressure = 1;
			tiles.cpuData[2].pressure = 1;
			tiles.cpuData[3].pressure = 1;
			tiles.cpuData[63].blocked = 1;
			tiles.cpuData[67].blocked = 1;
			tiles.cpuData[323].blocked = 1;
			tiles.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// disperse
			{
				disperse = shader.FindKernel(nameof(disperse));
				tiles.SendTo(disperse, shader);
			}

			// render
			{
				render = shader.FindKernel(nameof(render));
				tiles.SendTo(render, shader);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
			}
		}

		public void Tick()
		{
			int threadGroups = Resolution.x / numXYThreads;

			shader.Dispatch(disperse, threadGroups, threadGroups, 1);
			shader.Dispatch(render, threadGroups, threadGroups, 1);
		}
	}
}
