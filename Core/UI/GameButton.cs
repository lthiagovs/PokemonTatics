using System;
using GAME.CORE;
using GAME.TABLE;
using GAME.UI;
using Microsoft.Xna.Framework;

public class GameButton : GameInterfaceElement
{
    public GameButton(short POS_X, short POS_Y, short SIZE_X, short SIZE_Y, bool VISIBLE, GameInterfaceElement PARENT = null) 
    : base(POS_X, POS_Y, SIZE_X, SIZE_Y, VISIBLE, PARENT) {}

    public override void Update()
    {

        this.GetRendererConfig().COLOR = COLOR_STATE;

        // MOUSE DETECTION
        if(this.GetRectangle().Intersects(GameMouse.GetRectangle())) {
            
            // HOVER
            //if(this.CONFIG.IsHover()) { this.RENDER_CONFIG.COLOR = Color.Green * 1.2f; }

            //ACTION
            if (GameMouse.LeftPressed())
            {   
                
                if(GameGlobals.STATE == GameState.TITLE) { GameGlobals.STATE = GameState.GAME; GameMusic.Stop(); return; }

                if(GameGlobals.GAME_STARTED) return;

                if(!GameGlobals.GAME_STARTED) { GameGlobals.GAME_STARTED = true; GameBonus.Initialize(); this.RENDER_CONFIG.COLOR = Color.Green * 2.2f; }
            }

        }

    }

}