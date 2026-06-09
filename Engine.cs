using GAME.CORE;
using GAME.TABLE;
using GAME.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Engine : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Engine()
    {
        _graphics = new GraphicsDeviceManager(this);
        //_graphics.IsFullScreen = true;
        var screen = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

        _graphics.PreferredBackBufferWidth = screen.Width;
        _graphics.PreferredBackBufferHeight = screen.Height;
        Window.AllowUserResizing = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        GameRenderer.InitializeRenderer(_spriteBatch, GraphicsDevice);
        GameRenderer.SetFont(Content.Load<SpriteFont>("Fonts/GameFont"));
        GameTable.Initialize();
        GameMap.Initialize();
        GameDeck.Initialize();

        //TESTS

        //TESTS

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        //RESETS
        GameMouse.SetStateDefault();
        //RESETS

        GameRenderer.Update();
        GameRenderer.UpdateFromList(GameDeck.GetDeck());

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GameRenderer.Render();
        GameRenderer.RenderFromList(GameMap.GetMap());
        GameRenderer.RenderFromList(GameTable.GetTable());
        GameRenderer.RenderFromList(GameDeck.GetDeck());

        base.Draw(gameTime);
    }
}
