using Godot;
using System;



public partial class Bullet : Area2D
{
	public int ID = 0;

	public float Speed = 1000;

	public float BulletPower = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Move the bullet forward
		Position += Transform.X.Normalized() * Speed * (float)delta;
	}

	private void OnExitedScreen() //TODO: change to out of bounds
	{
		QueueFree();
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Ship ship)
		{
			if (ID != ship.ID)
			{
				ship.TakeDamage(BulletPower);
				QueueFree();
			}
		}
	}


}
