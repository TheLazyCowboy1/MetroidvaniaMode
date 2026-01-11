using RWCustom;
using System;
using UnityEngine;

namespace MetroidvaniaMode.Items;

public class HealFruit : DangleFruit, IDrawable, IPlayerEdible
{
    public const int HEAL_AMOUNT = 2;

    public HealFruit(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
    {
    }

    public int FoodPoints => 0;
    public bool Edible => Abilities.Health.CurrentHealth < Abilities.CurrentAbilities.MaxHealth;

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            if (i % 2 == 0)
            {
                sLeaser.sprites[i].color = palette.blackColor;
            }
        }

        base.color = Color.Lerp(new Color(0, 1f, 0), palette.blackColor, base.darkness);
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        Vector2 drawPos = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
        Vector2 drawRot = Vector3.Slerp(base.lastRotation, base.rotation, timeStacker);
        Vector2 perpRot = Custom.PerpendicularVector(drawRot);
        base.lastDarkness = base.darkness;
        base.darkness = rCam.room.Darkness(drawPos) * (1f - rCam.room.LightSourceExposure(drawPos));
        if (base.darkness != base.lastDarkness)
        {
            this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }
        Vector2 vector4 = Vector2.zero;
        Vector2 vector5 = drawRot;
        for (int j = 0; j < sLeaser.sprites.Length; j++)
        {
            sLeaser.sprites[j].x = drawPos.x - camPos.x + vector4.x;
            sLeaser.sprites[j].y = drawPos.y - camPos.y + vector4.y;
            sLeaser.sprites[j].rotation = Custom.VecToDeg(vector5);
            sLeaser.sprites[j].element = Futile.atlasManager.GetElementWithName("DangleFruit" + Custom.IntClamp(3 - base.bites, 0, 2).ToString() + ((j % 2 == 0) ? "A" : "B"));
            if (j % 2 == 1)
            {
                if (base.blink > 0 && global::UnityEngine.Random.value < 0.5f)
                {
                    sLeaser.sprites[j].color = base.blinkColor;
                }
                else
                {
                    sLeaser.sprites[j].color = base.color;
                }
            }
        }
        if (base.slatedForDeletetion || base.room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }
    }

    public void BitByPlayer(Creature.Grasp grasp, bool eu)
    {
        base.bites--;
        base.room.PlaySound((base.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, base.firstChunk);
        base.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
        if (base.bites < 1)
        {
            //ADD HEALTH
            Abilities.Health.CurrentHealth = Math.Min(Abilities.Health.CurrentHealth + HEAL_AMOUNT, Abilities.CurrentAbilities.MaxHealth);
            Plugin.Log("Ate heal fruit!", 2);
            base.room.PlaySound(SoundID.MENU_Karma_Ladder_Hit_Upper_Cap, base.firstChunk);

            (grasp.grabber as Player).ObjectEaten(this);
            grasp.Release();
            base.Destroy();
        }
    }
}
