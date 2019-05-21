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
		Dictionary<Vector2Int, Data> modifications;
		readonly string BufferName;
		#endregion

		public DataBuffer(string bufferName, Vector3Int resolution)
		{
			modifications = new Dictionary<Vector2Int, Data>();
			BufferName = bufferName;

			cpuData = new Data[resolution.x * resolution.y];
			gpuBuffer = new ComputeBuffer(cpuData.Length, Marshal.SizeOf(typeof(Data)));
		}

		int index(Vector2Int position)
		{
			return position.x + position.y * 256;
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

		#region Shader
		public RenderTexture RenderTexture;
		/// <summary>
		/// If you update this, you need to update gas.compute!
		/// </summary>
		readonly int numXYThreads = 16;

		ComputeShader shader;
		int disperse;
		int render;
		#endregion

		public GasFlowGpu(Vector3Int resolution)
		{
			Resolution = resolution;

			RenderTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
			RenderTexture.enableRandomWrite = true;
			RenderTexture.Create();
			RenderTexture.filterMode = FilterMode.Point;

			SetupShaders(resolution);
		}

		void SetupShaders(Vector3Int resolution)
		{
			tiles = new DataBuffer<PressureTile>("PressureTiles", resolution);
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


			tiles.AddDelta(new Vector2Int(2, 3), tile);
			tiles.AddDelta(new Vector2Int(3, 3), tile);

			tile.Blocked = false;
			tile.pressure = 1000;
			tiles.AddDelta(new Vector2Int(1, 2), tile);
			tiles.AddDelta(new Vector2Int(8, 8), tile);
			tiles.SendUpdatesToGpu();

			// shader
			shader = Resources.Load<ComputeShader>("gas");

			// disperse
			{
				disperse = shader.FindKernel(nameof(disperse));
				tiles.SendTo(disperse, shader);
			}

			// render
			{
				render = shader.FindKernel(nameof(render));
				tiles.SendTo(render, shader);
				shader.SetTexture(render, nameof(RenderTexture), RenderTexture);
			}
		}

		public void Tick()
		{
			int threadGroups = Resolution.x / numXYThreads;

			shader.Dispatch(disperse, threadGroups, threadGroups, 1);
			shader.Dispatch(render, threadGroups, threadGroups, 1);
		}
	}
}
