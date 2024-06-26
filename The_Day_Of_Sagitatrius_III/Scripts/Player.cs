using System.Runtime.Serialization.Formatters.Binary;
using Godot;

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

    #endregion Nodes

    public bool IsDragging = false;

    public Vector2 DragStart = Vector2.Zero;
    public RectangleShape2D SelectionBox = new();
    private Godot.Collections.Array<Ship> _selectedShips = new();

    [Export]
    public int PlayerID;

    [Export]
    private int _maxMergeDistance = 500;

    [Export]
    private int _maxNumberOfShips = 20;

    private int _numberOfShips = 1;

    private Color _color = Colors.Yellow;

    public GameManager.Team team = GameManager.Team.Neutral;

    private enum PlayerState
    {
        Idle,
        AwaitSplitting,
        AwaitMerge
    }

    private PlayerState _state = PlayerState.Idle;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(PlayerID); 
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
                        ship.RpcId(1, Ship.MethodName.Split, ship.FleetSize / 2);
                        _numberOfShips++;
                        _state = PlayerState.Idle;
                        _color = Colors.Yellow;
                    }
                    else if (_state == PlayerState.AwaitMerge && Selected.Count > 1)
                    {
                        Merge();
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
                if (ship.ID != PlayerID && team != ship.Team)
                {
                    foreach (var selected in _selectedShips)
                    {
                        selected.Rpc(Ship.MethodName.SetTarget, ship.GetPath());
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
                                ship.Rpc(Ship.MethodName.SetDestination, GetGlobalMousePosition());
                                if (ship.Target != null) ship.Rpc(Ship.MethodName.SetTarget, new NodePath());
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

    private void OnSplitButtonPressed()
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
                ship.RpcId(1, Ship.MethodName.Split, ship.FleetSize / 2);
                _numberOfShips++;
            }
            _state = PlayerState.Idle;
            _selectedShips.Clear();
            _color = Colors.Yellow;
        }
    }

    private void OnMergeButtonPressed()
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
            Merge();
            _selectedShips.Clear();
            _state = PlayerState.Idle;
            _color = Colors.Yellow;
        }
    }

   
    public void Merge()
    {
        int totalShips = 0;
        Ship firstShip = _selectedShips[0];
        for (int index = _selectedShips.Count - 1; index >= 0; index--)
        {
            Ship ship = _selectedShips[index];
            if (firstShip != ship && firstShip.GlobalPosition.DistanceTo(ship.GlobalPosition) < _maxMergeDistance)
            {
                totalShips += ship.FleetSize;
                _selectedShips.RemoveAt(index);
                ship.Rpc(Ship.MethodName.Kill);
                _numberOfShips--;
            }
        }
        firstShip.Rpc(Ship.MethodName.SetFleetSize, firstShip.FleetSize + totalShips);
        firstShip.SetSelected(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (clicked)
        {
            time += (float)delta;
        }
    }
}