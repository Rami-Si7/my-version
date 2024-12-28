using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

[Tool]
public partial class World : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public int chunkSize = 1;
	public int worldSize = 1;
	private Dictionary<Vector3, Chunk> chunks;
	[Export]
	public PackedScene chunkScene { get; set; }

	public static World Instance;
	public int noiseSeed = 1234;
	public float maxHeight = 0.15f;
	public float noiseScale = 0.12f;
	public int loadRadius = 5;
	public int unloadRadius = 7;
	public float[,] noiseArray;
	public float topPoint;

	[Export]
	private Player player;
	private Vector3 playerPosition;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	private Queue<Chunk> chunkPool = new Queue<Chunk>();
	public int initialPoolSize = 10;

	private Queue<Vector3> chunkLoadQueue = new Queue<Vector3>();
	private int chunksPerFrame = 4; // Number of chunks to load per frame
	private int loadInterval = 4; // Load chunks every 4 frames
	private int frameCounter = 0;

	private Queue<Vector3> chunkUnloadQueue = new Queue<Vector3>();
	private int unloadFrameCounter = 0;
	private int unloadInterval = 5;
	private int chunksPerFrameUnloading = 4;

	private Vector3I lastPlayerChunkCoordinates;
	private int chunksMovedCount = 0;
	public int chunkUpdateThreshold = 5; // Update every 5 chunks
	private bool JustStarted = true;


	public void PopulateInitialPool() 
	{
		for (int i = 0; i < initialPoolSize; i++) {
			Chunk newChunk = InstantiateNewChunk();
			chunkPool.Enqueue(newChunk);
		}
	}

	public Chunk GetChunk()
	{
		Chunk chunk;
		if (chunkPool.Count > 0) {
			chunk = chunkPool.Dequeue();
		} else {
			chunk = InstantiateNewChunk();
		}
		return chunk;
	}

	public void ReturnChunk(Chunk chunk)
	{
		// GD.Print("in here");
		// GD.Print("chunkPool: ", chunkPool.Count);
		chunk.ResetChunk();
		chunk.Visible = false;
		chunkPool.Enqueue(chunk);
	}

	private Chunk InstantiateNewChunk() {
		
		Chunk newChunk = chunkScene.Instantiate<Chunk>();
		return newChunk;
	}


	public override void _Ready()
	{

		Instance = this;
		GlobalNoise.SetSeed();
		player = GetNodeOrNull<Player>("Player");
		chunks = new Dictionary<Vector3, Chunk>();
		noiseArray = GlobalNoise.GetNoise();
		PopulateInitialPool();
		GenerateWorld();
	}
	
	private void ClearChunks()
	{
		foreach (var chunk in chunks.Values)
		{
			chunk.QueueFree();
		}
		chunks.Clear();
	}
	private void GenerateWorld()
	{
		ClearChunks();
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

					// chunk.octree = new Octree(chunkPosition + new Vector3(chunkSize/2, chunkSize/2, chunkSize/2), chunkSize, 0, false);	
					chunk.Initialize(chunkSize, chunkPosition);		
					chunks.Add(chunkPosition, chunk);

					// Initialize the chunk
				}
			}
		}
		foreach(var chunk in chunks)
		{
			chunk.Value.GenerateMesh();		
		}
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0,0.0f), new Vector3(0,0,0.0f));
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0.6f,0,0.5f), new Vector3(0.6f,0,0.5f));
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0.78f,0f), new Vector3(0,0.78f,0f));
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0.9f,0.55f), new Vector3(0,0.78f,0.55f));	
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0.9f,0.1f), new Vector3(0,0.9f,0.1f));
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0.9f,0.14f), new Vector3(0,0.9f,0.14f));
	// chunks[new Vector3(0,0,0)].BreakVoxels(new Vector3(0,0.91f,0.14f), new Vector3(0,0.91f,0.14f));				
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
	public override void _PhysicsProcess(double delta)
	{
		// if(player != null)
		// {
		// 	// GD.Print("im process, frame number", frameCounter);
		// 	playerPosition = player.getPlayerPosition();
		// 	UpdateChunks(playerPosition);
		// 	ProcessChunkLoadingQueue();
		// 	ProcessChunkUnloadingQueue();
		// }
	}
	// void UpdateChunks(Vector3 playerPosition)
	// {
	// 	// GD.Print("playerPosition: (", playerPosition.X, ", ", playerPosition.Y, ", ", playerPosition.Z, ")");
	// 	// Determine the chunk coordinates for the player's position
	// 	Vector3I playerChunkCoordinates = new Vector3I(
	// 		Mathf.FloorToInt(playerPosition.X / chunkSize),
	// 		Mathf.FloorToInt(playerPosition.Y / chunkSize),
	// 		Mathf.FloorToInt(playerPosition.Z / chunkSize));

	// 	// Load and unload chunks based on the player's position
	// 	LoadChunksAround(playerChunkCoordinates);
	// 	UnloadDistantChunks(playerChunkCoordinates);
	// }
	void UpdateChunks(Vector3 playerPosition)
{
		Vector3I playerChunkCoordinates = new Vector3I(
		Mathf.FloorToInt(playerPosition.X / chunkSize),
		Mathf.FloorToInt(playerPosition.Y / chunkSize),
		Mathf.FloorToInt(playerPosition.Z / chunkSize));

	// Check if player has moved to a new chunk
	if (!playerChunkCoordinates.Equals(lastPlayerChunkCoordinates))
	{
		if(chunksMovedCount >= chunkUpdateThreshold || JustStarted) {
			LoadChunksAround(playerChunkCoordinates);
			UnloadDistantChunks(playerChunkCoordinates);
			JustStarted = false;
			chunksMovedCount = 0;
		}
		
		lastPlayerChunkCoordinates = playerChunkCoordinates;
		chunksMovedCount++;
	}
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

				if (!chunks.ContainsKey(chunkPosition))
				{
					// GD.Print("chunk loaded");
					chunkLoadQueue.Enqueue(chunkPosition);
					// var chunk = GetChunk();
					// chunk.Visible = true;

					// AddChild(chunk);
					
					// chunk.GlobalTransform = new Transform3D(Basis.Identity, chunkPosition);
					
					// chunk.Initialize(chunkSize);
					// chunks.Add(chunkPosition, chunk);
					// chunk.GenerateMesh();
					// newChunks.Add(chunkPosition, chunk);
					
				}
			}
		}
		// foreach(var chunk in newChunks)
		// {
		// 	chunk.Value.GenerateMesh();
		// }	
	}
	void ProcessChunkLoadingQueue()
	{
		frameCounter++;
		Dictionary<Vector3, Chunk> newChunks  = new Dictionary<Vector3, Chunk>();
		if(frameCounter % loadInterval == 0) 
		{
			for(int i = 0; i < chunksPerFrame && chunkLoadQueue.Count > 0; i++) 
			{
				Vector3 chunkPosition = chunkLoadQueue.Dequeue();
				if (!chunks.ContainsKey(chunkPosition)) 
				{
					
					var chunk = GetChunk();
					chunk.Visible = true;

					AddChild(chunk);
					
					chunk.GlobalTransform = new Transform3D(Basis.Identity, chunkPosition);
					
					chunk.Initialize(chunkSize, chunkPosition);
					chunk.GenerateMesh();
					chunks.Add(chunkPosition, chunk);
					newChunks.Add(chunkPosition, chunk);
				}
			}
			// foreach(var chunk in newChunks)
			// {
			// 	GD.Print("in generating mesh for new chunk");
			// }	
		}
		
}
	void UnloadDistantChunks(Vector3I centerChunkCoordinates)
	{
		// List<Vector3> chunksToUnload = new List<Vector3>();
		foreach (var chunk in chunks)
		{
			Vector3I chunkCoord = new Vector3I(
				Mathf.FloorToInt(chunk.Key.X / chunkSize),
				Mathf.FloorToInt(chunk.Key.Y / chunkSize),
				Mathf.FloorToInt(chunk.Key.Z / chunkSize));

			if ((chunkCoord - centerChunkCoordinates).Length() > unloadRadius)
			{
				chunkUnloadQueue.Enqueue(chunk.Key);
				
			}
		}

		// foreach (var chunkPos in chunksToUnload)
		// {
		// 	ReturnChunk(chunks[chunkPos]);
		// 	chunks[chunkPos].GetParent().RemoveChild(chunks[chunkPos]);
		// 	chunks.Remove(chunkPos);
		// }
	}
	void ProcessChunkUnloadingQueue()
	{
	// Check if there are chunks in the unload queue
		if (chunkUnloadQueue.Count > 0) 
		{
			unloadFrameCounter++;
			if (unloadFrameCounter % unloadInterval == 0) 
			{
				int chunksToProcess = Mathf.Min(chunksPerFrameUnloading, chunkUnloadQueue.Count);
				for (int i = 0; i < chunksToProcess; i++) 
				{
					Vector3 chunkPosition = chunkUnloadQueue.Dequeue();
					Chunk chunkToUnload = GetChunkAt(chunkPosition);
					if (chunkToUnload != null) 
					{
						ReturnChunk(chunkToUnload);
						chunks[chunkPosition].GetParent().RemoveChild(chunks[chunkPosition]);
						chunks.Remove(chunkPosition); // Remove the chunk from the active chunks dictionary
					}
				}
			}
		}
	}
	// public override void _EnterTree()
	// {
	//     foreach(var chunkPosition in chunks.Keys)
	// 	{
	// 		var thread = chunks[chunkPosition].thread;
	// 		if(thread != null)
	// 		{
				
	// 		}
	// 	}
	// }
}
