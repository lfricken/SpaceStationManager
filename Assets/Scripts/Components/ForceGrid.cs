using UnityEngine;

namespace Game
{
	public class ForceGrid : GameMonoBehaviour
	{
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
