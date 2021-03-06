﻿using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Cubizer.Models
{
	public static class VoxFileImport
	{
		private static uint[] _paletteDefault = new uint[256]
		{
			0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
			0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
			0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
			0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
			0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
			0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
			0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
			0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
			0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
			0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
			0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
			0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
			0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
			0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
			0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
			0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
		};

		private static UnityEngine.Object _assetPrefab;

		public static VoxFileData Load(byte[] _data)
		{
			Debug.Assert(_data.Length > 0);

			using (MemoryStream stream = new MemoryStream(_data))
			{
				using (var reader = new BinaryReader(stream))
				{
					VoxFileData voxel = new VoxFileData();
					voxel.hdr.header = reader.ReadBytes(4);
					voxel.hdr.version = reader.ReadInt32();

					if (voxel.hdr.header[0] != 'V' || voxel.hdr.header[1] != 'O' || voxel.hdr.header[2] != 'X' || voxel.hdr.header[3] != ' ')
						throw new System.Exception("Bad Token: magic number is not VOX.");

					if (voxel.hdr.version != 150)
						throw new System.Exception("The version of file isn't 150 that version of vox, tihs version of file is " + voxel.hdr.version + ".");

					voxel.main.name = reader.ReadBytes(4);
					voxel.main.chunkContent = reader.ReadInt32();
					voxel.main.chunkNums = reader.ReadInt32();

					if (voxel.main.name[0] != 'M' || voxel.main.name[1] != 'A' || voxel.main.name[2] != 'I' || voxel.main.name[3] != 'N')
						throw new System.Exception("Bad Token: token is not MAIN.");

					if (voxel.main.chunkContent != 0)
						throw new System.Exception("Bad Token: chunk content is " + voxel.main.chunkContent + ", it should be 0.");

					if (reader.PeekChar() == 'P')
					{
						voxel.pack.name = reader.ReadBytes(4);
						if (voxel.pack.name[0] != 'P' || voxel.pack.name[1] != 'A' || voxel.pack.name[2] != 'C' || voxel.pack.name[3] != 'K')
							throw new System.Exception("Bad Token: token is not PACK");

						voxel.pack.chunkContent = reader.ReadInt32();
						voxel.pack.chunkNums = reader.ReadInt32();
						voxel.pack.modelNums = reader.ReadInt32();

						if (voxel.pack.modelNums == 0)
							throw new System.Exception("Bad Token: model nums must be greater than zero.");
					}
					else
					{
						voxel.pack.chunkContent = 0;
						voxel.pack.chunkNums = 0;
						voxel.pack.modelNums = 1;
					}

					voxel.chunkChild = new VoxFileChunkChild[voxel.pack.modelNums];

					for (int i = 0; i < voxel.pack.modelNums; i++)
					{
						var chunk = new VoxFileChunkChild();

						chunk.size.name = reader.ReadBytes(4);
						chunk.size.chunkContent = reader.ReadInt32();
						chunk.size.chunkNums = reader.ReadInt32();
						chunk.size.x = reader.ReadInt32();
						chunk.size.y = reader.ReadInt32();
						chunk.size.z = reader.ReadInt32();

						if (chunk.size.name[0] != 'S' || chunk.size.name[1] != 'I' || chunk.size.name[2] != 'Z' || chunk.size.name[3] != 'E')
							throw new System.Exception("Bad Token: token is not SIZE");

						if (chunk.size.chunkContent != 12)
							throw new System.Exception("Bad Token: chunk content is " + chunk.size.chunkContent + ", it should be 12.");

						chunk.xyzi.name = reader.ReadBytes(4);
						if (chunk.xyzi.name[0] != 'X' || chunk.xyzi.name[1] != 'Y' || chunk.xyzi.name[2] != 'Z' || chunk.xyzi.name[3] != 'I')
							throw new System.Exception("Bad Token: token is not XYZI");

						chunk.xyzi.chunkContent = reader.ReadInt32();
						chunk.xyzi.chunkNums = reader.ReadInt32();
						if (chunk.xyzi.chunkNums != 0)
							throw new System.Exception("Bad Token: chunk nums is " + chunk.xyzi.chunkNums + ", it should be 0.");

						var voxelNums = reader.ReadInt32();
						var voxels = new byte[voxelNums * 4];
						if (reader.Read(voxels, 0, voxels.Length) != voxels.Length)
							throw new System.Exception("Failed to read voxels");

						chunk.xyzi.voxels = new VoxData(voxels, chunk.size.x, chunk.size.y, chunk.size.z);

						voxel.chunkChild[i] = chunk;
					}

					if (reader.BaseStream.Position < reader.BaseStream.Length)
					{
						byte[] palette = reader.ReadBytes(4);
						if (palette[0] != 'R' || palette[1] != 'G' || palette[2] != 'B' || palette[3] != 'A')
							throw new System.Exception("Bad Token: token is not RGBA");

						voxel.palette.chunkContent = reader.ReadInt32();
						voxel.palette.chunkNums = reader.ReadInt32();

						var bytePalette = new byte[voxel.palette.chunkContent];
						reader.Read(bytePalette, 0, voxel.palette.chunkContent);

						voxel.palette.values = new uint[voxel.palette.chunkContent / 4];

						for (int i = 4; i < bytePalette.Length; i += 4)
							voxel.palette.values[i / 4] = BitConverter.ToUInt32(bytePalette, i - 4);
					}
					else
					{
						voxel.palette.values = new uint[_paletteDefault.Length];
						_paletteDefault.CopyTo(voxel.palette.values, 0);
					}

					return voxel;
				}
			}
		}

		public static VoxFileData Load(string path)
		{
			try
			{
				if (File.Exists(path))
					return Load(File.ReadAllBytes(path));
			}
			catch
			{
				return null;
			}

			return null;
		}

		public static Color32[] CreateColor32FromPelatte(uint[] palette)
		{
			Debug.Assert(palette.Length == 256);

			Color32[] colors = new Color32[256];

			for (uint j = 0; j < 256; j++)
			{
				uint rgba = palette[j];

				Color32 color;
				color.r = (byte)((rgba >> 0) & 0xFF);
				color.g = (byte)((rgba >> 8) & 0xFF);
				color.b = (byte)((rgba >> 16) & 0xFF);
				color.a = (byte)((rgba >> 24) & 0xFF);

				colors[j] = color;
			}

			return colors;
		}

		public static Texture2D CreateTextureFromColor16x16(Color32[] colors)
		{
			Debug.Assert(colors.Length == 256);

			Texture2D texture = new Texture2D(16, 16, TextureFormat.ARGB32, false, false);
			texture.name = "texture";
			texture.SetPixels32(colors);
			texture.Apply();

			return texture;
		}

		public static Texture2D CreateTextureFromColor256(Color32[] colors)
		{
			Debug.Assert(colors.Length == 256);

			Texture2D texture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false);
			texture.name = "texture";
			texture.SetPixels32(colors);
			texture.Apply();

			return texture;
		}

		public static Texture2D CreateTextureFromPelatte16x16(uint[] palette)
		{
			Debug.Assert(palette.Length == 256);
			return CreateTextureFromColor16x16(CreateColor32FromPelatte(palette));
		}

		public static int CalcFaceCountAsAllocate(VOXModel model, Color32[] palette, ref Dictionary<string, int> entities)
		{
			entities.Add("opaque", 0);

			foreach (var it in model.voxels)
			{
				int facesCount = 0;

				for (int j = 0; j < 6; j++)
				{
					if (it.faces[j])
						facesCount++;
				}

				entities["opaque"] += facesCount;
			}

			return entities.Count;
		}

		public static GameObject CreateGameObject(string name, VoxData data, Texture2D texture, Color32[] colors, float scale, Shader shader)
		{
			var cruncher = VOXPolygonCruncher.CalcVoxelCruncher(data, colors, VOXCruncherMode.Greedy);

			var entities = new Dictionary<string, int>();
			if (CalcFaceCountAsAllocate(cruncher, colors, ref entities) == 0)
				throw new System.Exception(name + ": There is no voxel for this file");

			var model = new GameObject(name);

			foreach (var entity in entities)
			{
				if (entity.Value == 0)
					continue;

				var index = 0;
				var allocSize = entity.Value;

				var vertices = new Vector3[allocSize * 4];
				var normals = new Vector3[allocSize * 4];
				var uv = new Vector2[allocSize * 4];
				var triangles = new int[allocSize * 6];

				bool isTransparent = false;

				foreach (var it in cruncher.voxels)
				{
					VOXModelExtensions.CreateCubeMesh16x16(it, ref vertices, ref normals, ref uv, ref triangles, ref index, scale);
					isTransparent |= (colors[it.material].a < 255) ? true : false;
				}

				if (triangles.Length > 0)
				{
					Mesh mesh = new Mesh();
					mesh.name = "mesh";
					mesh.vertices = vertices;
					mesh.normals = normals;
					mesh.uv = uv;
					mesh.triangles = triangles;

					var meshFilter = model.AddComponent<MeshFilter>();

#if UNITY_EDITOR
					MeshUtility.Optimize(mesh);
					meshFilter.sharedMesh = mesh;

					if (shader != null)
					{
						var meshRenderer = model.AddComponent<MeshRenderer>();
						meshRenderer.sharedMaterial = new Material(shader);
						meshRenderer.sharedMaterial.name = "material";
						meshRenderer.sharedMaterial.mainTexture = texture;
					}

#else
					meshFilter.mesh = mesh;

					if (shader != null)
					{
						var meshRenderer = model.AddComponent<MeshRenderer>();
						meshRenderer.material = new Material(shader);
						meshRenderer.material.name = "material";
						meshRenderer.material.mainTexture = texture;
					}
#endif
				}
			}

			return model;
		}

		public static void LoadVoxelFileAsGameObject(GameObject parent, VoxFileData voxel, int lodLevel, Shader shader)
		{
			try
			{
				var colors = CreateColor32FromPelatte(voxel.palette.values);
				var texture = CreateTextureFromColor16x16(colors);

				if (lodLevel <= 1)
				{
					foreach (var chunk in voxel.chunkChild)
					{
						var submesh = CreateGameObject("model", chunk.xyzi.voxels, texture, colors, 1, shader);
						submesh.transform.parent = parent.transform;
						submesh.transform.localPosition = Vector3.zero;
						submesh.transform.localRotation = Quaternion.identity;
						submesh.transform.localScale = Vector3.one;
					}
				}
				else
				{
					foreach (var chunk in voxel.chunkChild)
					{
						for (int lod = 1; lod < lodLevel + 1; lod++)
						{
							var submesh = CreateGameObject("lod" + (lod - 1), chunk.xyzi.voxels.GetVoxelDataLOD(lod), texture, colors, lod, shader);
							submesh.transform.parent = parent.transform;
						}

						var lodgroup = parent.AddComponent<LODGroup>();
						var lods = lodgroup.GetLODs();

						for (int i = 0; i < parent.transform.childCount; i++)
							lods[i].renderers = new Renderer[] { parent.transform.GetChild(i).GetComponent<MeshRenderer>() };

						lodgroup.SetLODs(lods);
					}
				}
			}
			catch (SystemException e)
			{
				Debug.LogException(e);

				throw e;
			}
		}

		public static GameObject LoadVoxelFileAsGameObject(string name, VoxFileData voxel, int lodLevel, string shader = "Mobile/Diffuse")
		{
			Debug.Assert(!String.IsNullOrEmpty(name));

			GameObject gameObject = new GameObject();
			gameObject.name = name;
			gameObject.isStatic = true;
			LoadVoxelFileAsGameObject(gameObject, voxel, lodLevel, Shader.Find(shader));

			return gameObject;
		}

		public static GameObject LoadVoxelFileAsGameObject(string name, VoxFileData voxel, int lodLevel, Shader shader)
		{
			Debug.Assert(!String.IsNullOrEmpty(name));

			GameObject gameObject = new GameObject();
			gameObject.name = name;
			gameObject.isStatic = true;
			LoadVoxelFileAsGameObject(gameObject, voxel, lodLevel, shader);

			return gameObject;
		}

		public static GameObject LoadVoxelFileAsGameObject(string path)
		{
			var voxel = VoxFileImport.Load(path);
			return LoadVoxelFileAsGameObject(Path.GetFileNameWithoutExtension(path), voxel, 0);
		}

		public static GameObject LoadVoxelFileAsGameObjectLOD(string path, int lodLevel)
		{
			var voxel = VoxFileImport.Load(path);
			return LoadVoxelFileAsGameObject(Path.GetFileNameWithoutExtension(path), voxel, lodLevel);
		}

#if UNITY_EDITOR

		public static GameObject LoadVoxelFileAsPrefab(VoxFileData voxel, string name, string path = "Assets/", int lodLevel = 0)
		{
			Debug.Assert(!String.IsNullOrEmpty(name));

			GameObject gameObject = null;

			try
			{
				gameObject = LoadVoxelFileAsGameObject(name, voxel, lodLevel);

				var prefabPath = path + name + ".prefab";
				var prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
				var prefabTextures = new Dictionary<string, int>();

				for (int i = 0; i < gameObject.transform.childCount; i++)
				{
					var subObject = gameObject.transform.GetChild(i);

					var meshFilter = subObject.GetComponent<MeshFilter>();
					if (meshFilter != null)
					{
						AssetDatabase.AddObjectToAsset(meshFilter.sharedMesh, prefabPath);
					}

					var renderer = subObject.GetComponent<MeshRenderer>();
					if (renderer != null)
					{
						if (renderer.sharedMaterial != null)
						{
							AssetDatabase.AddObjectToAsset(renderer.sharedMaterial, prefabPath);

							var textureName = renderer.sharedMaterial.mainTexture.name;
							if (!prefabTextures.ContainsKey(textureName))
							{
								prefabTextures.Add(textureName, 1);

								AssetDatabase.AddObjectToAsset(renderer.sharedMaterial.mainTexture, prefabPath);
							}
						}
					}
				}

				return PrefabUtility.ReplacePrefab(gameObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
			}
			finally
			{
				GameObject.DestroyImmediate(gameObject);
			}
		}

		public static GameObject LoadVoxelFileAsPrefab(string path, string outpath = "Assets/", int lodLevel = 0)
		{
			var voxel = VoxFileImport.Load(path);
			return LoadVoxelFileAsPrefab(voxel, Path.GetFileNameWithoutExtension(path), outpath, lodLevel);
		}

#endif
	}
}