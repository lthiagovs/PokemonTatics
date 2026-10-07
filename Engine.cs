using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using PokemonTFT.Core;
using PokemonTFT.Data;
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
    private bool _ready;

    public Engine()
    {
        _graphics = new GraphicsDeviceManager(this) { HardwareModeSwitch = false };

        GameSettings.Load();

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

        PokemonDatabase.Load();
        DungeonThemes.Load();
        UITheme.Rebuild();
        UIIcons.Load();
        EffectLibrary.Load();
        ItemDatabase.Load();

        GameMusic.MAIN     = Content.Load<Song>("Songs/MAIN");
        GameMusic.TITLE    = Content.Load<Song>("Songs/TITLE");
        GameMusic.VICTORY  = Content.Load<SoundEffect>("Sounds/VICTORY");
        GameMusic.LEVEL_UP = Content.Load<SoundEffect>("Sounds/LEVEL");
        GameMusic.HIT      = Content.Load<SoundEffect>("Sounds/HIT");
        GameSettings.ApplyAudio();

        GameHost.TOGGLE_FULLSCREEN = () =>
        {
            _graphics.ToggleFullScreen();
            GameHost.FULLSCREEN = _graphics.IsFullScreen;
        };
        GameHost.QUIT_HANDLER = Exit;

        GameRenderer.InitializeRenderer(_spriteBatch, GraphicsDevice, Content);
        GameRenderer.SetFont(Content.Load<SpriteFont>("Fonts/GameFont"));

        BuildLayout();
        GameTable.InitializeEnemyTeam();
        GameTableLogic.RefreshCaches();
        GameBonus.Refresh();
        GameHud.Refresh();
        GameInventory.Refresh();

        _lastWidth  = GameRenderer.GetScreenWidth();
        _lastHeight = GameRenderer.GetScreenHeight();
        _ready = true;

        if (GameSettings.FULLSCREEN) GameHost.ToggleFullscreen();
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
        if (!_ready || GraphicsDevice == null) return;

        int width  = GraphicsDevice.Viewport.Width;
        int height = GraphicsDevice.Viewport.Height;
        if (width <= 0 || height <= 0) return;
        if (width == _lastWidth && height == _lastHeight) return;

        _lastWidth  = width;
        _lastHeight = height;

        ItemLogic.ReturnCarried();
        GameTable.Rebuild();
        GameMap.Initialize();
        GameDeck.Initialize();
        TitleScreen.Initialize();
        GameTableLogic.RefreshCaches();
        GameBonus.Refresh();
        GameHud.Refresh();
    }

    protected override void Update(GameTime gameTime)
    {
        GameTimeLogic.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) Exit();

        GameMouse.BeginFrame();
        GameModal.BeginFrame();
        TypeHighlight.BeginFrame();

        if (GameOverScene.IsActive) GameOverScene.Update();
        else if (EvolutionScene.IsActive) EvolutionScene.Update();
        else if (GameGlobals.STATE == GameState.TITLE) UpdateTitle();
        else UpdateGame();

        TypeHighlight.EndFrame();
        GameModal.EndFrame();
        GameMouse.EndFrame();

        base.Update(gameTime);
    }

    private static void UpdateTitle()
    {
        GameMusic.PlayTitle();

        if (GameModal.Blocking)
        {
            GameModal.Update();
            return;
        }

        GameModal.Update();
        GameElement.UpdateAll(TitleScreen.GetElements());
        GameElement.UpdateAll(TitleScreen.GetMenu());
    }

    private static void UpdateGame()
    {
        GameMusic.PlayMain();

        if (GameModal.Blocking)
        {
            GameModal.Update();
            return;
        }

        GameTableLogic.RefreshCaches();

        GameModal.Update();
        if (GameModal.CursorInside && GameMouse.LeftPressed()) GameMouse.ConsumeClick();

        GameInventory.Update();

        GameTableLogic.RefreshPreview();
        GameElement.UpdateAll(GameTable.TableElements);
        GameElement.UpdateAll(GameDeck.GetDeck());
        GameElement.UpdateAll(GameBonus.GetBonusElements());
        GameElement.UpdateAll(GameHud.GetElements());

        GameDeck.Update();
        GameBonus.Update();
        GameHud.Update();
        GameEffect.UpdateAll();
        FloatingText.UpdateAll();
        GameTableLogic.Update();
    }

    protected override void Draw(GameTime gameTime)
    {
        if (GameOverScene.HasCapture)
        {
            GameRenderer.BeginDirect(Color.Black);
            GameOverScene.Render();
            GameRenderer.End();

            base.Draw(gameTime);
            return;
        }

        GameRenderer.BeginFrame(Color.Black);

        if (GameGlobals.STATE == GameState.TITLE)
        {
            GameRenderer.Render(TitleScreen.GetElements());
            GameRenderer.ResolveBackdrop();
            GameRenderer.Render(TitleScreen.GetMenu());
            GameModal.Render();
        }
        else
        {
            GameRenderer.Render(GameMap.GetMap());
            GameRenderer.Render(GameTable.TableElements);
            GameRenderer.RenderSortedByDepth(GameTableElement.GetTableElements());
            GameRenderer.RenderEffects();
            GameRenderer.RenderFloatingText();

            GameRenderer.ResolveBackdrop();

            GameRenderer.Render(GameDeck.GetDeck());
            GameRenderer.Render(GameBonus.GetBonusElements());
            GameRenderer.Render(GameHud.GetElements());
            GameRenderer.Render(GameInventory.GetElements());
            GameRenderer.Render(ItemDrag.Ghost);
            GameRenderer.Render(GameMouse.GetCarry());
            GameModal.Render();
        }

        EvolutionScene.Render();

        GameRenderer.PresentFrame();
        if (GameOverScene.IsActive) GameOverScene.Capture(GameRenderer.FrameTexture);

        GameRenderer.End();

        base.Draw(gameTime);
    }
}
