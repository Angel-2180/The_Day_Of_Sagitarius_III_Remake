using Godot;
using System;

public partial class ShipSpawner : MultiplayerSpawner
{
	// Called when the node enters the scene tree for the first time.

	public static ShipSpawner Instance;


	public override void _Ready()
	{
		Instance = this;
		
		SpawnFunction =  new Callable(this, MethodName.SpawnShip);
	}

	private Node SpawnShip(Godot.Collections.Array SpawnData)
	{
		Ship ship = ResourceLoader.Load<PackedScene>(GetSpawnableScene(0)).Instantiate<Ship>();
		ship.Position = (Vector2)SpawnData[0];
		ship.ID = (int)SpawnData[1];
		ship.Team = (GameManager.Team)(int)SpawnData[2];
		ship.SetFleetSize((int)SpawnData[3]);
		return ship;
	}

}
