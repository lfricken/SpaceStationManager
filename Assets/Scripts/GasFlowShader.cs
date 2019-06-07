using System;
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
		DataBuffer<Delta> Delta2;
		DataBuffer<Delta> Delta;

		DataBuffer<double> Mass;

		DataBuffer<int> IsBlocked;
		DataBuffer<int> DebugData;

		public RenderTexture RenderTexture;
		public RenderTexture VelocityMap;
		public RenderTexture FakeMap;

		const int NumSides = 4;
		int ResolutionX;
		const float VelocityConservation = 0.994f;
		const float MassCollisionDeflectFraction = 6.0f;
		const float WaveSpread = 0.1f;
		const float MaxPressureRender = 2.0f;
		const float MaxDeltaRender = 0.5f;
		#endregion

		#region Shader
		readonly int numXYThreads = 8; // If you update this, you need to update gas.compute!
		int threadGroups;
		ComputeShader shader;
		#endregion

		#region Kernels
		int diffuse_deltas;
		int set_mass;
		int share_deltas;
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

			FakeMap = new RenderTexture(resolution.x, resolution.y, resolution.z);
			FakeMap.enableRandomWrite = true;
			FakeMap.Create();
			FakeMap.filterMode = FilterMode.Point;

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
			Delta2.SendTo(handle, shader);
			Delta.SendTo(handle, shader);

			Mass.SendTo(handle, shader);

			IsBlocked.SendTo(handle, shader);
			DebugData.SendTo(handle, shader);
		}

		void SetupData(Vector3Int resolution)
		{
			ResolutionX = resolution.x;

			Delta2 = new DataBuffer<Delta>(nameof(Delta2), resolution);
			Delta = new DataBuffer<Delta>(nameof(Delta), resolution);

			Mass = new DataBuffer<double>(nameof(Mass), resolution);

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
				ApplyDelta(new Vector2Int(5, 10), new Vector2Int(resolution.x - 80, 1), 1, IsBlocked);

				//ApplyDelta(new Vector2Int(0, 2), new Vector2Int(resolution.x, 1), 1, IsBlocked);
				IsBlocked.SendUpdatesToGpu();
			}
			
			//Delta d = new Delta { r = 1, d = 1, l = 1, u = 1, };
			//ApplyDelta(new Vector2Int(4, 4), new Vector2Int(5, 1), d, Delta);
			Delta.SendUpdatesToGpu();

			//// pressure
			//ApplyDelta(new Vector2Int(5, 5), new Vector2Int(1, 1), 10f, Mass);
			//Mass.SendUpdatesToGpu();

			// pressure
			{
				var center = new Vector2Int(3, 1);// new Vector2Int(resolution.x / 2, resolution.y / 2);
				var p = resolution.x * resolution.x / 2;

				Mass.AddDelta(center, p);
				Mass.SendUpdatesToGpu();
			}
		}

		void SetupShaders(Vector3Int resolution)
		{
			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// globals
			{
				shader.SetInt(nameof(NumSides), NumSides);
				shader.SetInt(nameof(ResolutionX), ResolutionX);
				shader.SetFloat(nameof(VelocityConservation), VelocityConservation);
				shader.SetFloat(nameof(MassCollisionDeflectFraction), MassCollisionDeflectFraction);
				shader.SetFloat(nameof(WaveSpread), WaveSpread);
				shader.SetFloat(nameof(MaxPressureRender), MaxPressureRender);
				shader.SetFloat(nameof(MaxDeltaRender), MaxDeltaRender);
			}
			// render
			{
				render = shader.FindKernel(nameof(render));

				sendAll(render);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
				shader.SetTexture(render, nameof(VelocityMap), VelocityMap);
				shader.SetTexture(render, nameof(FakeMap), FakeMap);
			}
			// diffuse_deltas
			{
				diffuse_deltas = shader.FindKernel(nameof(diffuse_deltas));
				sendAll(diffuse_deltas);
			}
			// set_mass
			{
				set_mass = shader.FindKernel(nameof(set_mass));
				sendAll(set_mass);
			}
			// share_deltas
			{
				share_deltas = shader.FindKernel(nameof(share_deltas));
				sendAll(share_deltas);
			}
			// copy_all
			{
				copy_all = shader.FindKernel(nameof(copy_all));
				sendAll(copy_all);
			}
		}

		public void Tick()
		{
			Run(diffuse_deltas);
			Run(set_mass);
			Run(share_deltas);
			//Run(copy_all);
			Run(render);

			DebugData.SendUpdatesToGpu();

			//Debug.Log(DebugData.cpuData[0]);
		}

		void Run(int handle)
		{
			shader.Dispatch(handle, threadGroups, threadGroups, 1);
		}
	}
}
