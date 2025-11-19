using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MetroidvaniaMode.Items;

public class HealFruit : DangleFruit
{
    public HealFruit(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
    {
    }

    public new int FoodPoints => 0;
    public new bool Edible => Abilities.Health.CurrentHealth < Abilities.CurrentAbilities.MaxHealth;

    public new void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);

        this.color = Color.Lerp(new Color(0, 1, 0), palette.blackColor, this.darkness);
    }

    public new void BitByPlayer(Creature.Grasp grasp, bool eu)
    {
        this.bites--;
        this.room.PlaySound((this.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, base.firstChunk);
        base.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
        if (this.bites < 1)
        {
            //ADD HEALTH
            if (Abilities.Health.CurrentHealth < Abilities.CurrentAbilities.MaxHealth)
                Abilities.Health.CurrentHealth++;

            (grasp.grabber as Player).ObjectEaten(this);
            grasp.Release();
            this.Destroy();
        }
    }
}
