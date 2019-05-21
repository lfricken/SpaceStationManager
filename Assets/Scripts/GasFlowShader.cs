using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="ImplementingClass"></typeparam>
	interface IApplyDelta<ImplementingClass>
	{
		/// <summary>
		/// Adds the other variables to this instance.
		/// </summary>
		void ApplyDelta(ImplementingClass other);
	}

	struct PressureTile : IApplyDelta<PressureTile>
	{
		public float pressure;
		private int blocked;

		public bool Blocked
		{
			get { return blocked == 1; }
			set { blocked = value ? 1 : 0; }
		}

		/// <summary>
		/// Adds the other variables to this instance.
		/// </summary>
		public void ApplyDelta(PressureTile other)
		{
			pressure += other.pressure;
			Blocked = other.Blocked;
		}
	}

	class DataBuffer<Data> where Data : IApplyDelta<Data>
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
			if (modifications.ContainsKey(position))
			{
				modifications[position].ApplyDelta(data);
			}
			else
			{
				modifications[position] = data;
			}
		}

		public void SendUpdatesToGpu()
		{
			gpuBuffer.GetData(cpuData);
			foreach (var kvp in modifications)
			{
				cpuData[index(kvp.Key)].ApplyDelta(kvp.Value);
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

		DataBuffer<PressureTile> tiles;
		DataBuffer<PressureTile> writeTiles;

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;

		ComputeShader shader;
		int forces;
		int diffuse;
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

		public void Block(Vector2Int start, Vector2Int end)
		{
			PressureTile tile = new PressureTile();
			tile.Blocked = true;
			tile.pressure = 0;
			for (int x = start.x; x <= end.x; x++)
			{
				for (int y = start.y; y <= end.y; y++)
				{
					tiles.AddDelta(new Vector2Int(x, y), tile);
				}
			}

			tiles.SendUpdatesToGpu();
		}

		void SetupShaders(Vector3Int resolution)
		{
			tiles = new DataBuffer<PressureTile>("PressureTiles", resolution);
			writeTiles = new DataBuffer<PressureTile>("PressureTilesWrite", resolution);
			PressureTile tile = new PressureTile();

			tile.Blocked = true;
			tile.pressure = 0;

			for (int x = 0; x < resolution.x; x++)
			{
				tiles.AddDelta(new Vector2Int(x, 0), tile);
				tiles.AddDelta(new Vector2Int(x, resolution.y - 1), tile);
			}

			for (int y = 0; y < resolution.y; y++)
			{
				tiles.AddDelta(new Vector2Int(0, y), tile);
				tiles.AddDelta(new Vector2Int(resolution.x - 1, y), tile);
			}

			Block(new Vector2Int(10, 16), new Vector2Int(20, 16));

			tile.Blocked = false;
			tile.pressure = 724;
			tiles.AddDelta(new Vector2Int(14, 14), tile);
			tiles.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// forces
			{
				forces = shader.FindKernel(nameof(forces));
				tiles.SendTo(forces, shader);
			}

			// disperse
			{
				diffuse = shader.FindKernel(nameof(diffuse));
				tiles.SendTo(diffuse, shader);
				writeTiles.SendTo(diffuse, shader);
			}

			// render
			{
				render = shader.FindKernel(nameof(render));
				tiles.SendTo(render, shader);
				writeTiles.SendTo(render, shader);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
			}
		}

		public void Tick()
		{
			int threadGroups = Resolution.x / numXYThreads;

			//shader.Dispatch(forces, threadGroups, threadGroups, 1);
			shader.Dispatch(diffuse, threadGroups, threadGroups, 1);
			shader.Dispatch(render, threadGroups, threadGroups, 1);
		}
	}
}
