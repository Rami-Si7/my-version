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

	public override void _Ready()
	{
		// Capture the mouse for first-person controls
		Input.MouseMode = Input.MouseModeEnum.Captured;
		playerTransform = GlobalTransform;
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
}
