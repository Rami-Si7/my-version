using Godot;
using System;

// [Tool]
public partial class Voxel : Resource
{
	public Vector3 position;
	public VoxelType type; // Using the VoxelType enum
	public enum VoxelType
	{
		Air,    // Represents empty space
		Stone,  // Represents stone block
		// Add more types as needed
	}
	public Boolean isActive;

	public Voxel(Vector3 position, VoxelType type, Boolean isActive)
	{
		this.position = position;
		this.type = type;
		this.isActive = isActive;
	}
}
