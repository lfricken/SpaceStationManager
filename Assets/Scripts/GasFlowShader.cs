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

		DataBuffer<float> pressureRead;
		DataBuffer<float> pressure;

		DataBuffer<int> blockedRead;
		DataBuffer<int> blocked;

		DataBuffer<float> dxRead;
		DataBuffer<float> dx;

		DataBuffer<float> dyRead;
		DataBuffer<float> dy;

		int shaderSizeX;
		float viscosityGlobal;
		float dtGlobal;

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;
		int threadGroups;

		ComputeShader shader;

		int advect_dx;
		int advect_dy;
		int advect_pressure;

		int swap_dx;
		int swap_dy;
		int swap_pressure;

		int project_start;
		int project_loop;
		int project_end;

		int render;
		#endregion

		public GasFlowGpu(Vector3Int resolution)
		{
			Resolution = new Vector3Int(resolution.x, resolution.y, resolution.z);
			threadGroups = Resolution.x / numXYThreads;

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
			pressureRead = new DataBuffer<float>(nameof(pressureRead), resolution);

			dx = new DataBuffer<float>(nameof(dx), resolution);
			dxRead = new DataBuffer<float>(nameof(dxRead), resolution);

			dy = new DataBuffer<float>(nameof(dy), resolution);
			dyRead = new DataBuffer<float>(nameof(dyRead), resolution);

			blocked = new DataBuffer<int>(nameof(blocked), resolution);
			blockedRead = new DataBuffer<int>(nameof(blockedRead), resolution);

			//for (int x = 0; x < resolution.x; x++)
			//{
			//	blocked.AddDelta(new Vector2Int(x, 0), 1);
			//	blocked.AddDelta(new Vector2Int(x, resolution.y - 1), 1);
			//}
			//for (int y = 0; y < resolution.y; y++)
			//{
			//	blocked.AddDelta(new Vector2Int(0, y), 1);
			//	blocked.AddDelta(new Vector2Int(resolution.x - 1, y), 1);
			//}


			//ApplyDelta(new Vector2Int(10, 16), new Vector2Int(20, 16), 1, blocked);
			//blocked.SendUpdatesToGpu();

			ApplyDelta(new Vector2Int(1, 1), new Vector2Int(16, 16), 0f, dx);
			dx.SendUpdatesToGpu();

			pressure.AddDelta(new Vector2Int(15, 15), 500);
			pressure.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			{
				shader.SetInt(nameof(shaderSizeX), shaderSizeX);
				shader.SetFloat(nameof(shaderSizeX), shaderSizeX);
			}

			// clear
			//{
			//	clear = shader.FindKernel(nameof(clear));
			//	blocked.SendTo(copyToWrite, shader);
			//	blockedWrite.SendTo(copyToWrite, shader);
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
				//doStep = shader.FindKernel(nameof(doStep));
				//pressure.SendTo(doStep, shader);
				//pressureWrite.SendTo(doStep, shader);

				//blocked.SendTo(doStep, shader);
				//blockedWrite.SendTo(doStep, shader);

				//dx.SendTo(doStep, shader);
				//dxWrite.SendTo(doStep, shader);

				//dy.SendTo(doStep, shader);
				//dyWrite.SendTo(doStep, shader);
			}

			// render
			{
				render = shader.FindKernel(nameof(render));
				pressure.SendTo(render, shader);
				pressureRead.SendTo(render, shader);

				blocked.SendTo(render, shader);
				blockedRead.SendTo(render, shader);

				dx.SendTo(render, shader);
				dxRead.SendTo(render, shader);

				dy.SendTo(render, shader);
				dyRead.SendTo(render, shader);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
			}
		}

		enum FieldType
		{
			DeltaX = 0,
			DeltaY,
			Pressure,
		}

		void Run(int handle)
		{
			shader.Dispatch(handle, threadGroups, threadGroups, 1);
		}

		void swap(FieldType type)
		{
			if (FieldType.DeltaX == type)
				Run(swap_dx);
			if (FieldType.DeltaY == type)
				Run(swap_dy);
			if (FieldType.Pressure == type)
				Run(swap_pressure);
		}

		void advect(FieldType type)
		{
			if (FieldType.DeltaX == type)
				Run(advect_dx);
			if (FieldType.DeltaY == type)
				Run(advect_dy);
			if (FieldType.Pressure == type)
				Run(advect_pressure);
		}

		void vel_step()
		{
			//swap(FieldType.DeltaX); diffuse();
		}

		void project()
		{

		}

		void dense_step()
		{

		}

		void diffuse(FieldType type)
		{

		}

		public void Tick()
		{

			//shader.Dispatch(copyToWrite, threadGroups, threadGroups, 1);

			//shader.Dispatch(doStep, threadGroups, threadGroups, 1);
			//shader.Dispatch(copyToRead, threadGroups, threadGroups, 1);

			//shader.Dispatch(forces, threadGroups, threadGroups, 1);
			//shader.Dispatch(copyToRead, threadGroups, threadGroups, 1);


			shader.Dispatch(render, threadGroups, threadGroups, 1);
		}

	}
}
