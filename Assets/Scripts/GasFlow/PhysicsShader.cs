﻿using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public class PhysicsShader
	{
		public struct Deltas
		{
			public double r;
			public double d;
			public double l;
			public double u;
		}

		public static void FindDelta(float a, float b, out float dA, out float dB)
		{
			float total = a + b;
			float aP = a / total;
			float bP = b / total;

			float diff = (bP - aP);// [1, -1];

			Graph(diff, out dA, out dB);
			dA *= a;
			dB *= b;
		}

		public static void Graph(float diff, out float dA, out float dB)
		{
			float aIn = 0.25f * (diff - 1);
			float bIn = 0.25f * (diff + 1);

			dA = aIn * aIn * aIn * aIn * 4;
			dB = bIn * bIn * bIn * bIn * 4;
		}

		public Vector3Int Resolution;

		#region GPU
		DataBuffer<Deltas> Delta2;
		DataBuffer<Deltas> Delta;

		DataBuffer<double> Mass;
		public DataBuffer<double> AddRemoveMass;

		public DataBuffer<int> IsBlocked;
		DataBuffer<int> DebugData;

		public List<RenderTexture> RenderTextures;

		public RenderTexture RenderTexture;
		public RenderTexture VelocityMap;
		public RenderTexture FakeMap;

		const int NumSides = 4;
		int ResolutionX;
		const float VelocityConservation = 0.99f;
		const float MassCollisionDeflectFraction = 6.0f;
		const float WaveSpread = 0.15f;
		const float MaxPressureRender = 2.0f;
		const float MaxDeltaRender = 0.5f;
		#endregion

		#region Shader
		readonly int numXYThreads = 8; // If you update this, you need to update gas.compute!
		readonly int threadGroups;
		ComputeShader shader;
		#endregion

		#region Kernels
		int diffuse_deltas;
		int set_mass;
		int share_deltas;
		int render;
		#endregion

		public PhysicsShader(Vector3Int resolution, ComputeShader shader)
		{
			this.shader = shader;
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

			RenderTextures = new List<RenderTexture>();
			RenderTextures.Add(RenderTexture);
			RenderTextures.Add(VelocityMap);

			SetupData(resolution);
			SetupShaders(resolution);
		}

		void ApplyDelta<T>(Vector2Int start, Vector2Int size, T delta, DataBuffer<T> tiles)
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

		void SendAll(int handle)
		{
			Delta2.SendTo(handle, shader);
			Delta.SendTo(handle, shader);

			Mass.SendTo(handle, shader);
			AddRemoveMass.SendTo(handle, shader);

			IsBlocked.SendTo(handle, shader);
			DebugData.SendTo(handle, shader);
		}

		void SetupData(Vector3Int resolution)
		{
			ResolutionX = resolution.x;

			Delta2 = new DataBuffer<Deltas>(nameof(Delta2), resolution);
			Delta = new DataBuffer<Deltas>(nameof(Delta), resolution);

			Mass = new DataBuffer<double>(nameof(Mass), resolution);
			AddRemoveMass = new DataBuffer<double>(nameof(AddRemoveMass), resolution);

			IsBlocked = new DataBuffer<int>(nameof(IsBlocked), resolution);
			DebugData = new DataBuffer<int>(nameof(DebugData), new Vector3Int(10, 1, 1));


			// blocked
			{
				for (int x = 0; x < resolution.x; x++)
				{
					IsBlocked.AddDelta(new Vector2Int(x, 0), 1);
					IsBlocked.AddDelta(new Vector2Int(x, resolution.y - 1), 1);
				}
				IsBlocked.SendUpdatesToGpu();

				for (int y = 0; y < resolution.y; y++)
				{
					IsBlocked.AddDelta(new Vector2Int(0, y), 1);
					IsBlocked.AddDelta(new Vector2Int(resolution.x - 1, y), 1);
				}
				IsBlocked.SendUpdatesToGpu();


				ApplyDelta(new Vector2Int(0, 20), new Vector2Int(resolution.x - 5, 1), 1, IsBlocked);
				ApplyDelta(new Vector2Int(0, 10), new Vector2Int(resolution.x / 2, 1), 1, IsBlocked);
				ApplyDelta(new Vector2Int(0, 2), new Vector2Int(resolution.x - 5, 1), 1, IsBlocked);

			}


			// pressure
			{
				var center = new Vector2Int(1, 1);// new Vector2Int(resolution.x / 2, resolution.y / 2);
				var p = resolution.x * resolution.x / 2;

				Mass.AddDelta(center, 2000);
				Mass.SendUpdatesToGpu();

				AddRemoveMass.AddDelta(center, 0.1);
				AddRemoveMass.AddDelta(new Vector2Int(21, 23), -5);
				AddRemoveMass.SendUpdatesToGpu();
			}
		}

		void SetupShaders(Vector3Int resolution)
		{
			// shader

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

				SendAll(render);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
				shader.SetTexture(render, nameof(VelocityMap), VelocityMap);
				shader.SetTexture(render, nameof(FakeMap), FakeMap);
			}
			// diffuse_deltas
			{
				diffuse_deltas = shader.FindKernel(nameof(diffuse_deltas));
				SendAll(diffuse_deltas);
			}
			// set_mass
			{
				set_mass = shader.FindKernel(nameof(set_mass));
				SendAll(set_mass);
			}
			// share_deltas
			{
				share_deltas = shader.FindKernel(nameof(share_deltas));
				SendAll(share_deltas);
			}
		}

		public void Tick()
		{
			Run(diffuse_deltas);
			Run(set_mass);
			Run(share_deltas);
			Run(render);
		}

		void Run(int handle)
		{
			shader.Dispatch(handle, threadGroups, threadGroups, 1);
		}

		public void Dispose()
		{
			//Mass.Dispose();
			//Delta2.Dispose();
			//Delta.Dispose();

			//AddRemoveMass.Dispose();

			//IsBlocked.Dispose();
			//DebugData.Dispose();
		}
	}
}
