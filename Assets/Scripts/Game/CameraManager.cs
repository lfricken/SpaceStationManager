using UnityEngine;
using UnityEngine.U2D;

namespace SSM
{
	public class CameraManager : SingletonMonoBehavior<CameraManager>
	{
		void Start()
		{
			WindowManager.Instance.OnScreenSizeChange += UpdatePixelPerfectCamera;
		}

		private void UpdatePixelPerfectCamera(Vector2Int size)
		{
			bool modified = false;
			if (size.x % 2 == 1)
			{
				modified = true;
				size.x += 1;
			}
			if (size.y % 2 == 1)
			{
				modified = true;
				size.y += 1;
			}

			var c = GameObject.Find("camera").GetComponent<PixelPerfectCamera>();
			c.refResolutionX = size.x;
			c.refResolutionY = size.y;

			if(modified)
				Screen.SetResolution(size.x, size.y, false);
		}
	}
}