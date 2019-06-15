using UnityEngine;

namespace Game
{
	public class Builder : GameMonoBehaviour
	{
		[SerializeField]
		private GameObject[] placeableObjectPrefabs;

		[SerializeField]
		private GameObject BlueprintObject;

		private GameObject currentPlaceableObject;

		private float mouseWheelRotation;
		private int currentPrefabIndex = -1;

		private void Update()
		{
			HandleNewObjectHotkey();

			if (currentPlaceableObject != null)
			{
				MoveCurrentObjectToMouse();
				RotateFromMouseWheel();
				ReleaseIfClicked();
			}
		}

		private void HandleNewObjectHotkey()
		{
			for (int i = 0; i < placeableObjectPrefabs.Length; i++)
			{
				if (Input.GetKeyDown(KeyCode.Alpha0 + 1 + i))
				{
					if (PressedKeyOfCurrentPrefab(i))
					{
						Destroy(currentPlaceableObject);
						currentPrefabIndex = -1;
					}
					else
					{
						if (currentPlaceableObject != null)
						{
							Destroy(currentPlaceableObject);
						}

						currentPlaceableObject = Instantiate(placeableObjectPrefabs[i]);
						currentPrefabIndex = i;
					}

					break;
				}
			}
		}

		private bool PressedKeyOfCurrentPrefab(int i)
		{
			return currentPlaceableObject != null && currentPrefabIndex == i;
		}

		private void MoveCurrentObjectToMouse()
		{
			Vector3 pz = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			pz.z = 0;
			currentPlaceableObject.transform.position = pz;
		}

		private void RotateFromMouseWheel()
		{
			Debug.Log(Input.mouseScrollDelta);
			mouseWheelRotation += Input.mouseScrollDelta.y;
			currentPlaceableObject.transform.Rotate(Vector3.up, mouseWheelRotation * 10f);
		}

		private void ReleaseIfClicked()
		{
			if (Input.GetMouseButtonDown(0))
			{
				// actually instantiate it here
				currentPlaceableObject = null;
			}
		}
	}

}
