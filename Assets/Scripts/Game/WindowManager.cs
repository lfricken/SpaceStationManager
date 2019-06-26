using UnityEngine;

namespace Game
{
	public class WindowManager : SingletonMonoBehavior<WindowManager>
	{
		private Vector2Int lastScreenSize;
		public delegate void ScreenSizeChangeEventHandler(Vector2Int size);
		public event ScreenSizeChangeEventHandler OnScreenSizeChange;

		void Start()
		{
			//Screen.fullScreen = true;
		}

		void Update()
		{
			var size = new Vector2Int(Screen.width, Screen.height);

			if (lastScreenSize != size)
			{
				lastScreenSize = size;
				OnScreenSizeChange?.Invoke(size);
			}

			//if (!Screen.fullScreen && Input.GetKeyDown(KeyCode.LeftCommand) && Input.GetKeyDown(KeyCode.UpArrow))
			//{
			//	Screen.fullScreen = true;
			//}
			if (Input.GetKey(KeyCode.LeftWindows) && Input.GetKey(KeyCode.DownArrow))
			{
				Screen.fullScreen = false;
			}
		}
	}
}
