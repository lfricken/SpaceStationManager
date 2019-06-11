using UnityEngine;

namespace Game
{
	/// <summary>
	/// Controls how gas flows.
	/// </summary>
	public class GasComponent : GameMonoBehaviour
	{
		/// <summary>
		/// Force this object applies to the surrounding gas.
		/// </summary>
		public Vector2 ForceApplication;

		/// <summary>
		/// True if this should block gas flow.
		/// </summary>
		public bool BlocksGasFlow;

		/// <summary>
		/// Adds or subtracts this much mass per second to this cell.
		/// </summary>
		public float DeltaMass;
		
		private void Start()
		{

		}
	}
}
