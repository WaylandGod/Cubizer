﻿using System.Collections.Generic;

using UnityEngine;

namespace Cubizer
{
	public class ChunkData : IChunkData
	{
		private ChunkPrimer _chunk;
		private IChunkDataManager _chunkManager;

		public override IChunkDataManager chunkManager
		{
			get
			{
				return _chunkManager;
			}
		}

		public override ChunkPrimer chunk
		{
			get
			{
				return _chunk;
			}
			set
			{
				Debug.Assert(_chunk != value);
				Debug.Assert(_chunkManager != null);

				if (_chunk != null)
					_chunk.onChunkChange -= OnBuildChunk;

				_chunk = value;

				if (_chunk != null)
					_chunk.onChunkChange += OnBuildChunk;
			}
		}

		public void Start()
		{
			this.OnBuildChunk();
		}

		public void OnDestroy()
		{
			if (_chunkManager != null)
				_chunkManager.Set(_chunk.position.x, _chunk.position.y, _chunk.position.z, null);
		}

		public void OnDrawGizmos()
		{
			if (chunk == null)
				return;

			if (chunk.voxels != null || chunk.voxels.count > 0)
			{
				var bound = chunk.voxels.bound;

				Vector3 pos = transform.position;
				pos.x += (bound.x - 1) * 0.5f;
				pos.y += (bound.y - 1) * 0.5f;
				pos.z += (bound.z - 1) * 0.5f;

				Gizmos.color = Color.black;
				Gizmos.DrawWireCube(pos, new Vector3(bound.x, bound.y, bound.z));
			}
		}

		public override void OnBuildChunk()
		{
			for (int i = 0; i < transform.childCount; i++)
				Destroy(transform.GetChild(i).gameObject);

			if (_chunk == null || _chunk.voxels.count == 0)
				return;

			var model = _chunk.CreateVoxelModel(VoxelCullMode.Culled);
			if (model != null)
			{
				var entities = new Dictionary<int, int>();
				if (model.CalcFaceCountAsAllocate(ref entities) == 0)
					return;

				foreach (var it in entities)
				{
					var material = VoxelMaterialManager.GetInstance().GetMaterial(it.Key);
					if (material == null)
						continue;

					var controller = material.userdata as ILiveBehaviour;
					if (controller == null)
						continue;

					controller.OnBuildChunk(this, model, it.Value);
				}
			}
		}

		public void Init(ChunkPrimer chunk, IChunkDataManager chunkManager)
		{
			Debug.Assert(_chunk == null && _chunkManager == null);

			_chunk = chunk;
			_chunk.onChunkChange += OnBuildChunk;

			_chunkManager = chunkManager;
		}
	}
}