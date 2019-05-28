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
		public Data[] cpuData;
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

		DataBuffer<int> blocked;

		DataBuffer<float> dxRead;
		DataBuffer<float> dx;

		DataBuffer<float> dyRead;
		DataBuffer<float> dy;

		DataBuffer<int> debug;

		int shaderSizeX;

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;
		int threadGroups;

		const float viscosityGlobal = 0.1f;
		const int iterations = 20; // needs to be even because we are ping ponging values
		float dtGlobal = 0.1f;

		ComputeShader shader;

		int swap_dx;
		int swap_dy;
		int swap_pressure;

		int set_bnd_diffuse_pressure;

		int diffuse_dx;
		int diffuse_dy;
		int diffuse_pressure;

		int advect_dx;
		int advect_dy;
		int advect_pressure;

		int project_start;
		int project_loop;
		int project_end;

		int render_pressure;
		int totalMass1000;
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

		public void ApplyDelta<T>(Vector2Int start, Vector2Int size, T delta, DataBuffer<T> tiles)
		{
			Vector2Int end = start + size;
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
			dtGlobal *= shaderSizeX;


			pressure = new DataBuffer<float>(nameof(pressure), resolution);
			pressureRead = new DataBuffer<float>(nameof(pressureRead), resolution);

			dx = new DataBuffer<float>(nameof(dx), resolution);
			dxRead = new DataBuffer<float>(nameof(dxRead), resolution);

			dy = new DataBuffer<float>(nameof(dy), resolution);
			dyRead = new DataBuffer<float>(nameof(dyRead), resolution);

			blocked = new DataBuffer<int>(nameof(blocked), resolution);
			debug = new DataBuffer<int>(nameof(debug), new Vector3Int(10, 1, 1));

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
			blocked.SendUpdatesToGpu();

			ApplyDelta(new Vector2Int(0 + 3, 0 + 3), new Vector2Int(resolution.x - 8, 4), 0.1f, dx);
			dx.SendUpdatesToGpu();

			var center = new Vector2Int(resolution.x / 2, resolution.y / 2);

			var p = resolution.x * resolution.x / 2;

			pressureRead.AddDelta(center, p);
			pressureRead.SendUpdatesToGpu();

			pressure.AddDelta(center, p);
			pressure.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// globals
			{
				shader.SetInt(nameof(shaderSizeX), shaderSizeX);
				shader.SetFloat(nameof(viscosityGlobal), viscosityGlobal);
				shader.SetFloat(nameof(dtGlobal), dtGlobal);
			}

			// set_bnd
			{
				set_bnd_diffuse_pressure = shader.FindKernel(nameof(set_bnd_diffuse_pressure));

				pressure.SendTo(set_bnd_diffuse_pressure, shader);
				pressureRead.SendTo(set_bnd_diffuse_pressure, shader);
			}

			// diffuse
			{
				diffuse_pressure = shader.FindKernel(nameof(diffuse_pressure));

				debug.SendTo(diffuse_pressure, shader);
				blocked.SendTo(diffuse_pressure, shader);
				pressure.SendTo(diffuse_pressure, shader);
				pressureRead.SendTo(diffuse_pressure, shader);
			}

			// advect_pressure
			{
				advect_pressure = shader.FindKernel(nameof(advect_pressure));

				dx.SendTo(advect_pressure, shader);
				dy.SendTo(advect_pressure, shader);

				debug.SendTo(advect_pressure, shader);
				blocked.SendTo(advect_pressure, shader);
				pressure.SendTo(advect_pressure, shader);
				pressureRead.SendTo(advect_pressure, shader);
			}

			// swap
			{
				swap_pressure = shader.FindKernel(nameof(swap_pressure));

				debug.SendTo(diffuse_pressure, shader);
				pressure.SendTo(swap_pressure, shader);
				pressureRead.SendTo(swap_pressure, shader);
			}

			// render
			{
				render_pressure = shader.FindKernel(nameof(render_pressure));

				debug.SendTo(render_pressure, shader);
				blocked.SendTo(render_pressure, shader);
				pressure.SendTo(render_pressure, shader);
				pressureRead.SendTo(render_pressure, shader);
				shader.SetTexture(render_pressure, nameof(RenderTexture), RenderTexture);
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

		void diffuse(FieldType type)
		{
			for (int k = 0; k < iterations; ++k)
			{
				if (FieldType.DeltaX == type)
					Run(diffuse_dx);
				if (FieldType.DeltaY == type)
					Run(diffuse_dy);
				if (FieldType.Pressure == type)
					Run(diffuse_pressure);
				Run(set_bnd_diffuse_pressure);
			}
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

		void project()
		{
			Run(project_start);
			for (int k = 0; k < iterations; ++k)
				Run(project_loop);
			Run(project_end);
		}

		void vel_step()
		{
			// add source
			swap(FieldType.DeltaX); diffuse(FieldType.DeltaX);
			swap(FieldType.DeltaY); diffuse(FieldType.DeltaY);
			project();

			swap(FieldType.DeltaX); swap(FieldType.DeltaY);

			advect(FieldType.DeltaX); advect(FieldType.DeltaY);
			project();
		}

		void dense_step()
		{
			FieldType pressure = FieldType.Pressure;
			swap(pressure);
			diffuse(pressure);

			swap(pressure);
			advect(pressure);
		}

		void display()
		{
			Run(render_pressure);
		}

		int i = 0;

		public void Tick()
		{
			//vel_step();
			//if (i < 2)
			{
				++i;
				dense_step();
				display();
			}

			debug.SendUpdatesToGpu();
			Debug.Log(debug.cpuData[0]);
		}

	}
}
