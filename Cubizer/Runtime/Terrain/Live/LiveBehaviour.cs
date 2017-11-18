﻿using UnityEngine;

namespace Cubizer
{
	[DisallowMultipleComponent]
	public abstract class LiveBehaviour : LiveBehaviourBase
	{
		[SerializeField]
		public VoxelMaterialParams settings;

		private VoxelMaterial _material;

		public VoxelMaterial material
		{
			set
			{
				if (value != null)
					value.userdata = this;
				_material = value;
			}
			get
			{
				return _material;
			}
		}
	}
}