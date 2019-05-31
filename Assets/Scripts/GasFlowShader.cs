﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts
{
	struct Delta
	{
		public double r;
		public double d;
		public double l;
		public double u;
	}

	public class GasFlowGpu
	{
		public Vector3Int Resolution;

		#region GPU
		DataBuffer<Delta> DeltaRead;
		DataBuffer<Delta> Delta;

		DataBuffer<double> PressureRead;
		DataBuffer<double> Pressure;

		DataBuffer<int> IsBlocked;
		DataBuffer<int> DebugData;

		public RenderTexture RenderTexture;
		public RenderTexture VelocityMap;

		const int NumSides = 4;
		const float Rate = 0.1f;
		int ResolutionX;
		const float ViscosityGlobal = 0.01f;
		const float DtGlobal = 0.01f;
		#endregion

		#region Shader
		readonly int numXYThreads = 16; // If you update this, you need to update gas.compute!
		int threadGroups;
		ComputeShader shader;
		#endregion

		#region Kernels
		int calc_diffusion_forces;
		int apply_diffusion_forces;
		int diffuse_forces;
		int copy_all;
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

			VelocityMap = new RenderTexture(resolution.x, resolution.y, resolution.z);
			VelocityMap.enableRandomWrite = true;
			VelocityMap.Create();
			VelocityMap.filterMode = FilterMode.Point;

			SetupData(resolution);
			SetupShaders(resolution);
		}

		public void ApplyDelta<T>(Vector2Int start, Vector2Int size, T delta, DataBuffer<T> tiles)
		{
			Vector2Int end = start + size;
			for (int x = start.x; x < end.x; x++)
			{
				for (int y = start.y; y < end.y; y++)
				{
					tiles.AddDelta(new Vector2Int(x, y), delta);
				}
			}

			tiles.SendUpdatesToGpu();
		}

		void sendAll(int handle)
		{
			DeltaRead.SendTo(handle, shader);
			Delta.SendTo(handle, shader);

			PressureRead.SendTo(handle, shader);
			Pressure.SendTo(handle, shader);

			IsBlocked.SendTo(handle, shader);
			DebugData.SendTo(handle, shader);
		}

		void SetupData(Vector3Int resolution)
		{
			ResolutionX = resolution.x;

			DeltaRead = new DataBuffer<Delta>(nameof(DeltaRead), resolution);
			Delta = new DataBuffer<Delta>(nameof(Delta), resolution);

			PressureRead = new DataBuffer<double>(nameof(PressureRead), resolution);
			Pressure = new DataBuffer<double>(nameof(Pressure), resolution);

			IsBlocked = new DataBuffer<int>(nameof(IsBlocked), resolution);
			DebugData = new DataBuffer<int>(nameof(DebugData), new Vector3Int(10, 1, 1));


			// blocked
			{
				for (int x = 0; x < resolution.x; x++)
				{
					IsBlocked.AddDelta(new Vector2Int(x, 0), 1);
					IsBlocked.AddDelta(new Vector2Int(x, resolution.y - 1), 1);
				}
				for (int y = 0; y < resolution.y; y++)
				{
					IsBlocked.AddDelta(new Vector2Int(0, y), 1);
					IsBlocked.AddDelta(new Vector2Int(resolution.x - 1, y), 1);
				}

				ApplyDelta(new Vector2Int(0, 2), new Vector2Int(resolution.x, 1), 1, IsBlocked);
				IsBlocked.SendUpdatesToGpu();
			}

			//Delta d = new Delta { r = 1, d = 1, l = 1, u = 1, };
			//ApplyDelta(new Vector2Int(4, 4), new Vector2Int(5, 1), d, Delta);
			Delta.SendUpdatesToGpu();

			//// pressure
			//ApplyDelta(new Vector2Int(5, 5), new Vector2Int(1, 1), 10f, Pressure);
			//Pressure.SendUpdatesToGpu();

			// pressure
			{
				var center = new Vector2Int(3, 1);// new Vector2Int(resolution.x / 2, resolution.y / 2);
				var p = resolution.x * resolution.x / 2;

				PressureRead.AddDelta(center, 10);
				PressureRead.SendUpdatesToGpu();

				Pressure.AddDelta(center, 10);
				Pressure.SendUpdatesToGpu();
			}
		}

		void SetupShaders(Vector3Int resolution)
		{
			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// globals
			{
				shader.SetInt(nameof(NumSides), NumSides);
				shader.SetFloat(nameof(Rate), Rate);
				shader.SetInt(nameof(ResolutionX), ResolutionX);
				shader.SetFloat(nameof(ViscosityGlobal), ViscosityGlobal);
				shader.SetFloat(nameof(DtGlobal), DtGlobal);
			}
			// render
			{
				render = shader.FindKernel(nameof(render));

				sendAll(render);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
				shader.SetTexture(render, nameof(VelocityMap), VelocityMap);
			}
			// calc_diffusion_forces
			{
				calc_diffusion_forces = shader.FindKernel(nameof(calc_diffusion_forces));
				sendAll(calc_diffusion_forces);
			}
			// apply_diffusion_forces
			{
				apply_diffusion_forces = shader.FindKernel(nameof(apply_diffusion_forces));
				sendAll(apply_diffusion_forces);
			}
			// diffuse_forces
			{
				diffuse_forces = shader.FindKernel(nameof(diffuse_forces));
				sendAll(diffuse_forces);
			}
			// copy_all
			{
				copy_all = shader.FindKernel(nameof(copy_all));
				sendAll(copy_all);
			}
		}

		public void Tick()
		{
			Run(calc_diffusion_forces);
			Run(apply_diffusion_forces);
			Run(render);
			Run(diffuse_forces);

			DebugData.SendUpdatesToGpu();

			//Debug.Log(DebugData.cpuData[0]);
		}

		void Run(int handle)
		{
			shader.Dispatch(handle, threadGroups, threadGroups, 1);
		}
	}
}
