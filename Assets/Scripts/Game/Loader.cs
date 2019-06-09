using UnityEngine;

namespace SSM
{
	public class Loader : MonoBehaviour
	{
		void Awake()
		{
			gameObject.AddComponent<GameManager>();
			gameObject.AddComponent<WindowManager>();
			gameObject.AddComponent<CameraManager>();
		}
	}
}
