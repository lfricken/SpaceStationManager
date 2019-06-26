using UnityEngine;

namespace Game
{
	public class Loader : MonoBehaviour
	{
		void Awake()
		{
			gameObject.AddComponent<GameManager>();
			gameObject.AddComponent<WindowManager>();
			gameObject.AddComponent<CameraManager>();
			gameObject.AddComponent<GasflowManager>();
			gameObject.AddComponent<OverlayManager>();
			gameObject.AddComponent<GuiManager>();
		}
	}
}
