using UnityEngine;

namespace Game
{
	public class GameMonoBehaviour : MonoBehaviour
	{
		public Vector2 Position
		{
			get
			{
				return gameObject.transform.position;
			}
			set
			{
				gameObject.transform.position = new Vector3(value.x, value.y, 0);
			}
		}
	}
}
