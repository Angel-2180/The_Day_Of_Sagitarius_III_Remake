using Godot;
using System;

public partial class PlayerCamera : Camera2D
{

	[Export]
	Ship player;

	[Export]
	public float Speed = 200.0f;
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

		if (Input.IsActionPressed("right"))
		{
			Position += Vector2.Right * Speed * (float)delta;
		}
		if (Input.IsActionPressed("left"))
		{
			Position += Vector2.Left * Speed * (float)delta;
		}
		if (Input.IsActionPressed("up"))
		{
			Position += Vector2.Up * Speed * (float)delta;
		}
		if (Input.IsActionPressed("down"))
		{
			Position += Vector2.Down * Speed * (float)delta;
		}
		if (Input.IsActionJustPressed("zoom_in"))
		{
			if (Zoom.X < 1.5 && Zoom.Y < 1.5)
			Zoom += new Vector2(0.1f, 0.1f);
		}
		if (Input.IsActionJustPressed("zoom_out"))
		{
			if (Zoom.X >0.5f && Zoom.Y > 0.5f )
				Zoom -= new Vector2(0.1f, 0.1f);
		}
		

	}
}
