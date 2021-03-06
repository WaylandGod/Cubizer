﻿using System;

using UnityEngine;

namespace Cubizer.Live
{
	[Serializable]
	public sealed class LiveManagerComponent : CubizerComponent<LiveManagerModels>
	{
		private readonly string _name;
		private GameObject _liveObject;

		public override bool active
		{
			get
			{
				return model.enabled;
			}
			set
			{
				if (model.enabled != value)
				{
					if (value)
						this.OnEnable();
					else
						this.OnDisable();

					model.enabled = value;
				}
			}
		}

		public LiveManagerComponent(string name = "ServerEntities")
		{
			_name = name;
		}

		public override void OnEnable()
		{
			_liveObject = new GameObject(_name);
			foreach (var it in model.settings.lives)
			{
				if (it == null)
					throw new NullReferenceException("Missing of index:" + model.settings.lives.IndexOf(null));

				var gameObject = GameObject.Instantiate(it.gameObject);
				gameObject.name = it.name;
				gameObject.transform.parent = _liveObject.transform;
				gameObject.GetComponent<LiveBehaviour>().material = context.materialFactory.CreateMaterial(it.name, it.settings);
			}
		}

		public override void OnDisable()
		{
			if (_liveObject != null)
			{
				GameObject.Destroy(_liveObject);
				_liveObject = null;
			}
		}
	}
}