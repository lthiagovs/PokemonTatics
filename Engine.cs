using GAME.CORE;
using GAME.TABLE;
using GAME.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

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


        GameMusic.MAIN = Content.Load<Song>("Songs/MAIN");
        GameMusic.TITLE = Content.Load<Song>("Songs/TITLE");
        GameMusic.VICTORY = Content.Load<SoundEffect>("Sounds/VICTORY");
        GameMusic.LEVEL_UP = Content.Load<SoundEffect>("Sounds/LEVEL");
        GameMusic.HIT = Content.Load<SoundEffect>("Sounds/HIT");
        
        GameRenderer.InitializeRenderer(_spriteBatch, GraphicsDevice, Content);
        GameRenderer.SetFont(Content.Load<SpriteFont>("Fonts/GameFont"));
        GameTable.Initialize();
        GameMap.Initialize();
        GameDeck.GetRandomDecks();
        GameDeck.Initialize();
        GameTable.InitializeEnemyTeam();
        TitleScreen.Initialize();
        GameBonus.Initialize();

        //TESTS

        //TESTS

    }

    protected override void Update(GameTime gameTime)
    {

        GameTimeLogic.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();


        if (GameGlobals.STATE == GameState.TITLE) GameRenderer.Update(TitleScreen.TITLE_ELEMENTS);
        GameMouse.SetStateDefault();
        GameRenderer.Update(GameTable.GetTable());
        GameRenderer.Update(GameDeck.GetDeck());
        //GameRenderer.Update(GameTableElement.GetTableElements());
        GameMouse.Update();
        GameDeck.Update();
        GameEffect.UpdateAll();
        GameRenderer.Update(GameBonus.GetBonusElements());
        GameBonus.Update();
        
        //LOGIC
        GameTableLogic.Update();
    

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if(GameGlobals.STATE == GameState.TITLE) { GameRenderer.Render(TitleScreen.TITLE_ELEMENTS); }
        else{
            GameMusic.PlayMain();
            GameRenderer.Render(GameMap.GetMap());
            GameRenderer.Render(GameTable.GetTable());
            GameRenderer.Render(GameDeck.GetDeck());
            GameRenderer.Render(GameTableElement.GetTableElements());
            GameRenderer.Render(GameMouse.GetCarryElement());
            GameRenderer.RenderEffects();
            GameRenderer.Render(GameBonus.GetBonusElements());
        }

        base.Draw(gameTime);
    }
}
