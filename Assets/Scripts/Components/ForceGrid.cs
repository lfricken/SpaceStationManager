using UnityEngine;

namespace Game
{
	public class ForceGrid : GameMonoBehaviour
	{
		public static Vector3 ToGrid(Vector3 pos)
		{
			return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);

		}

		private void Start()
		{
			var worldPos = transform.position;
			transform.position = new Vector3(Mathf.Round(worldPos.x), Mathf.Round(worldPos.y), worldPos.z);

		}

		private void Update()
		{
		}
	}
}
