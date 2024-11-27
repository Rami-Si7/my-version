using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

// [Tool]
public partial class World : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public int chunkSize = 13;
	public int worldSize = 3;
	private Dictionary<Vector3, Chunk> chunks;
	[Export]
	public PackedScene chunkScene { get; set; }

	public static World Instance;
	public int noiseSeed = 1234;
	public float maxHeight = 0.15f;
	public float noiseScale = 0.015f;
	public int loadRadius = 5;
	public int unloadRadius = 7;
	public float[,] noiseArray;
	public float topPoint;

	[Export]
	private Player player;
	private Vector3 playerPosition;

	private RandomNumberGenerator rng = new RandomNumberGenerator();
	public override void _Ready()
	{
		GD.Print("World _Ready called");

		Instance = this;
		GlobalNoise.SetSeed();
		chunks = new Dictionary<Vector3, Chunk>();
		noiseArray = GlobalNoise.GetNoise();

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
		playerPosition = player.getPlayerPosition();
		UpdateChunks(playerPosition);
	}
	void UpdateChunks(Vector3 playerPosition)
	{
		// GD.Print("playerPosition: (", playerPosition.X, ", ", playerPosition.Y, ", ", playerPosition.Z, ")");
		// Determine the chunk coordinates for the player's position
		Vector3I playerChunkCoordinates = new Vector3I(
			Mathf.FloorToInt(playerPosition.X / chunkSize),
			Mathf.FloorToInt(playerPosition.Y / chunkSize),
			Mathf.FloorToInt(playerPosition.Z / chunkSize));

		// Load and unload chunks based on the player's position
		LoadChunksAround(playerChunkCoordinates);
		UnloadDistantChunks(playerChunkCoordinates);
	}
	void LoadChunksAround(Vector3I centerChunkCoordinates)
	{
		Dictionary<Vector3, Chunk> newChunks  = new Dictionary<Vector3, Chunk>();
		for (int x = -loadRadius; x <= loadRadius; x++)
		{
			for (int z = -loadRadius; z <= loadRadius; z++)
			{
				Vector3I chunkCoordinates = new Vector3I(centerChunkCoordinates.X + x, 0, centerChunkCoordinates.Z + z);
				Vector3 chunkPosition = new Vector3(chunkCoordinates.X * chunkSize, 0, chunkCoordinates.Z * chunkSize);
				GD.Print("chunkPosition: (", chunkPosition.X, ", ", chunkPosition.Y, ", ", chunkPosition.Z, ")");

				if (!chunks.ContainsKey(chunkPosition))
				{
					var chunk = chunkScene.Instantiate<Chunk>();

					// Add the chunk to the scene tree before setting its GlobalTransform
					AddChild(chunk);

					// Now you can safely set the GlobalTransform
					chunk.GlobalTransform = new Transform3D(Basis.Identity, chunkPosition);
					// Store the chunk in the dictionary

					
					chunk.Initialize(chunkSize);
					chunks.Add(chunkPosition, chunk);
					newChunks.Add(chunkPosition, chunk);
					
				}
			}
		}
		foreach(var chunk in newChunks)
		{
			chunk.Value.GenerateMesh();
		}	
	}
	void UnloadDistantChunks(Vector3I centerChunkCoordinates)
	{
		List<Vector3> chunksToUnload = new List<Vector3>();
		foreach (var chunk in chunks)
		{
			Vector3I chunkCoord = new Vector3I(
				Mathf.FloorToInt(chunk.Key.X / chunkSize),
				Mathf.FloorToInt(chunk.Key.Y / chunkSize),
				Mathf.FloorToInt(chunk.Key.Z / chunkSize));

			if ((chunkCoord - centerChunkCoordinates).Length() > unloadRadius)
			{
				chunksToUnload.Add(chunk.Key);
			}
		}

		foreach (var chunkPos in chunksToUnload)
		{
			chunks[chunkPos].QueueFree();
			chunks.Remove(chunkPos);
		}
	}
}
