﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public class PhysicsShader
	{
		#region C# Only
		public static float clamp(float value, float min, float max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}
		public static float abs(float value)
		{
			return Math.Abs(value);
		}
		public static float sqrt(float value)
		{
			return Mathf.Sqrt(value);
		}
		#endregion

		#region Shader Code Duplicate

		#region Data Structs
		//typedef float[] Gas;

		public struct HotMass
		{
			public HotMass(float e, float cap, float conduct, float m)
			{
				Energy = e;
				HeatCapacity = cap;
				Conduct = conduct;
				Mass = m;
			}

			public float Energy;
			public float HeatCapacity;
			public float Conduct;
			public float Mass;
		};
		#endregion

		#region Heat
		// OVERVIEW
		// heat computes deltas into the 2 deltas textures
		// after computing it, applies it to each cell
		// 2(render steps)

		// find temperature for this body
		public static float getTemp(HotMass a)
		{
			// e = m * c * t
			return a.Energy / (a.Mass * a.HeatCapacity);
		}

		// assume b is hotter and possitive flow is to a
		public static float deltaQFromTemp(HotMass a, HotMass b, float tA, float tB)
		{
			float netConductivity = sqrt(a.Conduct * b.Conduct); // geometric mean
			float dTemp = tB - tA;
			float dTime = 0.1f;
			return netConductivity * dTemp * dTime;
		}

		// cap heat flow to avoid all energy leaving this tile, or one tile getting too hot
		public static float findMaxedDeltaQ(HotMass a, HotMass b, float tA, float tB)
		{
			// cap heat flow to avoid all energy leaving this tile!
			float maxDeltaTemp = abs(tB - tA) / 5.1f; // (5 tiles, NOT 4!), 5.1 to be extra safe about entropy reversal)

			// careful not to have more than 20% of difference in temperature fluctuate
			float aTH = a.Mass * a.HeatCapacity;
			float bTH = b.Mass * b.HeatCapacity;
			bool aHasLessHeatCap = bTH > aTH;
			if (aHasLessHeatCap)
			{
				return aTH * maxDeltaTemp;
			}
			else
			{
				return bTH * maxDeltaTemp;
			}
		}

		// find total heat energy exchange [-Inf, +Inf]
		public static float finalDeltaQ(HotMass a, HotMass b)
		{
			// these tiles could never transmit heat
			if (a.Mass <= 0 || b.Mass <= 0 || a.HeatCapacity <= 0 || b.HeatCapacity <= 0)
			{
				return 0;
			}

			float tempA = getTemp(a);
			float tempB = getTemp(b);

			float deltaEnergy = deltaQFromTemp(a, b, tempA, tempB);
			// avoid strange fluctuations and entropy breaking
			if (abs(deltaEnergy) < 0.00001)
			{
				return 0;
			}

			float maxDeltaEnergy = findMaxedDeltaQ(a, b, tempA, tempB);

			// avoid energy swinging too much
			float final = clamp(deltaEnergy, -maxDeltaEnergy, maxDeltaEnergy);
			return final;
		}
		#endregion

		#region Gas
		// OVERVIEW
		// gas computes mass and energy deltas
		// applies mass and energy deltas

		public static int NumGasses = 2;

		public static float getTotalMass(float[] gasses, int tile)
		{
			float mass = 0;
			for(int i = 0; i<NumGasses; ++i)
			{
				mass += gasses[tile + i];
			}
			return mass;
		}

		// equation computed at https://www.desmos.com/calculator and wolfram
		// y=(0.235(x+1))^{4}*4+(0.235(x-1))^{4}*4 
		// [-1,0,1] -> dA:[0.195, 0.012, 0] dB:[reversed]
		// diff is a measure of mass held as percentage.
		// dA,dB are percentages
		public static void computeGasFlows(float diff, out float dA, out float dB)
		{
			float aIn = 0.235f * (diff - 1);
			float bIn = 0.235f * (diff + 1);

			dA = aIn * aIn * aIn * aIn * 4;
			dB = bIn * bIn * bIn * bIn * 4;
		}

		// a and b masses, dA and dB delta percentages
		public static void findGasDelta(float aMass, float bMass, out float dPercA, out float dPercB)
		{
			float total = aMass + bMass;

			// percentages of each gas of total between the two
			float aP = aMass / total;
			float bP = bMass / total;

			float diff = (bP - aP);// [1, -1];

			computeGasFlows(diff, out dPercA, out dPercB);
		}

		public static void finalGasAndEnergyDeltas(float[] gasTile, float[] )
		{
			// find percentages

			// get gas masses for each
			// get gas energy for each
		}
		#endregion

		#endregion


		public Vector3Int Resolution;

		#region GPU

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

			Mass.SendTo(handle, shader);
			AddRemoveMass.SendTo(handle, shader);

			IsBlocked.SendTo(handle, shader);
			DebugData.SendTo(handle, shader);
		}

		void SetupData(Vector3Int resolution)
		{
			ResolutionX = resolution.x;


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
