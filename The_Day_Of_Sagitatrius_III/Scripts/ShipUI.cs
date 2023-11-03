using Godot;
using System;

public partial class ShipUI : Control
{

	#region Signals
	[Signal]
	public delegate void AttackButtonPressedEventHandler();

	[Signal]
	public delegate void MoveButtonPressedEventHandler();

	[Signal]
	public delegate void SplitButtonPressedEventHandler(string line);

	[Signal]
	public delegate void LookAtButtonPressedEventHandler();

	#endregion
	
	[Export]
	private Control _InputField;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//make that the rotation of the ui is always the same as the world
		
	}

	private void _OnAttackButtonPressed()
	{
		EmitSignal("AttackButtonPressed");
	}

	private void OnMovePressed()
	{
		EmitSignal("MoveButtonPressed");
	}

	private void OnLookAtPressed()
	{
		EmitSignal("LookAtButtonPressed");
	}

	private void OnSplitPressed()
	{
		if (!_InputField.Visible)
		{
			_InputField.Show();
		}
		else
		{
			_InputField.Hide();
		}
	}

	private void OnLineSubmitted(string line)
	{
		if (line == null || line == "") return;
		EmitSignal("SplitButtonPressed", line);
		_InputField.Hide();
	}

}
