using System.Collections.Generic;
using Godot;
using System;
using SimplexNoise;
using System.Linq;
using System.Threading;
using System.Drawing;

[Tool]
public partial class Chunk : StaticBody3D
{
	
	private MeshInstance3D meshInstance3D;

	[Export]
	private CollisionShape3D collisionShape3D;
	
	public Voxel[,,] voxels{get; set;}

	private int chunkSize;

	private Dictionary<Vector3, List<Vector3>> vertices = new Dictionary<Vector3, List<Vector3>>();
	private Dictionary<Vector3, List<Vector3[]>> triangles = new Dictionary<Vector3, List<Vector3[]>>();
	private List<Vector2> uvs = new List<Vector2>();
	private Dictionary<int, Dictionary<Vector3, Voxel.VoxelType>> divisons;
	
	public SurfaceTool surfaceTool = new();

	private Random random = new Random();

	private static readonly Vector3I[] _vertices = new Vector3I[]
	{
		new Vector3I(0, 0, 0),
		new Vector3I(1, 0, 0),
		new Vector3I(0, 1, 0),
		new Vector3I(1, 1, 0),
		new Vector3I(0, 0, 1),
		new Vector3I(1, 0, 1),
		new Vector3I(0, 1, 1),
		new Vector3I(1, 1, 1)
	};

	private static readonly int[] _top = new int[] { 2, 3, 7, 6 };
	private static readonly int[] _bottom = new int[] { 0, 4, 5, 1 };
	private static readonly int[] _left = new int[] { 6, 4, 0, 2 };
	private static readonly int[] _right = new int[] { 3, 1, 5, 7 };
	private static readonly int[] _back = new int[] { 7, 5, 4, 6 };
	private static readonly int[] _front = new int[] { 2, 0, 1, 3 };

	public enum OperationType
	{
		Break,

		Place,
	}



	/// <summary>
	/// 
	/// Octree vars
	/// the idea is each chunk will represent an Octree
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// 
	/// </summary>
	public static int levels = 3;
	public int grassCount = 0;
	public int airCount = 0;


	public override void _Ready()
	{
		meshInstance3D = GetNode<MeshInstance3D>("MeshInstance3D");
		

	}
	public void Initialize(int chunkSize, Vector3 position)
	{
		// GD.Print("in Initialize");
		this.chunkSize = chunkSize;
		voxels = new Voxel[this.chunkSize, this.chunkSize, this.chunkSize];
		initializeVoxels();
	}
	private void initializeVoxels()
	{
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					Vector3 voxelPosition = new Vector3(x, y, z);
					Vector3 globalPosition = GlobalPosition + voxelPosition;

					Voxel.VoxelType type = DetermineVoxelType(globalPosition.X, globalPosition.Y, globalPosition.Z);
					Boolean isActive = type == Voxel.VoxelType.Air ? false: true;
					if (type == Voxel.VoxelType.Air)
					{
						airCount++;
					}
					else
					{
						grassCount++;
					}

					voxels[x, y, z] = new Voxel(GlobalTransform.Origin + new Vector3(x, y, z), type, isActive, new Vector3(x, y, z));

					vertices[voxelPosition] = new List<Vector3>();

					triangles[voxelPosition] = new List<Vector3[]>();
				}
			}
		}	
	}
		private void ProcessVoxel(int x, int y, int z)
	{
		if (voxels == null || x < 0 || x >= voxels.GetLength(0) ||
			y < 0 || y >= voxels.GetLength(1) || z < 0 || z >= voxels.GetLength(2))
		{
			return;
		}

		Voxel voxel = voxels[x, y, z];
		if (voxel.type == Voxel.VoxelType.Air)
		{
			return;
		}

		if (voxel.isActive)
		{
			if (IsFaceVisible(x, y, z, 0))
			{
				AddFaceWithSurfaceTool(x, y, z, _top);
			}
			if (IsFaceVisible(x, y, z, 1))
			{
				AddFaceWithSurfaceTool(x, y, z, _bottom);
			}
			if (IsFaceVisible(x, y, z, 2))
			{
				AddFaceWithSurfaceTool(x, y, z, _left);
			}			
			if (IsFaceVisible(x, y, z, 3))
			{
				AddFaceWithSurfaceTool(x, y, z, _right);
			}			
			if (IsFaceVisible(x, y, z, 4))
			{
				AddFaceWithSurfaceTool(x, y, z, _back);
			}			
			if (IsFaceVisible(x, y, z, 5))
			{
				AddFaceWithSurfaceTool(x, y, z, _front);
			}
		}
	}
	
	public void GenerateMesh()
	{
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					ProcessVoxel(x, y, z);
				}
			}
		}
		surfaceTool.SetMaterial(new StandardMaterial3D()
		{
			VertexColorUseAsAlbedo = true, 
			AlbedoColor = Colors.DarkGreen,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled 
		});

		var arrayMesh = surfaceTool.Commit();
		meshInstance3D.Mesh = arrayMesh;
		collisionShape3D.Shape = arrayMesh.CreateTrimeshShape();
	}
	public void UpdateChunkMesh()
	{
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		// GD.Print("int UpdateChunkMesh");
		foreach(var position in triangles.Keys)
		{
			int x = Mathf.FloorToInt(position.X);
			int y = Mathf.FloorToInt(position.Y);
			int z = Mathf.FloorToInt(position.Z);
			Octree root = voxels[x, y, z].octree;
			// if(position.X == 3 && position.Y == 3 && position.Z == 1)
			if(root.children != null)
			{
				GD.Print("in here");
				triangles[position].Clear();
				root.TraverseOctree(surfaceTool);
			}
			else
			{
				foreach(var triangle in triangles[position])
				{
					surfaceTool.AddTriangleFan(triangle);
				}
			}
						
		}

		surfaceTool.SetMaterial(new StandardMaterial3D()
		{
			VertexColorUseAsAlbedo = true, 
			AlbedoColor = Colors.DarkGreen,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled 
		});

		var arrayMesh = surfaceTool.Commit();
		meshInstance3D.Mesh = arrayMesh;
		collisionShape3D.Shape = arrayMesh.CreateTrimeshShape();
	}
	

	private void AddFaceWithSurfaceTool(int x, int y, int z, int[] face)
	{
		var blockPosition = new Vector3I(x, y, z);
		var a = _vertices[face[0]] + blockPosition;
		var b = _vertices[face[1]] + blockPosition;
		var c = _vertices[face[2]] + blockPosition;
		var d = _vertices[face[3]] + blockPosition;

		var triangle1 = new Vector3[] { a, b, c };
		var triangle2 = new Vector3[] { a, c, d };


		surfaceTool.AddTriangleFan(triangle1);
		surfaceTool.AddTriangleFan(triangle2);
		triangles[blockPosition].Add(triangle1);
		triangles[blockPosition].Add(triangle2);

	}
	private Voxel.VoxelType DetermineVoxelType(float x, float y, float z)
	{
		float noiseValue = GlobalNoise.GetNoisePoint((int)x, (int)z); 
		// Normalize noise value to [0, 1]
		float normalizedNoiseValue = (noiseValue + 1) / 2;

		// Calculate maxHeight
		float maxHeight = normalizedNoiseValue * World.Instance.maxHeight;
		if(maxHeight > World.Instance.topPoint)
		{
			World.Instance.topPoint = maxHeight;
		}


		if (y <= maxHeight)
			return Voxel.VoxelType.Stone; // Solid voxel
		else
			return Voxel.VoxelType.Air; // Air voxel
	}
	private bool IsFaceVisible(int x, int y, int z, int face)
	{
		int nx = x, ny = y, nz = z;
		// if (x < 0 || x >= chunkSize || y < 0 || y >= chunkSize || z < 0 || z >= chunkSize)
		// 	return true;
		switch (face)
		{
			case 0: ny += 1; break; // Top
			case 1: ny -= 1; break; // Bottom
			case 2: nx -= 1; break; // Left
			case 3: nx += 1; break; // Right
			case 4: nz += 1; break; // Front
			case 5: nz -= 1; break; // Back
		}

		// If the neighbor is out of bounds for this chunk, check the neighboring chunk
		if (nx < 0 || nx >= chunkSize || ny < 0 || ny >= chunkSize || nz < 0 || nz >= chunkSize)
		{
			// Get the global position of the neighboring voxel
			Vector3 globalPos = GlobalPosition + new Vector3(nx, ny, nz);

			// Check if the face is hidden in the world
			return IsVoxelHiddenInWorld(globalPos);
		}

		// For voxels within the chunk, check their active state
		return !voxels[nx, ny, nz].isActive;
	}



	private bool IsVoxelHiddenInWorld(Vector3 globalPos)
	{
		// Calculate the chunk position in the world
		Vector3 chunkPosition = new Vector3(
			Mathf.FloorToInt(globalPos.X / chunkSize) * chunkSize,
			Mathf.FloorToInt(globalPos.Y / chunkSize) * chunkSize,
			Mathf.FloorToInt(globalPos.Z / chunkSize) * chunkSize
		);

		// Get the neighbor chunk at the calculated position
		Chunk neighborChunk = World.Instance.GetChunkAt(chunkPosition);

		if (neighborChunk == null)
		{
			// No chunk exists at this position, so the voxel face is visible
			return true;
		}

		// Convert the global position to the local position within the neighboring chunk
		Vector3 localPos = globalPos - neighborChunk.GlobalPosition;

		// Get the local indices
		int localX = Mathf.FloorToInt(localPos.X);
		int localY = Mathf.FloorToInt(localPos.Y);
		int localZ = Mathf.FloorToInt(localPos.Z);

		if (CheckNeighbors(neighborChunk, localX, localY, localZ))
		{
			return true;
		}

		// Ensure the indices are within bounds
		if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize || localZ < 0 || localZ >= chunkSize)
		{
			return true; // Out of bounds in the neighbor chunk
		}

		// Return the active state of the voxel
		return !neighborChunk.voxels[localX, localY, localZ].isActive;
	}
	private Boolean CheckNeighbors(Chunk neighborChunk, int localX, int localY, int localZ)
	{
		if(neighborChunk.voxels[localX, localY, localZ].type == Voxel.VoxelType.Air)
		{
			return true;
		}
		if(localX < chunkSize - 1 && neighborChunk.voxels[localX + 1, localY, localZ].type == Voxel.VoxelType.Air)
		{
			return true;
		}
		if(localZ < chunkSize - 1 && neighborChunk.voxels[localX, localY, localZ + 1].type == Voxel.VoxelType.Air)
		{
			return true;
		}
		if(localZ < chunkSize - 1 && localX < chunkSize - 1 && neighborChunk.voxels[localX + 1, localY, localZ + 1].type == Voxel.VoxelType.Air)
		{
			return true;
		}
		return false;
	}

	public void SetBlock(Vector3 localPosition, OperationType type, Vector3 collisonNormal)
	{
			// Convert the local position to voxel indices
		int x = Mathf.FloorToInt(localPosition.X);
		int y = Mathf.FloorToInt(localPosition.Y);
		int z = Mathf.FloorToInt(localPosition.Z);

		// Ensure the indices are within bounds
		if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
		{

			Vector3I blockPosition = new Vector3I(x, y, z);

			if(OperationType.Break == type) 
			{
				voxels[x, y, z].type = Voxel.VoxelType.Air;
				voxels[x, y, z].isActive = false;
				triangles[blockPosition].Clear();
			}
			else 
			{
				voxels[x, y, z].type = Voxel.VoxelType.Stone;
				voxels[x, y, z].isActive = true;
				ProcessVoxel(x, y, z);
			}

			ProccessNeighbourTriangles(blockPosition);

			// here we check the boundries,
			// if the block we want to delete is at the boundry of the block
			// we must generate the neighbouring chunks
			
			if(x == chunkSize -1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x + 1, y, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);
				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}

			if(z == chunkSize - 1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y, z + 1);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}
			if(y == chunkSize - 1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y + 1, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}
			if(x == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x - 1, y, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);
				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}

			if(z == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y, z - 1);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}
			if(y == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y - 1, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.UpdateChunkMesh();
				}
			}
			

			UpdateChunkMesh();
		}
		else
		{
			GD.PrintErr("Voxel position is out of bounds for this chunk!");

			// will give us the neighbouring chunk
			Vector3 blockGlobalPosition = GlobalPosition + localPosition + collisonNormal;

			var chunk = World.Instance.GetChunkAt(blockGlobalPosition);
			if(chunk == null)
			{
				GD.PrintErr("you cant build here");
			}
			else
			{
				Vector3 localP = blockGlobalPosition - chunk.GlobalPosition - collisonNormal;
				x = Mathf.FloorToInt(localP.X);
				y = Mathf.FloorToInt(localP.Y);
				z = Mathf.FloorToInt(localP.Z);
				if(OperationType.Break == type) 
				{
					chunk.voxels[x, y, z].type = Voxel.VoxelType.Air;
					chunk.voxels[x, y, z].isActive = false;
					chunk.triangles[localP].Clear();
				}
				else 
				{
					chunk.voxels[x, y, z].type = Voxel.VoxelType.Stone;
					chunk.voxels[x, y, z].isActive = true;
					chunk.ProcessVoxel(x, y, z);
				}
				chunk.ProccessNeighbourTriangles((Vector3I)localP);
				chunk.UpdateChunkMesh();
				}

		}
	}
	public void BreakVoxels(Vector3 breakPosition, Vector3 blockPosition)
	{
		int x = Mathf.FloorToInt(blockPosition.X);
		int y = Mathf.FloorToInt(blockPosition.Y);
		int z = Mathf.FloorToInt(blockPosition.Z);

		Octree octree = voxels[x, y , z].octree;
		Octree leaf = octree.LocateLeaf(breakPosition);
		if (leaf != null)
		{
			// GD.Print("(x,y,z):" , x,", ", y, " ", z);

			if(leaf.alreadyDestroyed == false)
			{
				leaf.AddNodes(breakPosition);
				leaf.dominantType = Voxel.VoxelType.Air;
				UpdateChunkMesh();
			}
		}
	}
	void ProccessNeighbourTriangles(Vector3I blockPosition)
	{
		// here we need to check 5 neighbours

		// bottom
		// left
		// righ
		// forward
		// backword

		int x = blockPosition.X;
		int y = blockPosition.Y;
		int z = blockPosition.Z;

		ProcessVoxel(x, y-1, z);
		ProcessVoxel(x-1, y, z);
		ProcessVoxel(x+1, y, z);
		ProcessVoxel(x, y, z-1);
		ProcessVoxel(x, y, z+1);


	}
	public void ResetChunk() 
	{
		// Clear voxel data
		voxels = new Voxel[chunkSize, chunkSize, chunkSize];
	

		vertices.Clear();
		triangles.Clear();
		uvs.Clear();
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
