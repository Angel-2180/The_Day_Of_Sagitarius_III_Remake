using Godot;

public partial class Ship : CharacterBody2D
{
    [Export]
    private PackedScene _bulletScene;

    [Export]
    public Node2D CanonPosition;

    [Export]
    public Node2D UiOffsetNode;

    [Export]
    public Area2D Detect;

    [Export]
    public Sprite2D sprite;

    [Export]
    private Label _shipNumberLabel;

    [Export]
    private PointLight2D _light;

    [Export]
    private Area2D _lightDetectorArea;

    public Vector2 mousePos = Vector2.Zero;

    #region Variables

    public Vector2 TargetPosition = Vector2.Zero;

    public Vector2 LookAtPosition = Vector2.Zero;

    public bool IsSelected;

    [Export]
    public float Speed = 200.0f;

    [Export]
    public float RotationSpeed = 2f;

    [Export]
    public int FleetSize = 10000;

    public Vector2 UiOffset => UiOffsetNode.Position;

    public Vector2 AvoidanceForce = Vector2.Zero;

    public float AvoidanceRadius = 50;

    public float AvoidanceWeight = 0.2f;

    #endregion Variables

    public bool IsMoveSelected = false;

    public bool IsLookAtSelected = false;

    [Export]
    public int ID = 0;

    public Ship Target = null;

    public bool CanShoot = true;

    [Export]
    private Timer _shootDelay;

    public GameManager.Team Team = GameManager.Team.Neutral;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(ID);
        Name = (ID + GetParent().GetChildCount()).ToString();
    }

    public override void _Ready()
    {
        if (!GetParent().IsMultiplayerAuthority())
        {
            SetProcess(false);
            SetPhysicsProcess(false);
            SetProcessInput(false);
        }

        _shootDelay.Timeout += () => CanShoot = true;
        sprite.Material = sprite.Material.Duplicate() as ShaderMaterial;
        _shipNumberLabel.Text = FleetSize.ToString();

        AddToGroup("Ship");
        GetTree().NotifyGroup("Player", 0);
        (sprite.Material as ShaderMaterial).SetShaderParameter("player_pos", GlobalPosition);

    }
    
    public void SetLightVisible(int team)
    {
        GD.Print("Team: " + (GameManager.Team)team);
        if (Team == (GameManager.Team)team)
        {
            _light.Visible = true;
        }
        else
        {
            _light.Visible = false;
        }
    }
 
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selected)
        {
            (sprite.Material as ShaderMaterial).SetShaderParameter("width", 10);
        }
        else
        {
            (sprite.Material as ShaderMaterial).SetShaderParameter("width", 0);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetFleetSize(int newFleetSize)
    {
        FleetSize = newFleetSize;
        _shipNumberLabel.Text = FleetSize.ToString();
    }

    public override void _PhysicsProcess(double delta)
    {
        _shipNumberLabel.GlobalPosition = GlobalPosition + UiOffset;
        if (IsInstanceValid(Target) && Target.FleetSize > 0)
        {
            LookAtPosition = Target.GlobalPosition;
            if (CanShoot && RotateToTarget(LookAtPosition, delta))
            {
                CanShoot = false;
                _shootDelay.Start();
                Rpc(MethodName.Shoot);
            }
        }
        LookAtPosition = LookAtPosition == Vector2.Zero ? TargetPosition : LookAtPosition;
        if (RotateToTarget(LookAtPosition, delta))
        {
            Move((float)delta);
        }
    }

    public void Move(float delta)
    {
        Velocity = Vector2.Zero;
        if (TargetPosition != Vector2.Zero)
        {
            Velocity = GlobalPosition.DirectionTo(TargetPosition);
            if (GlobalPosition.DistanceTo(TargetPosition) < AvoidanceRadius)
            {
                TargetPosition = Vector2.Zero;
            }
            // EmitSignal(Main.SignalName.OnShipMove, GlobalPosition / Main.GRID_SIZE);
        }
        AvoidanceForce = Avoid();
        Velocity = (Velocity + AvoidanceForce * AvoidanceWeight).Normalized() * Speed;
        MoveAndCollide(Velocity * delta);

        (sprite.Material as ShaderMaterial).SetShaderParameter("player_pos", GlobalPosition);
        //EmitSignal(Main.SignalName.OnShipMove, GlobalPosition / Main.GRID_SIZE);
        
    }

    public Vector2 Avoid()
    {
        var result = Vector2.Zero;
        var neighbors = Detect.GetOverlappingBodies();

        if (neighbors.Count > 0)
        {
            foreach (var neighbor in neighbors)
            {
                result += neighbor.GlobalPosition.DirectionTo(GlobalPosition);
            }
            result /= neighbors.Count;
        }
        return result.Normalized();
    }

    public bool RotateToTarget(Vector2 target, double delta)
    {
        if (target == Vector2.Zero)
        {
            return true;
        }
        var direction = (target - GlobalPosition).Normalized();
        var AngleTo = Transform.X.AngleTo(direction);
        Rotate(Mathf.Sign(AngleTo) * Mathf.Min((float)delta * RotationSpeed, Mathf.Abs(AngleTo)));
        if (Mathf.Abs(AngleTo) < 0.01f)
        {
            LookAtPosition = Vector2.Zero;
            return true;
        }
        return false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Shoot()
    {
        var bullet = _bulletScene.Instantiate() as Bullet;
        bullet.ID = ID;
        GetParent().AddChild(bullet);
        bullet.GlobalPosition = CanonPosition.GlobalPosition;
        bullet.Rotation = Rotation;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetTarget(NodePath target)
    {
        Ship targetShip = GetTree().Root.GetNodeOrNull<Ship>(target);
        Target = targetShip;
        if (Target != null)
        {
            LookAtPosition = Target.GlobalPosition;
            TargetPosition = Vector2.Zero;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Split(int numberOfShipsToRemove)
    {
        if (numberOfShipsToRemove > FleetSize)
        {
            GD.Print("Not enough ships to split");
            return;
        }

        if (numberOfShipsToRemove <= 0)
        {
            GD.Print("Can't split 0 ships");
            return;
        }

        SetFleetSize(FleetSize - numberOfShipsToRemove);

        var newPlayer = GD.Load<PackedScene>("res://Scenes/ShipBody.tscn").Instantiate() as Ship;
        newPlayer.ID = ID;
        newPlayer.Team = Team;
        newPlayer.SetFleetSize(numberOfShipsToRemove);

        //random position around the ship
        newPlayer.Position = Position + new Vector2(GD.Randf() * 100 - 50, GD.Randf() * 100 - 50);
        GetParent().AddChild(newPlayer);
    }

    public void TakeDamage(float damagePower)
    {
        FleetSize -= (int)damagePower;
        _shipNumberLabel.Text = FleetSize.ToString();
        if (FleetSize <= 0)
        {
            Rpc(MethodName.Kill);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Kill()
    {
        QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetDestination(Vector2 destination)
    {
        TargetPosition = destination;
    }

    public void OnAreaEntered(Area2D area)
    {
        //disable light when near allies but make sure that there is at least one light still visible 
        

    }

    /*// In your Ship class
private List<Ship> _nearbyAllies = new List<Ship>();

public void UpdateNearbyAllies(List<Ship> allShips)
{
    _nearbyAllies.Clear();
    foreach (var ship in allShips)
    {
        if (ship.Team == Team && ship != this && (ship.Position - Position).Length() < AllyDetectionRadius)
        {
            _nearbyAllies.Add(ship);
        }
    }
    UpdateLight();
}

private void UpdateLight()
{
    if (_nearbyAllies.Count > 0)
    {
        // Disable the light if there are any nearby allies
        _light.Visible = false;
    }
    else
    {
        // Enable the light if there are no nearby allies
        _light.Visible = true;
    }
}

// In your GameManager class
public void UpdateAllShips()
{
    List<Ship> allShips = GetAllShips();
    foreach (var ship in allShips)
    {
        ship.UpdateNearbyAllies(allShips);
    }
    EnsureAtLeastOneLight(allShips);
}

private void EnsureAtLeastOneLight(List<Ship> allShips)
{
    if (!allShips.Any(ship => ship.LightVisible))
    {
        // If no lights are visible, enable the light of the ship with the most allies nearby
        Ship shipWithMostAllies = allShips.OrderByDescending(ship => ship.NearbyAlliesCount).First();
        shipWithMostAllies.LightVisible = true;
    }
}*/
}