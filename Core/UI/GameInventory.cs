using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PokemonTFT.Core;
using PokemonTFT.Data;
using PokemonTFT.Logic;
using PokemonTFT.Models;

namespace PokemonTFT.UI;

public static class GameInventory
{
    private const int MARGIN = 18;
    private const int SLOT = 56;
    private const int GAP = 6;
    private const int PER_ROW = 4;
    private const int HEADER = 40;
    private const int FOOTER = 26;

    private static readonly List<GameElement> ELEMENTS = [];

    private static int _signature = int.MinValue;
    private static bool _escapeHeld;

    public static IReadOnlyList<GameElement> GetElements() => ELEMENTS;

    public static void Refresh() => _signature = int.MinValue;

    public static void Update()
    {
        HandleCarry();
        ItemDrag.Update();

        int signature = ItemLogic.Signature;
        if (signature != _signature)
        {
            _signature = signature;
            Rebuild();
        }

        GameElement.UpdateAll(ELEMENTS);
    }

    private static void HandleCarry()
    {
        bool escape = Keyboard.GetState().IsKeyDown(Keys.Escape);
        bool escapePressed = escape && !_escapeHeld;
        _escapeHeld = escape;

        if (ItemLogic.Carried == null) return;

        if (GameMouse.RightPressed() || escapePressed)
        {
            ItemLogic.ReturnCarried();
            return;
        }

        if (!GameMouse.LeftPressed()) return;

        GameMouse.ConsumeClick();

        PokemonEntity? ally = AllyUnderCursor();
        if (ally?.POKEMON == null)
        {
            ItemLogic.ReturnCarried();
            return;
        }

        Item item = ItemLogic.Carried;

        switch (ItemLogic.EquipCarried(ally.POKEMON))
        {
            case EquipResult.EQUIPPED:
                FloatingText.SpawnStatus(item.NAME, HeadOf(ally), UITheme.ACCENT);
                ally.SetEffect(new RenderEffect(RenderEffectType.FLASH, 0.45f));
                break;

            case EquipResult.FULL:
                FloatingText.SpawnStatus($"MAX {Balance.MAX_ITEMS_PER_POKEMON} ITEMS", HeadOf(ally), UITheme.HEALTH);
                break;
        }
    }

    private static PokemonEntity? AllyUnderCursor()
    {
        Point mouse = GameMouse.GetPos();
        IReadOnlyList<PokemonEntity> allies = GameTableLogic.PlayerEntities;

        PokemonEntity? best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            PokemonEntity ally = allies[i];
            if (ally.POKEMON == null || ally.DEAD || ally.CARRIED) continue;
            if (!ally.GetRectangle().Contains(mouse)) continue;

            float dx = ally.Center.X - mouse.X;
            float dy = ally.Center.Y - mouse.Y;
            float distance = dx * dx + dy * dy;

            if (distance >= bestDistance) continue;

            best = ally;
            bestDistance = distance;
        }

        return best;
    }

    private static Point HeadOf(PokemonEntity ENTITY)
    {
        Rectangle bounds = ENTITY.GetRectangle();
        return new Point(bounds.X + bounds.Width / 2, bounds.Y);
    }

    private static void Rebuild()
    {
        ELEMENTS.Clear();

        IReadOnlyList<ItemPile> bag = ItemLogic.Bag;
        if (bag.Count == 0 && ItemLogic.Carried == null) return;

        int rows = Math.Max(1, (bag.Count + PER_ROW - 1) / PER_ROW);
        int width = PER_ROW * SLOT + (PER_ROW - 1) * GAP + UIFactory.PAD * 2;
        int height = HEADER + rows * SLOT + (rows - 1) * GAP + UIFactory.PAD * 2 + FOOTER;

        int top = Math.Max(MARGIN, GameRenderer.GetScreenHeight() / 2 - height / 2);
        GameInterfaceElement panel = UIFactory.Panel(MARGIN, top, width, height);
        ELEMENTS.Add(panel);

        int total = 0;
        for (int i = 0; i < bag.Count; i++) total += bag[i].COUNT;

        UIFactory.IconLabel("star", "BAG", UIFactory.PAD, UIFactory.PAD, panel, UITheme.ACCENT);

        string count = $"{total}";
        Vector2 countSize = GameFonts.Measure(count, GameFonts.BODY);
        UIFactory.LabelBox(count,
            new Rectangle(width - UIFactory.PAD - (int)countSize.X, UIFactory.PAD, (int)countSize.X, HEADER - 10),
            panel, UITheme.TEXT_DIM);

        for (int i = 0; i < bag.Count; i++)
        {
            int column = i % PER_ROW;
            int row = i / PER_ROW;

            BuildSlot(bag[i],
                MARGIN + UIFactory.PAD + column * (SLOT + GAP),
                top + UIFactory.PAD + HEADER + row * (SLOT + GAP));
        }

        string hint = ItemLogic.Carried != null ? "CLICK AN ALLY" : "CLICK TO PICK UP";
        UIFactory.LabelBox(hint,
            new Rectangle(UIFactory.PAD, height - FOOTER - UIFactory.PAD + 4, width - UIFactory.PAD * 2, FOOTER),
            panel, ItemLogic.Carried != null ? UITheme.ACCENT : UITheme.TEXT_DIM, GameFonts.SMALL);
    }

    private static void BuildSlot(ItemPile STACK, int X, int Y)
    {
        var slot = new ItemSlotElement(X, Y, SLOT, SLOT, VISIBLE: true)
        {
            HOVERABLE = true,
            STACK = STACK
        };

        slot.SetRendererConfig(GameRendererConfig.Solid(UITheme.BORDER));
        UIFactory.Solid(UIFactory.BORDER, UIFactory.BORDER, SLOT - UIFactory.BORDER * 2,
            SLOT - UIFactory.BORDER * 2, UITheme.SURFACE_DEEP, slot);

        Rectangle icon = STACK.ITEM.ICON;
        var sprite = new GameInterfaceElement((SLOT - icon.Width) / 2, (SLOT - icon.Height) / 2,
            icon.Width, icon.Height, VISIBLE: true, PARENT: slot);
        sprite.SetRendererConfig(GameRendererConfig.Sprite(ItemDatabase.TEXTURE, icon));
        slot.AddChild(sprite);

        if (STACK.COUNT > 1)
        {
            string badge = $"x{STACK.COUNT}";
            Vector2 size = GameFonts.Measure(badge, GameFonts.SMALL);
            UIFactory.LabelBox(badge,
                new Rectangle(SLOT - (int)size.X - 6, SLOT - (int)size.Y - 4, (int)size.X, (int)size.Y),
                slot, UITheme.ACCENT, GameFonts.SMALL);
        }

        ELEMENTS.Add(slot);
    }
}
