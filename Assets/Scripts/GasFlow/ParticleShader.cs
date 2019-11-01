using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public struct Float2
	{
		public float x;
		public float y;
	}

	public struct Particle
	{
		public Float2 pos;
		public Float2 vel;
	}

	public class ParticleGpu
	{
		public Vector3Int Resolution;

		#region GPU
		DataBuffer<Particle> Oxygens;
		DataBuffer<int> Mass;
		public DataBuffer<int> IsBlocked;

		public RenderTexture RenderTexture;
		public RenderTexture VelocityMap;
		public RenderTexture FakeMap;
		public List<RenderTexture> RenderTextures;

		int ParticlesPerPoint;
		int ResolutionX;

		const float MaxPressureRender = 50.0f;
		const float MaxDeltaRender = 0.5f;
		#endregion

		#region Shader
		readonly int numXYThreads = 8; // If you update this, you need to update gas.compute!
		readonly int threadGroups;
		ComputeShader shader;
		#endregion

		#region Kernels
		int render;
		int apply_step;
		int init_values;
		#endregion

		public ParticleGpu(Vector3Int resolution)
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
			Oxygens.SendTo(handle, shader);
			Mass.SendTo(handle, shader);
			IsBlocked.SendTo(handle, shader);
		}

		void SetupData(Vector3Int resolution)
		{
			ResolutionX = resolution.x;

			ParticlesPerPoint = 128;
			Oxygens = new DataBuffer<Particle>(nameof(Oxygens), new Vector3Int(resolution.x, resolution.y * ParticlesPerPoint, 1));
			Mass = new DataBuffer<int>(nameof(Mass), resolution);
			IsBlocked = new DataBuffer<int>(nameof(IsBlocked), resolution);


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
				var center = new Vector2Int(resolution.x / 2, resolution.y / 2);
				var p = resolution.x * resolution.x / 2;

				Mass.SendUpdatesToGpu();


				for (int x = 0; x < resolution.x; x++)
					for (int y = 0; y < resolution.y * ParticlesPerPoint; y++)
					{
						Particle part;
						part.pos.x = Random.Range(-1f, 1f) + center.x;
						part.pos.y = Random.Range(-1f, 1f) + center.y;
						Vector2 randomVector = new Vector2(Random.Range(-3f, 3f), Random.Range(-3f, 3f));
						randomVector = randomVector.normalized;
						part.vel.x = randomVector.x * Random.Range(0.1f, 0.2f);
						part.vel.y = randomVector.y * Random.Range(0.1f, 0.2f);

						Oxygens.AddDelta(new Vector2Int(x, y), part);
					}
				Oxygens.SendUpdatesToGpu();
			}
		}

		void SetupShaders(Vector3Int resolution)
		{
			// shader
			shader = Resources.Load<ComputeShader>("particle");

			// globals
			{
				shader.SetInt(nameof(ParticlesPerPoint), ParticlesPerPoint);
				shader.SetInt(nameof(ResolutionX), ResolutionX);
				shader.SetFloat(nameof(MaxPressureRender), MaxPressureRender);
				shader.SetFloat(nameof(MaxDeltaRender), MaxDeltaRender);
			}
			// apply_step
			{
				apply_step = shader.FindKernel(nameof(apply_step));
				SendAll(apply_step);
			}
			// render
			{
				render = shader.FindKernel(nameof(render));

				SendAll(render);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
				shader.SetTexture(render, nameof(VelocityMap), VelocityMap);
				shader.SetTexture(render, nameof(FakeMap), FakeMap);
			}
			// init
			{
				init_values = shader.FindKernel(nameof(init_values));
				SendAll(init_values);
			}
		}

		public void Init()
		{
			Run(init_values);
		}

		public void Tick()
		{
			Run(apply_step);
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
