using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 10.0f;       // Horizontal movement speed
	public const float FlySpeed = 10.0f;   // Vertical movement speed
	public const float LookSensitivity = 0.1f; // Mouse look sensitivity

	private Vector3 _velocity = Vector3.Zero;  // Player's velocity
	private Vector2 _lookDelta = Vector2.Zero; // For mouse movement tracking

	private Transform3D playerTransform;
	[Export] private RayCast3D rayCast3D;
	[Export] private MeshInstance3D blockHighlight;

	[Export] private MeshInstance3D dot;


	public override void _Ready()
	{
		// Capture the mouse for first-person controls
		Input.MouseMode = Input.MouseModeEnum.Captured;
		playerTransform = GlobalTransform;
		dot.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		// Handle mouse motion for looking around
		if (@event is InputEventMouseMotion mouseMotion)
		{
			_lookDelta = mouseMotion.Relative * LookSensitivity;
		}

		// Toggle mouse capture with Escape
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement(delta);
		HandleLook();
	}

	private void HandleMovement(double delta)
	{
		// Capture player input for movement
		Vector3 direction = Vector3.Zero;

		// Horizontal movement (WASD or arrow keys)
		if (Input.IsActionPressed("ui_up")) // Move forward
			direction -= Transform.Basis.Z;
		if (Input.IsActionPressed("ui_down")) // Move backward
			direction += Transform.Basis.Z;
		if (Input.IsActionPressed("ui_left")) // Move left
			direction -= Transform.Basis.X;
		if (Input.IsActionPressed("ui_right")) // Move right
			direction += Transform.Basis.X;

		// Vertical movement (Space and Shift)
		if (Input.IsActionPressed("move_up")) // Ascend
			direction += Vector3.Up;
		if (Input.IsActionPressed("move_down")) // Descend
			direction -= Vector3.Up;


		// Normalize the direction vector to ensure consistent speed
		direction = direction.Normalized();

		// Apply movement speed
		_velocity = direction * Speed;

		// Move the player
		Velocity = _velocity;
		MoveAndSlide();
	}

	private void HandleLook()
	{
		// Rotate the player left/right (yaw) based on mouse X movement
		RotateY(-Mathf.DegToRad(_lookDelta.X));

		// Rotate the camera up/down (pitch) based on mouse Y movement
		Node3D camera = GetNode<Node3D>("Camera3D");
		if (camera != null)
		{
			float rotationX = camera.RotationDegrees.X - _lookDelta.Y;
			rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f); // Prevent looking too far up or down
			camera.RotationDegrees = new Vector3(rotationX, camera.RotationDegrees.Y, camera.RotationDegrees.Z);
		}

		// Reset look delta
		_lookDelta = Vector2.Zero;
	}
	public Vector3 getPlayerPosition()
	{
		return GlobalPosition;
	}
	public override void _Process(double delta)
	{

		// Get the collision point
		Vector3 collisionPoint = rayCast3D.GetCollisionPoint();

		// Update the position of the dot
		dot.GlobalTransform = new Transform3D(dot.GlobalTransform.Basis, collisionPoint);

		// Make the dot visible
		dot.Visible = true;
		
		if(rayCast3D.IsColliding() && rayCast3D.GetCollider() is Chunk chunk)
		{
			// GD.Print("COLLISON DETECTED at position: ", rayCast3D.GetCollisionPoint());
			blockHighlight.Visible = true;
			var blockPosition = rayCast3D.GetCollisionPoint() - 0.5f * rayCast3D.GetCollisionNormal();
			var intBlockPosition = new Vector3I(Mathf.FloorToInt(blockPosition.X), Mathf.FloorToInt(blockPosition.Y), Mathf.FloorToInt(blockPosition.Z));
			blockHighlight.GlobalPosition = intBlockPosition + new Vector3(0.5f, 0.5f, 0.5f);
			blockHighlight.GlobalRotation = Vector3.Zero;

			if (Input.IsActionJustPressed("break"))
			{
				GD.Print("IS BREAKING!");
						// Find the chunk containing the block
				Chunk _chunk = World.Instance.GetChunkAt(blockPosition);

				if (_chunk != null)
				{
					// Calculate the local position within the chunk
					Vector3 localPosition = blockPosition - chunk.GlobalTransform.Origin;
					GD.Print("position od breaking block", localPosition);

					// Delete the block (mark it as Air/Empty in your voxel array)
					_chunk.BreakBlock(localPosition);
					
				}
			}
			if (Input.IsActionJustPressed("build"))
			{
				Chunk _chunk = World.Instance.GetChunkAt(blockPosition);

				if (_chunk != null)
				{
					// Calculate the local position within the chunk
					Vector3 localPosition = blockPosition - chunk.GlobalTransform.Origin + rayCast3D.GetCollisionNormal();
					GD.Print("place");
					// Delete the block (mark it as Air/Empty in your voxel array)
					_chunk.BuildBlock(localPosition);
				}
			}

		}
		else
		{
			// GD.Print("IN HERE");
			blockHighlight.Visible = false;
		}
	}
}
