using Godot;

public partial class Main : Node2D
{
    // private readonly Texture2D _lightTexture = ResourceLoader.Load("res://Arts/Light.png") as Texture2D;
    // public const int GRID_SIZE = 16;
    // private Sprite2D _fogOfWar = null;
    // private int _width = 0;
    // private int _height = 0;
    // private Image _fogOfWarImage = new();
    // private Image _lightImage = new();
    // private ImageTexture _fogOfWarTexture = new();
    // private Vector2 _lightPosition = new Vector2(0, 0);

    // [Signal]
    // public delegate void OnShipMoveEventHandler(Vector2 newGridPos);

    // public override void _Ready()
    // {
    //     _fogOfWar = GetNodeOrNull<Sprite2D>("Fog");
    //     _width = 4000;
    //     _height = 4000;
    //     if (_lightTexture != null && _fogOfWar != null)
    //     {
    //         _lightImage = _lightTexture.GetImage();
    //     }
    //     else
    //     {
    //         GD.Print("Not found");
    //     }
    //     _lightPosition = new Vector2(_lightTexture.GetWidth() / 2, _lightTexture.GetHeight() / 2);
    //     var fog_image_width = _width / GRID_SIZE;
    //     var fog_image_height = _height / GRID_SIZE;
    //     _fogOfWarImage = Image.Create(fog_image_width, fog_image_height, false, Image.Format.Rgbah);
    //     _fogOfWarImage.Fill(Colors.Black);
    //     _lightImage.Convert(Image.Format.Rgbah);
    //     _fogOfWar.Scale *= GRID_SIZE;
    //     //_fogOfWar.Position = new Vector2(-_width / 2, -_height / 2);

    //     OnShipMove += UpdateFogOfWar;

    //     UpdateFogOfWar(Vector2.Zero);

    // }

    // public void UpdateFogOfWar(Vector2 newGridPos)
    // {
    //     var lightRect = new Rect2I(Vector2I.Zero, new Vector2I(_lightImage.GetWidth(), _lightImage.GetHeight()));
    //     Vector2I fogRectPos = new Vector2I((int)(newGridPos.X - _lightPosition.X), (int)(newGridPos.Y - _lightPosition.Y));
    //     _fogOfWarImage.BlendRect(_lightImage, lightRect, fogRectPos);

    //     UpdateFogOfWarTexture();
    // }

    // private void UpdateFogOfWarTexture()
    // {
    //     if (_fogOfWarTexture != null)
    //     {
    //         _fogOfWarTexture = ImageTexture.CreateFromImage(_fogOfWarImage);
    //         _fogOfWar.Texture = _fogOfWarTexture;
    //     }
    // }

    // public override void _Input(InputEvent @event)
    // {
    //     UpdateFogOfWar(GetGlobalMousePosition() / GRID_SIZE);
    // }

}