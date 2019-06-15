using System.Collections;
using UnityEngine;

namespace Game
{
	public class BlinkerTest : MonoBehaviour
	{
		public float frequency;

		SpriteRenderer rend;

		private void Start()
		{
			rend = gameObject.GetComponent<SpriteRenderer>();
			StartCoroutine(nameof(blink));
		}

		IEnumerator blink()
		{
			while (true)
			{
				yield return new WaitForSeconds(frequency);
				if (rend.color.a == 0)
					rend.color = Color.white;
				else
					rend.color = Color.white;

			}
		}
	}
}
