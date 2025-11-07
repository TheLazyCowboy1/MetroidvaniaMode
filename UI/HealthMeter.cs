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
    private FSprite[] spriteBackgrounds;
    private FSprite[] slugSprites;

    private FContainer fContainer => this.hud.fContainers[1];

    public HealthMeter(HUD.HUD hud) : base(hud)
    {
        slugSprites = new FSprite[Options.MaxHealth];
        spriteBackgrounds = new FSprite[slugSprites.Length];

            //this pos is the FoodMeter's default location
        Vector2 pos = new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f));
        pos.x += 50f;
        pos.y += 50f; //offset it to not collide with food meter

        for (int i = 0; i < slugSprites.Length; i++)
        {
            spriteBackgrounds[i] = new FSprite("Kill_Slugcat");
            spriteBackgrounds[i].SetPosition(pos + new Vector2(i * 20 - 1, -1));
            spriteBackgrounds[i].scaleX = (spriteBackgrounds[i].width + 2) / spriteBackgrounds[i].width;
            spriteBackgrounds[i].scaleY = (spriteBackgrounds[i].height + 2) / spriteBackgrounds[i].height;
            spriteBackgrounds[i].color = new(0.5f, 0, 0);
            spriteBackgrounds[i].alpha = 0.7f;
            fContainer.AddChild(spriteBackgrounds[i]);

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
            spriteBackgrounds[i].RemoveFromContainer();
            slugSprites[i].RemoveFromContainer();
        }
    }
}