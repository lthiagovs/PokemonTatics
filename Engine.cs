using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using PokemonTFT.Core;
using PokemonTFT.Logic;
using PokemonTFT.Screens;
using PokemonTFT.Table;
using PokemonTFT.UI;

namespace PokemonTFT;

public class Engine : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private int _lastWidth;
    private int _lastHeight;

    public Engine()
    {
        _graphics = new GraphicsDeviceManager(this);

        DisplayMode screen = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.PreferredBackBufferWidth  = screen.Width;
        _graphics.PreferredBackBufferHeight = screen.Height;

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        Window.Title = TitleScreen.GAME_TITLE;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        GameMusic.MAIN     = Content.Load<Song>("Songs/MAIN");
        GameMusic.TITLE    = Content.Load<Song>("Songs/TITLE");
        GameMusic.VICTORY  = Content.Load<SoundEffect>("Sounds/VICTORY");
        GameMusic.LEVEL_UP = Content.Load<SoundEffect>("Sounds/LEVEL");
        GameMusic.HIT      = Content.Load<SoundEffect>("Sounds/HIT");

        GameRenderer.InitializeRenderer(_spriteBatch, GraphicsDevice, Content);
        GameRenderer.SetFont(Content.Load<SpriteFont>("Fonts/GameFont"));

        BuildLayout();
        GameTable.InitializeEnemyTeam();
        GameTableLogic.RefreshCaches();
        GameBonus.Refresh();

        _lastWidth  = GameRenderer.GetScreenWidth();
        _lastHeight = GameRenderer.GetScreenHeight();
    }

    private static void BuildLayout()
    {
        GameTable.Initialize();
        GameMap.Initialize();
        GameDeck.Reset();
        TitleScreen.Initialize();
    }

    private void OnClientSizeChanged(object? sender, System.EventArgs e)
    {
        int width  = GraphicsDevice.Viewport.Width;
        int height = GraphicsDevice.Viewport.Height;
        if (width <= 0 || height <= 0) return;
        if (width == _lastWidth && height == _lastHeight) return;

        _lastWidth  = width;
        _lastHeight = height;

        GameTable.Rebuild();
        GameMap.Initialize();
        GameDeck.Initialize();
        TitleScreen.Initialize();
        GameTableLogic.RefreshCaches();
        GameBonus.Refresh();
    }

    protected override void Update(GameTime gameTime)
    {
        GameTimeLogic.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        GameMouse.BeginFrame();
        GameTooltip.BeginFrame();

        if (EvolutionScene.IsActive)
        {
            EvolutionScene.Update();
            GameTooltip.EndFrame();
            GameMouse.EndFrame();
            base.Update(gameTime);
            return;
        }

        if (GameGlobals.STATE == GameState.TITLE)
        {
            GameMusic.PlayTitle();
            GameElement.UpdateAll(TitleScreen.GetElements());
        }
        else
        {
            GameMusic.PlayMain();

            GameTableLogic.RefreshCaches();

            GameElement.UpdateAll(GameTable.TableElements);
            GameElement.UpdateAll(GameDeck.GetDeck());
            GameElement.UpdateAll(GameBonus.GetBonusElements());

            GameDeck.Update();
            GameBonus.Update();
            GameEffect.UpdateAll();
            FloatingText.UpdateAll();
            GameTableLogic.Update();
        }

        GameTooltip.EndFrame();
        GameMouse.EndFrame();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        GameRenderer.Begin();

        if (GameGlobals.STATE == GameState.TITLE)
        {
            GameRenderer.Render(TitleScreen.GetElements());
        }
        else
        {
            GameRenderer.Render(GameMap.GetMap());
            GameRenderer.Render(GameTable.TableElements);
            GameRenderer.Render(GameDeck.GetDeck());
            GameRenderer.Render(GameTableElement.GetTableElements());
            GameRenderer.Render(GameMouse.GetCarry());
            GameRenderer.RenderEffects();
            GameRenderer.RenderFloatingText();
            GameRenderer.Render(GameBonus.GetBonusElements());
            GameRenderer.Render(GameTooltip.GetElement());
        }

        EvolutionScene.Render();

        GameRenderer.End();

        base.Draw(gameTime);
    }
}
