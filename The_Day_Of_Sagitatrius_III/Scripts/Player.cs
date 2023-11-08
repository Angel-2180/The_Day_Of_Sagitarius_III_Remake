using Godot;
using System;

public partial class Player : Node2D
{
	#region Nodes
	[Export]
	public Camera2D camera;

	[Export]
	private Button _SplitButton;

	[Export]
	private Button _MergeButton;

	[Export]
	private CanvasLayer _canvasLayer;
	#endregion

	public bool IsDragging = false;

	public Vector2 DragStart = Vector2.Zero;
	public RectangleShape2D SelectionBox = new ();
	private Godot.Collections.Array<Ship> _selectedShips = new ();

	[Export]
	public int PlayerID;


	[Export]
	private int _maxMergeDistance = 500;

	[Export]
	private int _maxNumberOfShips = 20;

	private int _numberOfShips = 1;


	private Color _color = Colors.Yellow;

	enum PlayerState
	{
		Idle,
		AwaitSplitting,
		AwaitMerge
	}

	private PlayerState _state = PlayerState.Idle;

	[Export]
	private PackedScene firstShip;

	public override void _EnterTree()
    {
        SetMultiplayerAuthority(PlayerID);

		var newShip = firstShip.Instantiate() as Ship;
		newShip.ID = PlayerID;

		AddChild(newShip);
    }


    public override void _Ready()
    {
		if (!IsMultiplayerAuthority())
		{
			camera.Enabled = false;
			_canvasLayer.Visible = false;
			SetProcess(false);
			SetPhysicsProcess(false);
			SetProcessInput(false);
		}
		if (_SplitButton != null && _MergeButton != null)
		{
			_SplitButton.Pressed += OnSplitButtonPressed;
			_MergeButton.Pressed += OnMergeButtonPressed;
		}
    }

    public void SelectShip()
	{
		var DragEnd = GetGlobalMousePosition();
		SelectionBox.Size = (DragEnd - DragStart).Abs();
		var query = new PhysicsShapeQueryParameters2D();
		var space = GetWorld2D().DirectSpaceState;
		query.Shape = SelectionBox;
		var mask = 1 << 1;
		query.CollisionMask = (uint)mask;			
		query.Transform = new Transform2D(0, (DragEnd + DragStart) / 2);
		var Selected = space.IntersectShape(query);
		foreach (var item in Selected)
		{
			if (item["collider"].As<Node>() is Ship ship)
			{
				if (ship.ID == PlayerID)
				{
					ship.SetSelected(true);
					if (_state == PlayerState.AwaitSplitting && _numberOfShips < _maxNumberOfShips)
					{
						ship.Rpc(nameof(ship.Split), ship.FleetSize / 2);
						_numberOfShips++;
						_state = PlayerState.Idle;
						_color = Colors.Yellow;
					}
					else if (_state == PlayerState.AwaitMerge && Selected.Count > 1)
					{
						Rpc(nameof(Merge)); 
						_state = PlayerState.Idle;
						_color = Colors.Yellow;
					}
					_selectedShips.Add(ship);
				}

			}
		}
	}

	public bool CheckForEnemy()
	{
		var spaceState = GetWorld2D().DirectSpaceState;
		var query = new PhysicsPointQueryParameters2D();
		query.Position = GetGlobalMousePosition();
		var result = spaceState.IntersectPoint(query);

		if (result.Count > 0)
		{
			var item = result[0]["collider"].As<Node>();
			if (item is Ship ship)
			{
				if (ship.ID != PlayerID)
				{
					foreach (var selected in _selectedShips)
					{
						selected.Rpc(nameof(selected.SetTarget), ship.GetPath());
					}
					return true;
				}
			}
		}

		return false;
	}

	private bool clicked = false;
	private float time;

	public void OnGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed)
				{
					clicked = true;
					time = 0;
					DragStart = GetGlobalMousePosition();
				}
				else
				{
					clicked = false;
					if (time < 0.2f)
					{
						if (!CheckForEnemy())
						{
							foreach (var ship in _selectedShips)
							{
								ship.Rpc(nameof(ship.SetDestination), GetGlobalMousePosition());
								if (ship.Target != null) ship.Rpc(nameof(ship.SetTarget), new NodePath());
								ship.SetSelected(false);
							}
							_selectedShips.Clear();
						}
					}
					else
					{
						IsDragging = false;
						SelectShip();	
					}
					QueueRedraw();

				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				if (mouseButton.Pressed)
				{
					foreach (var ship in _selectedShips)
					{
						ship.SetSelected(false);
					}
					_selectedShips.Clear();
				}
			}
		}
		else if (@event is InputEventMouseMotion mouseMotion)
		{
			if (clicked)
			{
				IsDragging = true;
				QueueRedraw();	
			}
		}

	}

	public override void _Draw()
	{
		if (IsDragging && clicked)
		{
			var localDragStart = ToLocal(DragStart);
			var localDragEnd = ToLocal(GetGlobalMousePosition());
			DrawRect(new Rect2(localDragStart, localDragEnd - localDragStart), _color, false);
		}
	}

	void OnSplitButtonPressed()
	{
		if (_state == PlayerState.AwaitSplitting)
		{
			_state = PlayerState.Idle;
			_color = Colors.Yellow;
			return;
		}
		_state = PlayerState.AwaitSplitting;
		_color = Colors.Red;
		if (_selectedShips.Count > 0 && _numberOfShips < _maxNumberOfShips)
		{
			foreach (var ship in _selectedShips)
			{
				if (_numberOfShips >= _maxNumberOfShips) break;
				ship.SetSelected(false);
				ship.Rpc(nameof(ship.Split), ship.FleetSize / 2);
				_numberOfShips++;
			}
			_state = PlayerState.Idle;
			_selectedShips.Clear();
			_color = Colors.Yellow;
		}	
	}

	void OnMergeButtonPressed()
	{
		if (_state == PlayerState.AwaitMerge)
		{
			_state = PlayerState.Idle;
			_color = Colors.Yellow;
			return;
		}
		_state = PlayerState.AwaitMerge;
		_color = Colors.Green;

		if (_selectedShips.Count > 0)
		{
			foreach (var ship in _selectedShips)
			{
				ship.SetSelected(false);
				Rpc(nameof(Merge));
			}
			_selectedShips.Clear();
			_state = PlayerState.Idle;
			_color = Colors.Yellow;
			_numberOfShips--;
		}
	
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Merge()
	{
		int totalShips = 0;
		Ship firstShip = _selectedShips[0];
		foreach (var ship in _selectedShips)
		{	
			if (ship.ID == PlayerID && ship != firstShip && ship.GlobalPosition.DistanceTo(firstShip.GlobalPosition) < _maxMergeDistance)
			{
				totalShips += ship.FleetSize;
				_selectedShips.Remove(ship);
				ship.QueueFree();
			}
		}
		firstShip.Rpc(nameof(firstShip.SetFleetSize), firstShip.FleetSize + totalShips);
		_numberOfShips--;
	}

    public override void _PhysicsProcess(double delta)
    {
        if (clicked)
		{
			time += (float)delta;
		}
    }
}


