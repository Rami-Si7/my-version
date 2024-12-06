using Godot;
using System;
using System.Collections.Generic;
using SimplexNoise;

[Tool]
public partial class Chunk : StaticBody3D
{
	private MeshInstance3D meshInstance3D;

	[Export]
	private CollisionShape3D collisionShape3D;
	
	public Voxel[,,] voxels{get; set;}

	private int chunkSize;

	private List<Vector3> vertices = new List<Vector3>();
	private List<int> triangles = new List<int>();
	private List<Vector2> uvs = new List<Vector2>();
	
	private SurfaceTool surfaceTool = new();

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


	private int index;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// GD.Print("in ready");
		meshInstance3D = GetNode<MeshInstance3D>("MeshInstance3D");
		// this.voxels = new Voxel[this.chunkSize, this.chunkSize, this.chunkSize];

	}
	public void Initialize(int chunkSize)
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
					Vector3 globalPosition = GlobalPosition + new Vector3(x, y, z);
					Voxel.VoxelType type = DetermineVoxelType(globalPosition.X, globalPosition.Y, globalPosition.Z);
					Boolean isActive = type == Voxel.VoxelType.Air ? false: true;
					voxels[x, y, z] = new Voxel(GlobalTransform.Origin + new Vector3(x, y, z), type, isActive);
				}
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
			VertexColorUseAsAlbedo = true, // Use vertex colors as the texture
			AlbedoColor = Colors.DarkGreen,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled // Disable face culling (double-sided rendering)
		});

		var arrayMesh = surfaceTool.Commit();
		meshInstance3D.Mesh = arrayMesh;
		collisionShape3D.Shape = arrayMesh.CreateTrimeshShape();
	}
	public void GenerateMeshNeighbor(int x, int y, int z)
	{
		
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		ProcessVoxel(x, y, z);
		ProcessVoxel(x + 1, y, z);
		ProcessVoxel(x, y + 1, z);
		ProcessVoxel(x, y, z + 1);
		ProcessVoxel(x - 1, y, z);
		ProcessVoxel(x, y - 1, z);
		ProcessVoxel(x, y, z - 1);

		surfaceTool.SetMaterial(new StandardMaterial3D()
		{
			VertexColorUseAsAlbedo = true, // Use vertex colors as the texture
			AlbedoColor = Colors.WebGray,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled // Disable face culling (double-sided rendering)
		});

		var arrayMesh = surfaceTool.Commit();
		
		meshInstance3D.Mesh = arrayMesh;
		collisionShape3D.Shape = arrayMesh.CreateTrimeshShape();
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
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _top);
			}
			if (IsFaceVisible(x, y, z, 1))
			{
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _bottom);
			}
			if (IsFaceVisible(x, y, z, 2))
			{
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _left);
			}			
			if (IsFaceVisible(x, y, z, 3))
			{
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _right);
			}			
			if (IsFaceVisible(x, y, z, 4))
			{
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _back);
			}			
			if (IsFaceVisible(x, y, z, 5))
			{
				// GD.Print("is visiable!");
				AddFaceWithSurfaceTool(x, y, z, _front);
			}
		}
	}

	private void AddFaceWithSurfaceTool(int x, int y, int z, int[] face)
	{
		// GD.Print("in here");
		// Vector3[] faceVertices = GetFaceVertices(x, y, z, faceIndex);
		Vector2[] faceUVs = GetFaceUVs();

		var blockPosition = new Vector3I(x, y, z);
		var a = _vertices[face[0]] + blockPosition;
		var b = _vertices[face[1]] + blockPosition;
		var c = _vertices[face[2]] + blockPosition;
		var d = _vertices[face[3]] + blockPosition;

		var triangle1 = new Vector3[] { a, b, c };
		var triangle2 = new Vector3[] { a, c, d };

		
		// Vector3 normal = ((faceVertices[1] - faceVertices[0]).Cross(faceVertices[2] - faceVertices[0])).Normalized();

		// var normals = new Vector3[] { normal, normal, normal };

		surfaceTool.AddTriangleFan(triangle1);
		surfaceTool.AddTriangleFan(triangle2);

	}

	private Vector3[] GetFaceVertices(int x, int y, int z, int faceIndex)
	{
		// Define the vertices for each face relative to the voxel
		switch (faceIndex)
		{
			case 0: // Top face
				return new Vector3[]
				{
					new Vector3(x, y + 1, z),
					new Vector3(x, y + 1, z + 1),
					new Vector3(x + 1, y + 1, z + 1),
					new Vector3(x + 1, y + 1, z)
				};
			case 1: // Bottom face
				return new Vector3[]
				{
					new Vector3(x, y, z),
					new Vector3(x + 1, y, z),
					new Vector3(x + 1, y, z + 1),
					new Vector3(x, y, z + 1)
				};
			case 2: // Left face
				return new Vector3[]
				{
					new Vector3(x, y, z),
					new Vector3(x, y + 1, z),
					new Vector3(x, y + 1, z + 1),
					new Vector3(x, y, z + 1)
				};
			case 3: // Right face
				return new Vector3[]
				{
					new Vector3(x + 1, y, z + 1),
					new Vector3(x + 1, y + 1, z + 1),
					new Vector3(x + 1, y + 1, z),
					new Vector3(x + 1, y, z)
				};
			case 4: // Front face
				return new Vector3[]
				{
					new Vector3(x, y, z + 1),
					new Vector3(x + 1, y, z + 1),
					new Vector3(x + 1, y + 1, z + 1),
					new Vector3(x, y + 1, z + 1)
				};
			case 5: // Back face
				return new Vector3[]
				{
					new Vector3(x + 1, y, z),
					new Vector3(x, y, z),
					new Vector3(x, y + 1, z),
					new Vector3(x + 1, y + 1, z)
				};
			default:
				return new Vector3[0];
		}
	}

	private Vector2[] GetFaceUVs()
	{
		// Define UVs for a face
		return new Vector2[]
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(1, 1),
			new Vector2(0, 1)
		};
	}

	// private void ProcessVoxel(int x, int y, int z)
	// {
	// 	if (voxels == null || x < 0 || x >= voxels.GetLength(0) ||
	// 		y < 0 || y >= voxels.GetLength(1) || z < 0 || z >= voxels.GetLength(2))
	// 	{
	// 		return;
	// 	}

	// 	Voxel voxel = voxels[x, y, z];
	// 	if (voxel.type == Voxel.VoxelType.Air)
	// 	{

	// 		// GD.Print("DID NOT GO THROW VISIBLE CHECK");
	// 		return;
	// 	}
	// 	else
	// 	{
	// 		if (voxel.isActive)
	// 		{
	// 			// Check and merge faces for optimization
	// 		for (int face = 0; face < 6; face++)
	// 		{
	// 			if (IsFaceVisible(x, y, z, face))
	// 			{
	// 				AddFaceData(x, y, z, face); // Add merged face data
	// 			}
	// 		}
	// 		}
	// 	}
	// }
	
	// private Voxel.VoxelType DetermineVoxelType(float x, float y, float z)
	// {
	// 	// float frequency = 0.2f;
	// 	// float amplitude = 5f;

	// 	// float xyOffset = MathF.Sin((x + z) * frequency) * amplitude;
	// 	// float yzffset = MathF.Sin((z + y) * frequency) * amplitude;

	// 	// float surfaceY = 10 + xyOffset + yzffset;
	// 	// GD.Print("Y: ", y, " surfaceY: ", surfaceY);

		
	// 	return y < height ? Voxel.VoxelType.Stone : Voxel.VoxelType.Air;


	// }
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
		// Calculate the neighbor voxel's position within the current chunk
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


	private void AddFaceData(int x, int y, int z, int faceIndex)
	{
		// Based on faceIndex, determine vertices and triangles
		// Add vertices and triangles for the visible face
		// Calculate and add corresponding UVs

		if (faceIndex == 0) // Top Face
		{
			vertices.Add(new Vector3(x,     y + 1, z    ));
			vertices.Add(new Vector3(x,     y + 1, z + 1)); 
			vertices.Add(new Vector3(x + 1, y + 1, z + 1));
			vertices.Add(new Vector3(x + 1, y + 1, z    )); 
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(0, 1));
		}

		if (faceIndex == 1) // Bottom Face
		{
			vertices.Add(new Vector3(x,     y, z    ));
			vertices.Add(new Vector3(x + 1, y, z    )); 
			vertices.Add(new Vector3(x + 1, y, z + 1));
			vertices.Add(new Vector3(x,     y, z + 1)); 
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(0, 1));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(1, 0));
		}

		if (faceIndex == 2) // Left Face
		{
			vertices.Add(new Vector3(x, y,     z    ));
			vertices.Add(new Vector3(x, y,     z + 1));
			vertices.Add(new Vector3(x, y + 1, z + 1));
			vertices.Add(new Vector3(x, y + 1, z    ));
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(0, 1));
			uvs.Add(new Vector2(0, 1));
		}

		if (faceIndex == 3) // Right Face
		{
			vertices.Add(new Vector3(x + 1, y,     z + 1));
			vertices.Add(new Vector3(x + 1, y,     z    ));
			vertices.Add(new Vector3(x + 1, y + 1, z    ));
			vertices.Add(new Vector3(x + 1, y + 1, z + 1));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(1, 0));
		}

		if (faceIndex == 4) // Front Face
		{
			vertices.Add(new Vector3(x,     y,     z + 1));
			vertices.Add(new Vector3(x + 1, y,     z + 1));
			vertices.Add(new Vector3(x + 1, y + 1, z + 1));
			vertices.Add(new Vector3(x,     y + 1, z + 1));
			uvs.Add(new Vector2(0, 1));
			uvs.Add(new Vector2(0, 1));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(1, 1));
		}

		if (faceIndex == 5) // Back Face
		{
			vertices.Add(new Vector3(x + 1, y,     z    ));
			vertices.Add(new Vector3(x,     y,     z    ));
			vertices.Add(new Vector3(x,     y + 1, z    ));
			vertices.Add(new Vector3(x + 1, y + 1, z    ));
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(0, 0));
			
		}
		AddTriangleIndices();
	}
	private void AddTriangleIndices()
	{
		int vertCount = vertices.Count;

		// First triangle
		triangles.Add(vertCount - 4);
		triangles.Add(vertCount - 3);
		triangles.Add(vertCount - 2);

		// Second triangle
		triangles.Add(vertCount - 4);
		triangles.Add(vertCount - 2);
		triangles.Add(vertCount - 1);
	}

	// public void GenerateMesh()
	// {
	// 	IterateVoxels();
	// 	if (vertices.Count == 0 || triangles.Count == 0)
	// 	{
	// 		return; // Skip creating a mesh for this chunk
	// 	}
	// 	var colors = new List<Color>();
	// 	foreach (var vertex in vertices)
	// 	{
	// 		if (vertex.Y >= World.Instance.topPoint - 2)
	// 		{
	// 			// Snow (highest elevation)
	// 			colors.Add(new Color(1.0f, 1.0f, 1.0f)); // White
	// 		}
	// 		else if (vertex.Y < chunkSize && vertex.Y >= chunkSize / 2)
	// 		{
	// 			// Grass (medium-high elevation)
	// 			colors.Add(new Color(0.0f, 1.0f, 0.0f)); // Green
	// 		}
	// 		else if (vertex.Y < chunkSize / 2 && vertex.Y >= chunkSize / 4)
	// 		{
	// 			// Dirt (medium-low elevation)
	// 			colors.Add(new Color(0.54f, 0.27f, 0.07f)); // Brown
	// 		}
	// 		else
	// 		{
	// 			// Stone (lowest elevation)
	// 			colors.Add(new Color(0.5f, 0.5f, 0.5f)); // Gray
	// 		}

	// 	}

	// 	var arrayMesh = new ArrayMesh();
	// 	var array = new Godot.Collections.Array();

	// 	array.Resize((int)Mesh.ArrayType.Max);

	// 	array[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
	// 	array[(int)Mesh.ArrayType.Index] = triangles.ToArray();
	// 	array[(int)Mesh.ArrayType.Color] = colors.ToArray();


	// 	arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);
	// 	// Texture2D texture = ResourceLoader.Load<Texture2D>("res://stone.png");

	// 	var material = new StandardMaterial3D()
	// 	{
	// 		VertexColorUseAsAlbedo = true, // Use vertex colors as the texture
	// 		CullMode = BaseMaterial3D.CullModeEnum.Disabled // Disable face culling (double-sided rendering)
	// 	};
		
	// 	arrayMesh.SurfaceSetMaterial(0, material);
	// 	collisionShape3D.Shape = arrayMesh.CreateTrimeshShape();
	// 	meshInstance3D.Mesh = arrayMesh;
	// }
	public void BreakBlock(Vector3 localPosition)
	{
			// Convert the local position to voxel indices
		int x = Mathf.FloorToInt(localPosition.X);
		int y = Mathf.FloorToInt(localPosition.Y);
		int z = Mathf.FloorToInt(localPosition.Z);

		// Ensure the indices are within bounds
		if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
		{
			// Mark the target block as Air
			voxels[x, y, z].type = Voxel.VoxelType.Air;
			voxels[x, y, z].isActive = false;

			// here we check the boundries, if the block we want to delete is at the boundry of the block
			// we must generate the neighbouring chunks
			if(x == chunkSize -1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x + 1, y, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);
				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}

			if(z == chunkSize - 1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y, z + 1);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}
			if(y == chunkSize - 1)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y + 1, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}
			if(x == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x - 1, y, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);
				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}

			if(z == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y, z - 1);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}
			if(y == 0)
			{
				Vector3 globalPos = GlobalPosition + new Vector3(x, y - 1, z);
				var neighborChunk = World.Instance.GetChunkAt(globalPos);

				if(neighborChunk != null)
				{
					neighborChunk.GenerateMesh();
				}
			}
			

			GenerateMesh();
		}
		else
		{
			GD.PrintErr("Voxel position is out of bounds for this chunk!");
		}
	}
	private void UpdateChunkMesh()
	{

	}
	
	public void BuildBlock(Vector3 localPosition)
	{
		int x = Mathf.FloorToInt(localPosition.X);
		int y = Mathf.FloorToInt(localPosition.Y);
		int z = Mathf.FloorToInt(localPosition.Z);

		if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
		{
			GD.Print(voxels[x, y, z].type);
			voxels[x, y, z].type = Voxel.VoxelType.Stone;
			voxels[x, y, z].isActive = true;
			GenerateMesh();
		}


	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
