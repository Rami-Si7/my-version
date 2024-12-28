using System;
using System.Collections.Generic;
using System.Linq;
// using System.Numerics;
using System.Threading.Tasks;
using Godot;
[Tool]

public class Octree
{
	
	public Vector3 position;
	public int level;
	public Octree[] children;
	public Octree parent;
	public Voxel voxel;
	public bool isLeaf =  true;
	public MeshInstance3D levelMesh;
	public CollisionShape3D levelCollision;

	public bool alreadyDestroyed = false;

	public Voxel.VoxelType dominantType = Voxel.VoxelType.Air;

	private Dictionary<Vector3, List<Vector3[]>> triangles = new Dictionary<Vector3, List<Vector3[]>>();

	private static Vector3[] _vertices;

	private static readonly int[] _top = new int[] { 2, 3, 7, 6 };
	private static readonly int[] _bottom = new int[] { 0, 4, 5, 1 };
	private static readonly int[] _left = new int[] { 6, 4, 0, 2 };
	private static readonly int[] _right = new int[] { 3, 1, 5, 7 };
	private static readonly int[] _back = new int[] { 7, 5, 4, 6 };
	private static readonly int[] _front = new int[] { 2, 0, 1, 3 };

	int[][] faces = new int[][]
	{
		_top, // Top
		_bottom, // Bottom
		_left, // Left
		_right, // Right
		_front, // Front
		_back  // Back
	};


	public Octree()
	{

	}
	public Octree(Vector3 position, int level, bool isLeaf, Voxel.VoxelType type)
	{
		this.position = position;
		this.level = level;
		this.dominantType = type;
		children = null;
		parent = null;
	}
	public void AddNodes(Vector3 position)
	{
		triangles.Clear(); // clear triangles of parent
		this.isLeaf = false;
		this.alreadyDestroyed = true;
		children = new Octree[8];
		float offSet = (float)Mathf.Pow(0.5, this.level + 1);

		float x = this.position.X;
		float y = this.position.Y;
		float z = this.position.Z;
		// GD.Print("(x,y,z) = ",x, " ", y, " ", z);
		
		for(int i = 0; i < 8; i++)
		{
			children[i] = new Octree();
		}

		children[0].position = new Vector3(x, y, z);
		children[1].position = new Vector3(x + offSet, y, z);
		children[2].position = new Vector3(x, y + offSet, z);
		children[3].position = new Vector3(x + offSet, y + offSet, z);
		children[4].position = new Vector3(x, y, z + offSet);
		children[5].position = new Vector3(x + offSet, y, z + offSet);
		children[6].position = new Vector3(x, y + offSet, z + offSet);
		children[7].position = new Vector3(x + offSet, y + offSet, z + offSet);
		
		foreach(var child in children)
		{
			GD.Print("child position, ", child.position);
			child.level = this.level + 1;
			child.parent = this;
			child.isLeaf = true;
			// GD.Print(child.Contains(position), " position: ", child.position );

			if(child.Contains(position))
			{
				GD.Print("child position, ", child.position);

				child.dominantType = Voxel.VoxelType.Air;
			}
			else
			{
				child.dominantType = Voxel.VoxelType.Stone;
			}
		}
	}

	public void AssembleTriangles(SurfaceTool surfaceTool, int[] face)
	{
		// GD.Print("start assemble");
		var offSet = (float)Mathf.Pow(0.5, this.level);
		float x = this.position.X;
		float y = this.position.Y;
		float z = this.position.Z;
		// GD.Print("childe position", this.position);

		_vertices = new Vector3[]
		{
			new Vector3(x, y, z),
			new Vector3(x + offSet, y, z),
			new Vector3(x, y + offSet, z),
			new Vector3(x + offSet, y + offSet, z),
			new Vector3(x, y, z + offSet),
			new Vector3(x + offSet, y, z + offSet),
			new Vector3(x, y + offSet, z + offSet),
			new Vector3(x + offSet, y + offSet, z + offSet)
		};
		triangles[this.position] = new List<Vector3[]>();

		// foreach (var _face in faces)
		// {
		// 	// GD.Print("start assemble here");
		// 	// Add the two triangles for each face
		// 	var a = _vertices[_face[0]];
		// 	// GD.Print("start assemble here1");
		// 	var b = _vertices[_face[1]];
		// 	// GD.Print("start assemble here2");
		// 	var c = _vertices[_face[2]];
		// 	// GD.Print("start assemble here3");
		// 	var d = _vertices[_face[3]];
		// 	// GD.Print("start assemble here4");

		// 	var triangle1 = new Vector3[] { a, b, c };
		// 	var triangle2 = new Vector3[] { a, c, d};

		// 	triangles[this.position].Add(triangle1);
		// 	// GD.Print("start assemble here5");
		// 	triangles[this.position].Add(triangle2);
		// 	// GD.Print("start assemble here6");

		// 	surfaceTool.AddTriangleFan(triangle1);
		// 	surfaceTool.AddTriangleFan(triangle2);
		// }

		// GD.Print("start assemble here");
		// Add the two triangles for each face
		var a = _vertices[face[0]];
		// GD.Print("start assemble here1");
		var b = _vertices[face[1]];
		// GD.Print("start assemble here2");
		var c = _vertices[face[2]];
		// GD.Print("start assemble here3");
		var d = _vertices[face[3]];
		// GD.Print("start assemble here4");

		var triangle1 = new Vector3[] { a, b, c };
		var triangle2 = new Vector3[] { a, c, d};

		triangles[this.position].Add(triangle1);
		// GD.Print("start assemble here5");
		triangles[this.position].Add(triangle2);
		// GD.Print("start assemble here6");

		surfaceTool.AddTriangleFan(triangle1);
		surfaceTool.AddTriangleFan(triangle2);
	}

	private void checkAllFaces(SurfaceTool surfaceTool)
	{
		if(IsFaceVisible(0))
		{
			// GD.Print("in here 0");
			AssembleTriangles(surfaceTool, _top);
		}
		if(IsFaceVisible(1))
		{
			// GD.Print("in here 1");
			AssembleTriangles(surfaceTool, _bottom);
		}
		if(IsFaceVisible(2))
		{
			// GD.Print("in here 2");
			AssembleTriangles(surfaceTool, _left);
		}
		if(IsFaceVisible(3))
		{
			// GD.Print("in here 3");
			AssembleTriangles(surfaceTool, _right);
		}
		if(IsFaceVisible(4))
		{
			// GD.Print("in here 4");
			AssembleTriangles(surfaceTool, _back);
		}
		if(IsFaceVisible(5))
		{
			// GD.Print("in here 5");
			AssembleTriangles(surfaceTool, _front);
		}
	}
	private bool CheckNeighbourNodes(float x, float y, float z)
	{
		var offSet = (float)Mathf.Pow(0.5, this.level);
		foreach(var child in parent.children)
		{
			if (child != this && child.dominantType != Voxel.VoxelType.Air )
			{
				
				if(Mathf.Abs(x - child.position.X) == 0 && Mathf.Abs(y - child.position.Y) == 0 && Mathf.Abs(z - child.position.Z) == 0)
				{
					// GD.Print("CheckNeighbourNodes");
					return false;
				}
			}
		}
		return true;
	}
	private bool IsFaceVisible(int face)
	{
		float x = position.X;
		float y = position.Y;
		float z = position.Z;
		var offSet = (float)Mathf.Pow(0.5, this.level);
		// GD.Print("offSet: ", offSet);

		switch (face)
		{
			case 0: y += offSet; break; // Top
			case 1: y -= offSet; break; // Bottom
			case 2: x -= offSet; break; // Left
			case 3: x += offSet; break; // Right
			case 4: z += offSet; break; // Front
			
			case 5: z -= offSet; break; // Back
		}
		// GD.Print("IsFaceVisible");
		return CheckNeighbourNodes(x, y, z);
	}
	public void TraverseOctree(SurfaceTool surfaceTool)
	{
		if(this.isLeaf)
		{
			if(this.dominantType != Voxel.VoxelType.Air)
			{
				// GD.Print("is leaf ", this.position);
				// this.AssembleTriangles(surfaceTool, _back);
				checkAllFaces(surfaceTool);
			}
			return;
		}
		else
		{
			// GD.Print("childs");
			foreach(var child in this.children)
			{
				child.TraverseOctree(surfaceTool);
			}
		}
	}
	public Octree LocateLeaf(Vector3 position)
	{
		if(children == null)
		{
			// GD.Print("base case locateleaf");
			return this;
		}
		foreach(var child in this.children)
		{
			// GD.Print(child.Contains(position));
			if(child.Contains(position))
			{
				return child.LocateLeaf(position);
			}
		}

		return null;
	}
	public bool Contains(Vector3 position)
	{
		float offSet = (float)Mathf.Pow(0.5, this.level);
		return position.X >= this.position.X && position.X <= this.position.X + offSet &&
			position.Y >= this.position.Y && position.Y <= this.position.Y + offSet &&
			position.Z >= this.position.Z && position.Z <= this.position.Z + offSet;
	}
}
