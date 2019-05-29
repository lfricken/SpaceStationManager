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
		public RenderTexture VelocityMap;
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

		int set_bnd_project_dxdy;
		int set_bnd_p;
		int set_bnd_project_dxdyRead;

		int copy_all;

		int render_pressure;
		#endregion

		public GasFlowGpu(Vector3Int resolution)
		{
			Resolution = new Vector3Int(resolution.x, resolution.y, resolution.z);
			threadGroups = Resolution.x / numXYThreads;

			RenderTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
			RenderTexture.enableRandomWrite = true;
			RenderTexture.Create();
			RenderTexture.filterMode = FilterMode.Point;

			VelocityMap = new RenderTexture(resolution.x, resolution.y, resolution.z);
			VelocityMap.enableRandomWrite = true;
			VelocityMap.Create();
			VelocityMap.filterMode = FilterMode.Point;

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

		void sendAll(int handle)
		{
			debug.SendTo(handle, shader);
			blocked.SendTo(handle, shader);

			dx.SendTo(handle, shader);
			dxRead.SendTo(handle, shader);

			dy.SendTo(handle, shader);
			dyRead.SendTo(handle, shader);

			pressure.SendTo(handle, shader);
			pressureRead.SendTo(handle, shader);
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

			//ApplyDelta(new Vector2Int(0 + 3, 0 + 3), new Vector2Int(resolution.x - 8, resolution.y - 8), 0.5f, dx);
			dx.SendUpdatesToGpu();

			var center = new Vector2Int(resolution.x - 2, resolution.y - 2);

			var p = resolution.x * resolution.x / 2;

			pressureRead.AddDelta(center, p);
			pressureRead.SendUpdatesToGpu();

			pressure.AddDelta(center, p);
			pressure.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// globals
			{
				int N = shaderSizeX - 2;
				shader.SetInt(nameof(shaderSizeX), shaderSizeX);
				shader.SetInt(nameof(N), N);
				shader.SetFloat(nameof(viscosityGlobal), viscosityGlobal);
				shader.SetFloat(nameof(dtGlobal), dtGlobal);
			}

			//// set_bnd
			//{
			//	set_bnd_diffuse_pressure = shader.FindKernel(nameof(set_bnd_diffuse_pressure));
			//	sendAll(set_bnd_diffuse_pressure);

			//	//pressure.SendTo(set_bnd_diffuse_pressure, shader);
			//	//pressureRead.SendTo(set_bnd_diffuse_pressure, shader);
			//}//



			// render_pressure
			{
				render_pressure = shader.FindKernel(nameof(render_pressure));

				sendAll(render_pressure);
				shader.SetTexture(render_pressure, nameof(RenderTexture), RenderTexture);
				shader.SetTexture(render_pressure, nameof(VelocityMap), VelocityMap);
			}


			// diffuse_pressure
			{
				diffuse_pressure = shader.FindKernel(nameof(diffuse_pressure));
				sendAll(diffuse_pressure);
			}
			// diffuse_dx
			{
				diffuse_dx = shader.FindKernel(nameof(diffuse_dx));
				sendAll(diffuse_dx);
			}
			// diffuse_dy
			{
				diffuse_dy = shader.FindKernel(nameof(diffuse_dy));
				sendAll(diffuse_dy);
			}


			// advect_pressure
			{
				advect_pressure = shader.FindKernel(nameof(advect_pressure));
				sendAll(advect_pressure);
			}
			// advect_dx
			{
				advect_dx = shader.FindKernel(nameof(advect_dx));
				sendAll(advect_dx);
			}
			// advect_dy
			{
				advect_dy = shader.FindKernel(nameof(advect_dy));
				sendAll(advect_dy);
			}


			// project_start
			{
				project_start = shader.FindKernel(nameof(project_start));
				sendAll(project_start);
			}
			// project_loop
			{
				project_loop = shader.FindKernel(nameof(project_loop));
				sendAll(project_loop);
			}
			// project_end
			{
				project_end = shader.FindKernel(nameof(project_end));
				sendAll(project_end);
			}


			// swap_pressure
			{
				swap_pressure = shader.FindKernel(nameof(swap_pressure));
				sendAll(swap_pressure);
			}
			// swap_dx
			{
				swap_dx = shader.FindKernel(nameof(swap_dx));
				sendAll(swap_dx);
			}
			// swap_dy
			{
				swap_dy = shader.FindKernel(nameof(swap_dy));
				sendAll(swap_dy);
			}

			// set_bnd_project_dxdy
			{
				set_bnd_project_dxdy = shader.FindKernel(nameof(set_bnd_project_dxdy));
				sendAll(set_bnd_project_dxdy);
			}

			// set_bnd_p
			{
				set_bnd_p = shader.FindKernel(nameof(set_bnd_p));
				sendAll(set_bnd_p);
			}

			// set_bnd_project_dxdyRead
			{
				set_bnd_project_dxdyRead = shader.FindKernel(nameof(set_bnd_project_dxdyRead));
				sendAll(set_bnd_project_dxdyRead);
			}

			// copy_all
			{
				copy_all = shader.FindKernel(nameof(copy_all));
				sendAll(copy_all);
			}
		}

		void copy()
		{
			Run(copy_all);
		}

		public void Tick()
		{
			vel_step();
			dense_step();
			display();
			copy();

			debug.SendUpdatesToGpu();
			Debug.Log(debug.cpuData[0]);
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
			if (FieldType.Pressure == type)
				Run(swap_pressure);
			if (FieldType.DeltaX == type)
				Run(swap_dx);
			if (FieldType.DeltaY == type)
				Run(swap_dy);
		}

		void diffuse(FieldType type)
		{
			for (int k = 0; k < iterations; ++k)
			{
				if (FieldType.Pressure == type)
					Run(diffuse_pressure);
				if (FieldType.DeltaX == type)
					Run(diffuse_dx);
				if (FieldType.DeltaY == type)
					Run(diffuse_dy);
			}
		}

		void advect(FieldType type)
		{
			if (FieldType.Pressure == type)
				Run(advect_pressure);
			if (FieldType.DeltaX == type)
				Run(advect_dx);
			if (FieldType.DeltaY == type)
				Run(advect_dy);
		}

		void project()
		{
			Run(project_start);
			Run(set_bnd_project_dxdyRead);

			for (int k = 0; k < iterations; ++k)
			{
				Run(project_loop);
				Run(set_bnd_p);
			}

			Run(project_end);
			Run(set_bnd_project_dxdy);
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
			swap(FieldType.Pressure);
			diffuse(FieldType.Pressure);

			swap(FieldType.Pressure);
			advect(FieldType.Pressure);
		}

		void display()
		{
			Run(render_pressure);
		}
	}
}
