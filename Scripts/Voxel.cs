using Godot;
using System;

// [Tool]
public partial class Voxel : Resource
{
	public Vector3 position;
	public Color color;
	public Boolean isActive;

	public Voxel(Vector3 position, Color color, Boolean isActive)
	{
		this.position = position;
		this.color = color;
		this.isActive = isActive;
	}
}
