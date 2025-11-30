using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Shield
{
    public static void ApplyHooks()
    {
        On.Player.Update += Player_Update;
    }

    public static void RemoveHooks()
    {
        On.Player.Update -= Player_Update;
    }


    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        try
        {
            PlayerInfo info = self.GetInfo();

            float a = 0;
            if (Options.HasShield && !self.Stunned && !self.dead)
            {
                a = Tools.Keybinds.GetAxis(Tools.Keybinds.LEFT_TRIGGER_AXIS, self.playerState.playerNumber);
            }

            if (a > 0)
            {
                if (info.Shield != null && info.Shield.slatedForDeletetion)
                    info.Shield = null; //we need a new one anyway
                if (info.Shield == null)
                {
                    info.Shield = new(self);
                    self.room.AddObject(info.Shield);
                    Plugin.Log("Added shield!");
                }
            }

            if (info.Shield != null)
                info.Shield.alpha = a;
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }


    public class ShieldSprite : UpdatableAndDeletable, IDrawable
    {
        private Player player;
        private Vector2 lastPos, pos;
        private float lastRot, rot;
        private float lastAlpha;
        public float alpha = 0;

        public ShieldSprite(Player player)
        {
            this.player = player;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (player == null || this.room == null || this.room != player.room)
            {
                this.Destroy();
                return;
            }

            //move to player
            lastPos = pos;
            pos = player.mainBodyChunk.pos;

            lastRot = rot;
            if (player.input[0].analogueDir != new Vector2(0, 0)) //for now, don't give myself the headache of dealing with no input
                rot = RWCustom.Custom.VecToDeg(player.input[0].analogueDir) - 90f;

            lastAlpha = alpha;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("Foreground");
            newContainer.AddChild(sLeaser.sprites[0]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            //do nothing so far
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 curPos = Vector2.Lerp(lastPos, pos, timeStacker);
            float curRot = Mathf.Lerp(lastRot, rot, timeStacker);
            float curAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);

            sLeaser.sprites[0].SetPosition(curPos - camPos);
            sLeaser.sprites[0].rotation = curRot;
            sLeaser.sprites[0].alpha = curAlpha;

            if (!sLeaser.deleteMeNextFrame && (this.slatedForDeletetion || this.room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true)
            {
                shader = Tools.Assets.ShieldEffect,
                width = 40,
                height = 120,
                color = new(0.2f, 0.4f, 1f),
                alpha = 0
            };

            AddToContainer(sLeaser, rCam, null);
        }
    }
}
