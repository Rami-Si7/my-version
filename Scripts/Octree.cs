using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class Voxel : Resource
{
	public Vector3 position;
	public VoxelType type; // Using the VoxelType enum
	public enum VoxelType
	{
		Air,    // Represents empty space
		Stone,  // Represents stone block
		// Add more types as needed
		Grass,
	}
	public Boolean isActive;

	public Octree octree;

	public Voxel()
	{
		
	}
	public Voxel(Vector3 position, VoxelType type, Boolean isActive, Vector3 octreePosition)
	{
		this.position = position;
		this.type = type;
		this.isActive = isActive;
		this.octree = new Octree(octreePosition, 0, true, type);
	}
}
