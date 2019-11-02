using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public class GasflowManager : SingletonMonoBehavior<GasflowManager>
	{
		public List<GasFlowGpu> GasWorlds;

		public Vector3Int Resolution
		{
			get;
			private set;
		}

		private void Start()
		{
			GasWorlds = new List<GasFlowGpu>();

			for (int i = 0; i < 1; ++i)
			{
				Resolution = new Vector3Int(256, 256, 32);
				ComputeShader shader = Instantiate(Resources.Load<ComputeShader>("gas"));
				var gas = new GasFlowGpu(Resolution, shader);
				gas.AddRemoveMass.AddDelta(new Vector2Int(100, 100), 10);
				gas.AddRemoveMass.SendUpdatesToGpu();
				GasWorlds.Add(gas);
			}
			StartCoroutine(Tick());
		}

		private void Update()
		{

		}

		private void OnDestroy()
		{
			foreach (var gas in GasWorlds)
			{
				gas.Dispose();
			}
		}

		IEnumerator Tick()
		{
			int mod = GasWorlds.Count;
			int simsPerTick = 1;
			int i = 0;


			Debug.Assert(mod >= simsPerTick);
			//gas.Init();
			while (true)
			{
				int nextGoal = (i + simsPerTick) % mod;
				do
				{
					var gas = GasWorlds[i];
					gas.Tick();
					i = (i + 1) % mod;
				} while (i != nextGoal);

				yield return new WaitForSeconds(0f);
			}
		}
	}
}
