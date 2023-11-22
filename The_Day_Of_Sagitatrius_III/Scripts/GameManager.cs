using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Export]
    public Godot.Collections.Dictionary<string, string> PlayerInfos = new();

    [Export]
    public Godot.Collections.Dictionary<int, Godot.Collections.Dictionary<string, string>> PlayerIDs = new ();

	static public GameManager Instance { get; private set; }
    public enum Team
	{
		Red,
		Blue,
		Neutral
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Instance != null)
		{
			GD.Print("GameManager already exist");
			return;
		}
		Instance = this;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
