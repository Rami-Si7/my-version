using Godot;
using System;
using System.Collections.Generic;

// [Tool]
public partial class Chunk : StaticBody3D
{
	private MeshInstance3D meshInstance3D;
	private CollisionObject3D collisionObject3D;
	
	public Voxel[,,] voxels{get; set;}

	private int chunkSize;

	private List<Vector3> vertices = new List<Vector3>();
	private List<int> triangles = new List<int>();
	private List<Vector2> uvs = new List<Vector2>();

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
					voxels[x, y, z] = new Voxel(GlobalTransform.Origin + new Vector3(x, y, z), Colors.Green, true);
				}
			}
		}	
	}
	public void IterateVoxels()
	{
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
	}


	private void ProcessVoxel(int x, int y, int z)
	{
		if (voxels == null || x < 0 || x >= voxels.GetLength(0) ||
			y < 0 || y >= voxels.GetLength(1) || z < 0 || z >= voxels.GetLength(2))
		{
			return;
		}

		Voxel voxel = voxels[x, y, z];
		if (voxel.isActive)
		{
			// Check and merge faces for optimization
			for (int face = 0; face < 6; face++)
			{
				if (IsFaceVisible(x, y, z, face))
				{
					AddFaceData(x, y, z, face); // Add merged face data
				}
			}
		}
	}
	private bool IsFaceVisible(int x, int y, int z, int face)
	{
		// Calculate the neighbor voxel's position within the current chunk
		int nx = x, ny = y, nz = z;

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

		// Ensure the indices are within bounds
		if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize || localZ < 0 || localZ >= chunkSize)
		{
			return true; // Out of bounds in the neighbor chunk
		}

		// Return the active state of the voxel
		return !neighborChunk.voxels[localX, localY, localZ].isActive;
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
	public void GenerateMesh()
	{
		IterateVoxels();
	if (vertices.Count == 0 || triangles.Count == 0)
    {
        return; // Skip creating a mesh for this chunk
    }

		var arrayMesh = new ArrayMesh();
		var array = new Godot.Collections.Array();
		var colors = new List<Color>();
		foreach (var vertex in vertices)
		{
			colors.Add(new Color(GD.Randf(), GD.Randf(), GD.Randf()));
		}
		array.Resize((int)Mesh.ArrayType.Max);

		array[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		array[(int)Mesh.ArrayType.Index] = triangles.ToArray();
		array[(int)Mesh.ArrayType.Color] = colors.ToArray();

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		var material = new StandardMaterial3D();
		material.VertexColorUseAsAlbedo = true; // Use vertex colors as the texture
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // Disable face culling (double-sided rendering)
		
		arrayMesh.SurfaceSetMaterial(0, material);
		meshInstance3D.Mesh = arrayMesh;

	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}