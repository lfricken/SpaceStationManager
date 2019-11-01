﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
	public class OverlayManager : SingletonMonoBehavior<OverlayManager>
	{
		public float OverlayAlpha = 0.9f;
		private List<Image> overlays;
		private MapOverlay Active;
		private Vector2Int size;

		public enum MapOverlay
		{
			GasPressure = 0,
			GasVelocity = 1,
			None,
		}

		OverlayManager()
		{
			size = new Vector2Int(10,10);
			overlays = new List<Image>();
		}

		private void Start()
		{
			var overlayParent = GameObject.Find($"{nameof(overlays)}");
			var gfm = GasflowManager.Instance;

			int num = 0;
			foreach (Transform child in overlayParent.transform)
			{
				GameObject go = child.gameObject;

				Image i = go.GetComponent<Image>();
				i.canvas.pixelPerfect = true;
				overlays.Add(i);
				i.material.mainTexture = gfm.gas.RenderTextures[num];
				i.color = new Color(1, 1, 1, 0.5f);

				RectTransform t = go.GetComponent<RectTransform>();

				t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, gfm.Resolution.x * size.x / i.material.mainTexture.width);
				t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gfm.Resolution.y * size.y / i.material.mainTexture.height);
				++num;
			}

			SetMapOverlay(MapOverlay.GasPressure);
		}

		private void Clear()
		{
			foreach (Image i in overlays)
			{
				i.color = new Color(0, 0, 0, 0);
			}
		}

		public void SetMapOverlay(MapOverlay type)
		{
			Clear();

			if (type == Active)
			{
				Active = MapOverlay.None;
			}
			else
			{
				Active = type;
				if (MapOverlay.GasPressure == type)
					overlays[(int)MapOverlay.GasPressure].color = new Color(1, 1, 1, OverlayAlpha);
				if (MapOverlay.GasVelocity == type)
					overlays[(int)MapOverlay.GasVelocity].color = new Color(1, 1, 1, OverlayAlpha);
			}

		}

		void Update()
		{
			if (Input.GetButtonDown("ViewGasPressure"))
				SetMapOverlay(MapOverlay.GasPressure);
			if (Input.GetButtonDown("ViewGasVelocity"))
				SetMapOverlay(MapOverlay.GasVelocity);
		}
	}
}
