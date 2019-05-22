using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts
{
	public class DataBuffer<Data>
	{
		#region Buffer Data
		ComputeBuffer gpuBuffer;
		Data[] cpuData;
		int xRes;
		Dictionary<Vector2Int, Data> modifications;
		readonly string BufferName;
		#endregion

		public DataBuffer(string bufferName, Vector3Int resolution)
		{
			modifications = new Dictionary<Vector2Int, Data>();
			BufferName = bufferName;

			xRes = resolution.x;
			cpuData = new Data[resolution.x * resolution.y];
			gpuBuffer = new ComputeBuffer(cpuData.Length, Marshal.SizeOf(typeof(Data)));
		}

		int index(Vector2Int position)
		{
			return position.x + position.y * xRes;
		}

		public void AddDelta(Vector2Int position, Data data)
		{
			modifications[position] = data;
		}

		public void SendUpdatesToGpu()
		{
			gpuBuffer.GetData(cpuData);
			foreach (var kvp in modifications)
			{
				cpuData[index(kvp.Key)] = kvp.Value;
			}
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

		DataBuffer<float> pressure;
		DataBuffer<float> pressureWrite;

		DataBuffer<int> blocked;
		DataBuffer<int> blockedWrite;

		DataBuffer<float> dx;
		DataBuffer<float> dxWrite;

		DataBuffer<float> dy;
		DataBuffer<float> dyWrite;

		int shaderSizeX;

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;

		ComputeShader shader;
		int copyToWrite;
		int copyToRead;
		int forces;
		int doStep;
		int render;
		#endregion

		public GasFlowGpu(Vector3Int resolution)
		{
			Resolution = new Vector3Int(resolution.x, resolution.y, resolution.z);

			RenderTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
			RenderTexture.enableRandomWrite = true;
			RenderTexture.Create();
			RenderTexture.filterMode = FilterMode.Point;

			SetupShaders(resolution);
		}

		public void ApplyDelta<T>(Vector2Int start, Vector2Int end, T delta, DataBuffer<T> tiles)
		{
			for (int x = start.x; x <= end.x; x++)
			{
				for (int y = start.y; y <= end.y; y++)
				{
					tiles.AddDelta(new Vector2Int(x, y), delta);
				}
			}

			tiles.SendUpdatesToGpu();
		}

		void SetupShaders(Vector3Int resolution)
		{
			shaderSizeX = resolution.x;

			pressure = new DataBuffer<float>(nameof(pressure), resolution);
			pressureWrite = new DataBuffer<float>(nameof(pressureWrite), resolution);

			dx = new DataBuffer<float>(nameof(dx), resolution);
			dxWrite = new DataBuffer<float>(nameof(dxWrite), resolution);

			dy = new DataBuffer<float>(nameof(dy), resolution);
			dyWrite = new DataBuffer<float>(nameof(dyWrite), resolution);

			blocked = new DataBuffer<int>(nameof(blocked), resolution);
			blockedWrite = new DataBuffer<int>(nameof(blockedWrite), resolution);

			for (int x = 0; x < resolution.x; x++)
			{
				blocked.AddDelta(new Vector2Int(x, 0), 1);
				blocked.AddDelta(new Vector2Int(x, resolution.y - 1), 1);
			}
			for (int y = 0; y < resolution.y; y++)
			{
				blocked.AddDelta(new Vector2Int(0, y), 1);
				blocked.AddDelta(new Vector2Int(resolution.x - 1, y), 1);
			}


			ApplyDelta(new Vector2Int(10, 16), new Vector2Int(20, 16), 1, blocked);
			blocked.SendUpdatesToGpu();

			ApplyDelta(new Vector2Int(1, 1), new Vector2Int(39, 10), 1f, dx);
			dx.SendUpdatesToGpu();

			pressure.AddDelta(new Vector2Int(10, 10), 1000f);
			pressure.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			{
				shader.SetInt(nameof(shaderSizeX), shaderSizeX);
			}

			//// copyToWrite
			//{
			//	copyToWrite = shader.FindKernel(nameof(copyToWrite));
			//	pressure.SendTo(copyToWrite, shader);
			//	pressureWrite.SendTo(copyToWrite, shader);

			//	blocked.SendTo(copyToWrite, shader);
			//	blockedWrite.SendTo(copyToWrite, shader);

			//	dx.SendTo(copyToWrite, shader);
			//	dxWrite.SendTo(copyToWrite, shader);

			//	dy.SendTo(copyToWrite, shader);
			//	dyWrite.SendTo(copyToWrite, shader);
			//}

			//// copyToRead
			//{
			//	copyToRead = shader.FindKernel(nameof(copyToRead));
			//	pressure.SendTo(copyToRead, shader);
			//	pressureWrite.SendTo(copyToRead, shader);

			//	blocked.SendTo(copyToRead, shader);
			//	blockedWrite.SendTo(copyToRead, shader);

			//	dx.SendTo(copyToRead, shader);
			//	dxWrite.SendTo(copyToRead, shader);

			//	dy.SendTo(copyToRead, shader);
			//	dyWrite.SendTo(copyToRead, shader);
			//}

			//// forces
			//{
			//	forces = shader.FindKernel(nameof(forces));
			//	pressure.SendTo(forces, shader);
			//	pressureWrite.SendTo(forces, shader);

			//	blocked.SendTo(forces, shader);
			//	blockedWrite.SendTo(forces, shader);

			//	dx.SendTo(forces, shader);
			//	dxWrite.SendTo(forces, shader);

			//	dy.SendTo(forces, shader);
			//	dyWrite.SendTo(forces, shader);
			//}

			// doStep
			{
				doStep = shader.FindKernel(nameof(doStep));
				pressure.SendTo(doStep, shader);
				pressureWrite.SendTo(doStep, shader);

				blocked.SendTo(doStep, shader);
				blockedWrite.SendTo(doStep, shader);

				dx.SendTo(doStep, shader);
				dxWrite.SendTo(doStep, shader);

				dy.SendTo(doStep, shader);
				dyWrite.SendTo(doStep, shader);
			}

			// render
			{
				render = shader.FindKernel(nameof(render));
				pressure.SendTo(render, shader);
				pressureWrite.SendTo(render, shader);

				blocked.SendTo(render, shader);
				blockedWrite.SendTo(render, shader);

				dx.SendTo(render, shader);
				dxWrite.SendTo(render, shader);

				dy.SendTo(render, shader);
				dyWrite.SendTo(render, shader);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
			}
		}

		public void Tick()
		{
			int threadGroups = Resolution.x / numXYThreads;

			//shader.Dispatch(copyToWrite, threadGroups, threadGroups, 1);

			shader.Dispatch(doStep, threadGroups, threadGroups, 1);
			//shader.Dispatch(copyToRead, threadGroups, threadGroups, 1);

			//shader.Dispatch(forces, threadGroups, threadGroups, 1);
			//shader.Dispatch(copyToRead, threadGroups, threadGroups, 1);

			shader.Dispatch(render, threadGroups, threadGroups, 1);
		}
	}
}
