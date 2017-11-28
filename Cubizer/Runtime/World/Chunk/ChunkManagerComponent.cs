﻿using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace Cubizer
{
	using ThreadTask = ThreadTask<ChunkThreadData>;

	public class ChunkManagerComponent : CubizerComponent<ChunkManagerModels>
	{
		private string _name;
		private GameObject _chunkObject;
		private ChunkDelegates _callbacks;

		private ThreadTask[] _threads;

		private bool _active;

		public override bool active
		{
			get
			{
				return _active;
			}
			set
			{
				if (_active != value)
				{
					if (value)
						this.OnEnable();
					else
						this.OnDisable();

					_active = value;
				}
			}
		}

		public int count
		{
			get { return _chunkObject != null ? _chunkObject.transform.childCount : 0; }
		}

		public ChunkDelegates callbacks
		{
			get { return _callbacks; }
		}

		private IChunkDataManager manager
		{
			get { return model.settings.chunkManager; }
		}

		public ChunkManagerComponent(string name = "ServerChunks")
		{
			_name = name;
			_active = true;
			_callbacks = new ChunkDelegates();
		}

		public override void OnEnable()
		{
			Debug.Assert(model.settings.chunkManager != null);

			_chunkObject = new GameObject(_name);

			_threads = new ThreadTask[model.settings.chunkThreadNums];

			for (int i = 0; i < _threads.Length; i++)
			{
				_threads[i] = new ThreadTask(this.CreateChunkPrimer);
				_threads[i].Start();
			}
		}

		public override void OnDisable()
		{
			for (int i = 0; i < _threads.Length; i++)
			{
				_threads[i].Dispose();
				_threads[i] = null;
			}

			if (_chunkObject != null)
			{
				this.DestroyChunks();
				GameObject.Destroy(_chunkObject);
				_chunkObject = null;
			}
		}

		public ChunkPrimer FindChunk(int x, int y, int z)
		{
			ChunkPrimer chunk;
			if (manager.Get(x, y, z, out chunk))
				return chunk;
			return null;
		}

		public short CalculateChunkPosByWorld(float x)
		{
			return (short)Mathf.FloorToInt(x / model.settings.chunkSize);
		}

		private bool HitTestByRay(Ray ray, int hitDistance, out ChunkPrimer chunk, out byte outX, out byte outY, out byte outZ, out ChunkPrimer lastChunk, out byte lastX, out byte lastY, out byte lastZ)
		{
			var chunkX = CalculateChunkPosByWorld(ray.origin.x);
			var chunkY = CalculateChunkPosByWorld(ray.origin.y);
			var chunkZ = CalculateChunkPosByWorld(ray.origin.z);

			lastChunk = null;
			lastX = lastY = lastZ = outX = outY = outZ = 255;

			if (!this.manager.Get(chunkX, chunkY, chunkZ, out chunk))
				return false;

			Vector3 origin = ray.origin;
			origin.x -= chunk.position.x * model.settings.chunkSize;
			origin.y -= chunk.position.y * model.settings.chunkSize;
			origin.z -= chunk.position.z * model.settings.chunkSize;

			VoxelMaterial block = null;

			for (int i = 0; i < hitDistance && block == null; i++)
			{
				int ix = Mathf.RoundToInt(origin.x);
				int iy = Mathf.RoundToInt(origin.y);
				int iz = Mathf.RoundToInt(origin.z);

				if (outX == ix && outY == iy && outZ == iz)
					continue;

				bool isOutOfChunk = false;
				if (ix < 0) { ix = ix + model.settings.chunkSize; origin.x += model.settings.chunkSize; chunkX--; isOutOfChunk = true; }
				if (iy < 0) { iy = iy + model.settings.chunkSize; origin.y += model.settings.chunkSize; chunkY--; isOutOfChunk = true; }
				if (iz < 0) { iz = iz + model.settings.chunkSize; origin.z += model.settings.chunkSize; chunkZ--; isOutOfChunk = true; }
				if (ix + 1 > model.settings.chunkSize) { ix = ix - model.settings.chunkSize; origin.x -= model.settings.chunkSize; chunkX++; isOutOfChunk = true; }
				if (iy + 1 > model.settings.chunkSize) { iy = iy - model.settings.chunkSize; origin.y -= model.settings.chunkSize; chunkY++; isOutOfChunk = true; }
				if (iz + 1 > model.settings.chunkSize) { iz = iz - model.settings.chunkSize; origin.z -= model.settings.chunkSize; chunkZ++; isOutOfChunk = true; }

				lastX = outX;
				lastY = outY;
				lastZ = outZ;
				lastChunk = chunk;

				if (isOutOfChunk)
				{
					chunk = FindChunk(chunkX, chunkY, chunkZ);
					if (chunk == null)
						return false;
				}

				chunk.voxels.Get(ix, iy, iz, ref block);

				origin += ray.direction;

				outX = (byte)ix;
				outY = (byte)iy;
				outZ = (byte)iz;
			}

			return block != null;
		}

		public bool HitTestByRay(Ray ray, int hitDistance, out ChunkPrimer chunk, out byte outX, out byte outY, out byte outZ)
		{
			byte lx, ly, lz;
			ChunkPrimer chunkLast = null;

			return this.HitTestByRay(ray, hitDistance, out chunk, out outX, out outY, out outZ, out chunkLast, out lx, out ly, out lz);
		}

		public bool HitTestByScreenPos(Vector3 pos, int hitDistance, out ChunkPrimer chunk, out byte outX, out byte outY, out byte outZ, ref ChunkPrimer lastChunk, out byte lastX, out byte lastY, out byte lastZ)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray.origin = Camera.main.transform.position;
			return this.HitTestByRay(ray, hitDistance, out chunk, out outX, out outY, out outZ, out lastChunk, out lastX, out lastY, out lastZ);
		}

		public bool HitTestByScreenPos(Vector3 pos, int hitDistance, out ChunkPrimer chunk, out byte outX, out byte outY, out byte outZ)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray.origin = Camera.main.transform.position;
			return this.HitTestByRay(ray, hitDistance, out chunk, out outX, out outY, out outZ);
		}

		public bool AddBlockByRay(Ray ray, int hitDistance, VoxelMaterial block)
		{
			Debug.Assert(block != null);

			byte x, y, z, lx, ly, lz;
			ChunkPrimer chunkNow = null;
			ChunkPrimer chunkLast = null;

			if (HitTestByRay(ray, hitDistance, out chunkNow, out x, out y, out z, out chunkLast, out lx, out ly, out lz))
			{
				var chunk = chunkLast != null ? chunkLast : chunkNow;

				if (_callbacks.OnAddBlockBefore != null)
					_callbacks.OnAddBlockBefore(chunk, lx, ly, lz, block);

				chunk.voxels.Set(lx, ly, lz, block);
				chunk.dirty = true;

				if (_callbacks.OnAddBlockAfter != null)
					_callbacks.OnAddBlockAfter(chunk, lx, ly, lz, block);

				return true;
			}

			return false;
		}

		public bool AddBlockByScreenPos(Vector3 pos, int hitDistance, VoxelMaterial block)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray.origin = Camera.main.transform.position;
			return this.AddBlockByRay(ray, hitDistance, block);
		}

		public bool RemoveBlockByRay(Ray ray, int hitDistance)
		{
			byte x, y, z;
			ChunkPrimer chunk;

			if (HitTestByRay(ray, hitDistance, out chunk, out x, out y, out z))
			{
				if (_callbacks.OnRemoveBlockBefore != null)
					_callbacks.OnRemoveBlockBefore(chunk, x, y, z, null);

				chunk.voxels.Set(x, y, z, null);
				chunk.dirty = true;

				if (_callbacks.OnRemoveBlockAfter != null)
					_callbacks.OnRemoveBlockAfter(chunk, x, y, z, null);

				return true;
			}

			return false;
		}

		public bool RemoveBlockByScreenPos(Vector3 pos, int hitDistance)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			ray.origin = Camera.main.transform.position;
			return this.RemoveBlockByRay(ray, hitDistance);
		}

		public bool Load(string path, bool is_save = true)
		{
			Debug.Assert(path != null);

			if (this.manager.Load(path))
			{
				DestroyChunks(is_save);

				foreach (ChunkPrimer chunk in this.manager.GetEnumerator())
				{
					var gameObject = new GameObject("Chunk");
					gameObject.transform.parent = _chunkObject.transform;
					gameObject.transform.position = new Vector3(chunk.position.x, chunk.position.y, chunk.position.z) * model.settings.chunkSize;
					gameObject.AddComponent<ChunkData>().Init(chunk);
				}

				return true;
			}

			return false;
		}

		public bool Save(string path)
		{
			Debug.Assert(path != null);

			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				var serializer = new BinaryFormatter();
				serializer.Serialize(stream, this.manager);

				return true;
			}
		}

		private void CreateChunkObject(IPlayerListener player, ChunkPrimer chunk, int x, int y, int z)
		{
			this.manager.Set(x, y, z, chunk);

			var gameObject = new GameObject("Chunk");
			gameObject.transform.parent = _chunkObject.transform;
			gameObject.transform.position = new Vector3(x, y, z) * context.profile.chunk.settings.chunkSize;
			gameObject.AddComponent<ChunkData>().Init(chunk);
		}

		private void CreateChunkPrimer(ref ChunkThreadData data)
		{
			ChunkPrimer chunk = null;
			if (_callbacks.OnLoadChunkBefore != null)
				_callbacks.OnLoadChunkBefore(data.x, data.y, data.z, ref chunk);

			if (chunk == null)
				chunk = data.biome.OnBuildChunk(this.context.behaviour, data.x, data.y, data.z);

			if (chunk != null)
				data.chunk = chunk;

			if (_callbacks.OnLoadChunkAfter != null)
				_callbacks.OnLoadChunkAfter(data.chunk);
		}

		private void CreateChunk(IPlayerListener player, int x, int y, int z, ThreadTask thread = null)
		{
			if (this.count > model.settings.chunkNumLimits)
				return;

			IBiomeData biomeData = context.behaviour.biomeManager.buildBiomeIfNotExist(x, y, z);
			if (biomeData != null)
			{
				var threadData = new ChunkThreadData(player, biomeData, x, y, z);
				if (thread != null)
				{
					threadData.chunk = new ChunkPrimer(model.settings.chunkSize, x, y, z);
					this.manager.Set(x, y, z, threadData.chunk);
					thread.Task(threadData);
				}
				else
				{
					CreateChunkPrimer(ref threadData);

					if (threadData.chunk != null)
						CreateChunkObject(player, threadData.chunk, x, y, z);
				}
			}
		}

		private void DestroyChunks(bool noticeAll = true)
		{
			Debug.Assert(_chunkObject != null);

			var transform = _chunkObject.transform;

			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i).gameObject;

				if (noticeAll)
				{
					var chunk = child.GetComponent<ChunkData>();

					if (_callbacks.OnDestroyChunk != null)
						_callbacks.OnDestroyChunk(chunk.chunk);
				}

				GameObject.Destroy(child);
			}
		}

		private void DestroyChunk(Vector3 point, float radius)
		{
			Debug.Assert(_chunkObject != null);

			int x = CalculateChunkPosByWorld(point.x);
			int y = CalculateChunkPosByWorld(point.y);
			int z = CalculateChunkPosByWorld(point.z);
			var chunkPos = new Vector3(x, y, z) * model.settings.chunkSize;

			var transform = _chunkObject.transform;
			var maxRadius = radius * model.settings.chunkSize;

			for (int i = 0; i < transform.childCount; i++)
			{
				var transformChild = transform.GetChild(i);

				var distance = Vector3.Distance(transformChild.position, chunkPos);
				if (distance > maxRadius)
				{
					var chunk = transformChild.GetComponent<ChunkData>();

					if (_callbacks.OnDestroyChunk != null)
						_callbacks.OnDestroyChunk(chunk.chunk);

					this.manager.Set(chunk.chunk.position.x, chunk.chunk.position.y, chunk.chunk.position.z, null);

					GameObject.Destroy(transformChild.gameObject);
					break;
				}
			}
		}

		private void DestroyChunk(int x, int y, int z)
		{
			Debug.Assert(_chunkObject != null);

			var transform = _chunkObject.transform;

			for (int i = 0; i < transform.childCount; i++)
			{
				var transformChild = transform.GetChild(i);
				if (transformChild.position.x == x &&
					transformChild.position.y == y &&
					transformChild.position.z == z)
				{
					var chunk = transformChild.GetComponent<ChunkData>();

					if (_callbacks.OnDestroyChunk != null)
						_callbacks.OnDestroyChunk(chunk.chunk);

					GameObject.Destroy(transformChild.gameObject);

					this.manager.Set(x, y, z, null);

					break;
				}
			}
		}

		private void UpdatePlayer(IPlayerListener player, ThreadTask thread = null)
		{
			var chunkX = CalculateChunkPosByWorld(player.player.transform.position.x);
			var chunkY = CalculateChunkPosByWorld(player.player.transform.position.y);
			var chunkZ = CalculateChunkPosByWorld(player.player.transform.position.z);

			var radius = model.settings.chunkUpdateRadius;
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dz = -radius; dz <= radius; dz++)
				{
					var x = chunkX + dx;
					var y = chunkY;
					var z = chunkZ + dz;

					var chunk = FindChunk(x, y, z);
					if (chunk != null)
					{
						if (!chunk.dirty)
							chunk.OnUpdate();
						else
						{
							chunk.OnChunkChange();
							chunk.dirty = false;
						}
					}
					else
					{
						CreateChunk(player, x, y, z);
					}
				}
			}
		}

		private bool UpdateCamera(IPlayerListener player, Vector3 translate, Plane[] planes, Vector2Int[] radius, ThreadTask thread = null)
		{
			int x = CalculateChunkPosByWorld(translate.x);
			int y = CalculateChunkPosByWorld(translate.y);
			int z = CalculateChunkPosByWorld(translate.z);

			int bestX = 0, bestY = 0, bestZ = 0;
			int bestScore = int.MaxValue;

			int start = bestScore;

			var chunkOffset = (Vector3.one * (model.settings.chunkSize - 1)) * 0.5f;

			for (int iy = radius[1].x; iy <= radius[1].y; iy++)
			{
				int dy = y + iy;

				if (dy < model.settings.chunkHeightLimitLow || dy > model.settings.chunkHeightLimitHigh)
					continue;

				for (int ix = radius[0].x; ix <= radius[0].y; ix++)
				{
					int dx = x + ix;

					for (int iz = radius[2].x; iz <= radius[2].y; iz++)
					{
						int dz = z + iz;

						var hit = FindChunk(dx, dy, dz);
						if (hit != null)
							continue;

						var p = chunkOffset + new Vector3(dx, dy, dz) * model.settings.chunkSize;

						int invisiable = GeometryUtility.TestPlanesAABB(planes, new Bounds(p, Vector3.one * model.settings.chunkSize)) ? 0 : 1;
						int distance = Mathf.Max(Mathf.Max(Mathf.Abs(ix), Mathf.Abs(iy)), Mathf.Abs(iz));
						int score = (invisiable << 24) | distance;

						if (score < bestScore)
						{
							bestScore = score;
							bestX = dx;
							bestY = dy;
							bestZ = dz;
						}
					}
				}
			}

			if (start == bestScore)
				return false;

			this.CreateChunk(player, bestX, bestY, bestZ, thread);
			return true;
		}

		private int FetchResults()
		{
			int idles = 0;

			foreach (var work in _threads)
			{
				if (work.state == ThreadTaskState.Quit)
					throw new System.Exception("this thread has a pending exception or has been stopped before starting");

				if (work.state == ThreadTaskState.Idle)
					idles++;

				if (work.state != ThreadTaskState.Done)
					continue;

				try
				{
					var data = work.context;
					if (data != null && data.chunk != null)
					{
						CreateChunkObject(work.context.player, data.chunk, data.x, data.y, data.z);
					}

					idles++;
				}
				finally
				{
					work.state = ThreadTaskState.Idle;
				}
			}

			return idles;
		}

		private void AutoGC()
		{
			if (this.manager.Count() < model.settings.chunkNumLimits)
				return;

			this.manager.GC();
			context.behaviour.biomeManager.biomes.GC();
		}

		public override void Update()
		{
			this.AutoGC();

			var players = context.players.settings.players;
			foreach (var it in players)
				DestroyChunk(it.player.transform.position, it.model.settings.chunkRadiusGC);

			foreach (var it in players)
				UpdatePlayer(it);

			var idleThreads = this.FetchResults();
			if (idleThreads > 0)
			{
				foreach (var it in players)
				{
					if (idleThreads == 0)
						break;

					var translate = it.player.transform.position;
					var planes = GeometryUtility.CalculateFrustumPlanes(it.player);

					foreach (var thread in _threads)
					{
						if (thread.state != ThreadTaskState.Idle)
							continue;

						if (UpdateCamera(it, translate, planes, it.model.settings.chunkRadius, thread))
							idleThreads--;
					}
				}
			}
		}
	}
}