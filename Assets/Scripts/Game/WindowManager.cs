using UnityEngine;

namespace SSM
{
	public class WindowManager : SingletonMonoBehavior<WindowManager>
	{
		private Vector2Int lastScreenSize;
		public delegate void ScreenSizeChangeEventHandler(Vector2Int size);
		public event ScreenSizeChangeEventHandler OnScreenSizeChange;

		void Update()
		{
			var size = new Vector2Int(Screen.width, Screen.height);

			if (lastScreenSize != size)
			{
				lastScreenSize = size;
				OnScreenSizeChange?.Invoke(size);
			}
		}
	}
}
