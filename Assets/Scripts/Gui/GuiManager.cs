using UnityEngine;

namespace Game
{
	public class GuiManager : GameMonoBehaviour
	{
		public Vector2Int SubSize;

		public Rect windowRect = new Rect(0, 0, 600, 200);

		void Start()
		{
			SubSize = new Vector2Int(200, 200);
			WindowManager.Instance.OnScreenSizeChange += OnScreenSizeChange;
		}

		private void OnScreenSizeChange(Vector2Int size)
		{
			windowRect = new Rect(0, size.y - SubSize.y, SubSize.x, SubSize.y);
		}


		void OnGUI()
		{
			// Register the window. Notice the 3rd parameter
			windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
		}

		// Make the contents of the window
		void DoMyWindow(int windowID)
		{
			if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
			{
				print("Got a click");
			}
		}
	}
}
