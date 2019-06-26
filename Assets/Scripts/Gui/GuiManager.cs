using UnityEngine;

namespace Game
{
	public class GuiManager : GameMonoBehaviour
	{
		public Rect windowRect = new Rect(0, 0, 600, 200);

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
