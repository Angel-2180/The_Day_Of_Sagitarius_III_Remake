using Godot;
using System;

public partial class PlayerSpawner : MultiplayerSpawner
{
	public static PlayerSpawner Instance;
	public override void _Ready()
	{
		Instance = this;
		SpawnFunction = new Callable(this, MethodName.SpawnPlayer);
	}

	private Node SpawnPlayer(Godot.Collections.Array SpawnData)
	{
		Player player = ResourceLoader.Load<PackedScene>(GetSpawnableScene(0)).Instantiate<Player>();
		player.Position = (Vector2)SpawnData[0];
		player.PlayerID = (int)SpawnData[1];
		player.team = (GameManager.Team)(int)SpawnData[2];
		return player;
	}
}
