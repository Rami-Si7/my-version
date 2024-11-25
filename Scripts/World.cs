using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

// [Tool]
public partial class World : Node3D
{
	// Called when the node enters the scene tree for the first time.
	private int chunkSize = 5;
	public int worldSize = 5;
	private Dictionary<Vector3, Chunk> chunks;
	[Export]
	public PackedScene chunkScene { get; set; }

	public static World Instance;
	public override void _Ready()
	{
		GD.Print("World _Ready called");
		Instance = this;
		chunks = new Dictionary<Vector3, Chunk>();
		GenerateWorld();
	}
	private void GenerateWorld()
	{
		int i = 1;
		for (int x = 0; x < worldSize; x++)
		{
			for (int y = 0; y < worldSize; y++)
			{
				for (int z = 0; z < worldSize; z++)
				{
					// GD.Print("chunk ", i);
					i += 1;
					Vector3 chunkPosition = new Vector3(x * chunkSize, y * chunkSize, z * chunkSize);
					var chunk = chunkScene.Instantiate<Chunk>();

					// Add the chunk to the scene tree before setting its GlobalTransform
					AddChild(chunk);

					// Now you can safely set the GlobalTransform
					chunk.GlobalTransform = new Transform3D(Basis.Identity, chunkPosition);
					// Store the chunk in the dictionary

					
					chunk.Initialize(chunkSize);			
					chunks.Add(chunkPosition, chunk);


					// Initialize the chunk
				}
			}
		}
		foreach(var chunk in chunks)
		{
			chunk.Value.GenerateMesh();
		}		
	}
	public Chunk GetChunkAt(Vector3 globalPos)
	{
		// Calculate the chunk position in the world
		Vector3 chunkPos = new Vector3(
			Mathf.FloorToInt(globalPos.X / chunkSize) * chunkSize,
			Mathf.FloorToInt(globalPos.Y / chunkSize) * chunkSize,
			Mathf.FloorToInt(globalPos.Z / chunkSize) * chunkSize
		);

		// Check if the chunk exists in the dictionary
		if (chunks.ContainsKey(chunkPos))
		{
			return chunks[chunkPos];
		}

		return null; // No chunk at this position
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
