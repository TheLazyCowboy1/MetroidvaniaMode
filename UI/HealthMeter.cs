using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class HealthMeter : HudPart
{
    private FSprite[] slugSprites;

    private FContainer fContainer => this.hud.fContainers[1];

    public HealthMeter(HUD.HUD hud) : base(hud)
    {
        slugSprites = new FSprite[Options.MaxHealth];
        Vector2 pos = new Vector2(Mathf.Max(70f, hud.rainWorld.options.SafeScreenOffset.x + 25.5f), Mathf.Max(45f, hud.rainWorld.options.SafeScreenOffset.y + 37.25f));

        for (int i = 0; i < slugSprites.Length; i++)
        {
            slugSprites[i] = new FSprite("Kill_Slugcat");
            slugSprites[i].SetPosition(pos + new Vector2(i * 20, 0));
            fContainer.AddChild(slugSprites[i]);
        }
    }

    public override void Draw(float timeStacker)
    {
        base.Draw(timeStacker);

        for (int i = 0; i < slugSprites.Length; i++)
        {
            slugSprites[i].isVisible = i < Abilities.Health.CurrentHealth;
        }
    }

    public override void ClearSprites()
    {
        base.ClearSprites();

        for (int i = 0; i < slugSprites.Length; i++)
        {
            slugSprites[i].RemoveFromContainer();
        }
    }
}