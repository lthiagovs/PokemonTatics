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

        //TitleScreen.Initialize();
        GameRenderer.InitializeRenderer(_spriteBatch, GraphicsDevice, Content);
        GameRenderer.SetFont(Content.Load<SpriteFont>("Fonts/GameFont"));
        GameTable.Initialize();
        GameMap.Initialize();
        GameDeck.GetRandomDecks();
        GameDeck.Initialize();
        GameTable.InitializeEnemyTeam();

        //TESTS

        //TESTS

    }

    protected override void Update(GameTime gameTime)
    {

        GameTimeLogic.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        GameMouse.SetStateDefault();
        GameRenderer.Update(GameTable.GetTable());
        GameRenderer.Update(GameDeck.GetDeck());
        //GameRenderer.Update(GameTableElement.GetTableElements());
        GameMouse.Update();
        GameDeck.Update();
        
        //LOGIC
        GameTableLogic.Update();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GameRenderer.Render(GameMap.GetMap());
        GameRenderer.Render(GameTable.GetTable());
        GameRenderer.Render(GameDeck.GetDeck());
        GameRenderer.Render(GameTableElement.GetTableElements());
        GameRenderer.Render(GameMouse.GetCarryElement());

        base.Draw(gameTime);
    }
}
