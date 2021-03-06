﻿using System;
using System.Collections.Generic;

using UnityEngine;

namespace Cubizer
{
	[Serializable]
	public class LiveManagerModels : CubizerModel
	{
		[Serializable]
		public struct LiveSettings
		{
			[SerializeField]
			public List<ILiveBehaviour> lives;

			public static LiveSettings defaultSettings
			{
				get
				{
					return new LiveSettings
					{
						lives = new List<ILiveBehaviour>()
					};
				}
			}
		}

		[SerializeField]
		private LiveSettings _settings = LiveSettings.defaultSettings;

		public LiveSettings settings
		{
			get { return _settings; }
			set { _settings = value; }
		}

		public override void Reset()
		{
			_settings = LiveSettings.defaultSettings;
		}
	}
}