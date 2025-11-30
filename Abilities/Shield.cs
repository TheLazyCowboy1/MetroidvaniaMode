using RWCustom;
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
        //On.Player.Update += Player_Update;
        On.Player.checkInput += Player_checkInput;
    }

    public static void RemoveHooks()
    {
        On.Player.checkInput -= Player_checkInput;
    }


    //checkInput is used so that we can prevent throwing or grabbing
    private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);

        try
        {
            if (self.isNPC) return; //just in case

            PlayerInfo info = self.GetInfo();

            info.ShieldStrength = 0;
            if (Options.HasShield && !self.Stunned && !self.dead)
            {
                info.ShieldStrength = Tools.Keybinds.GetAxis(Tools.Keybinds.LEFT_TRIGGER_AXIS, self.playerState.playerNumber);
            }

            if (info.ShieldStrength > 0)
            {
                float maxStrength = Mathf.Clamp01((Options.ShieldMaxTime - info.ShieldCounter) / (float)(Options.ShieldMaxTime - Options.ShieldFullTime));
                info.ShieldStrength = Mathf.Min(info.ShieldStrength, maxStrength);

                if (info.Shield != null && info.Shield.slatedForDeletetion)
                    info.Shield = null; //we need a new shield

                if (info.Shield == null) //create a new shield
                {
                    info.Shield = new(self);
                    self.room.AddObject(info.Shield);
                    Plugin.Log("Added shield!", 2);
                }

                //prevent the player from grabbing or throwing and stuff like that
                self.input[0].thrw = false;
                self.input[0].pckp = false;

                //count how long the shield has been up
                info.ShieldCounter += info.ShieldStrength;
            }
            else //if the shield is down, decrement the counter
                info.ShieldCounter = Mathf.Max(0, info.ShieldCounter - 1);

            if (info.Shield != null)
                info.Shield.nextAlpha = info.ShieldStrength;
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }


    public class ShieldSprite : UpdatableAndDeletable, IDrawable
    {
        private Player player;
        private Vector2 lastPos, pos;
        private float lastRot, rot;
        private float lastAlpha, alpha = 0;
        public float nextAlpha = 0;

        private bool posDirty = false;

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
                rot = Custom.LerpAndTick(rot, Custom.VecToDeg(player.input[0].analogueDir) - 90f, 0.1f, 5f);

            if (rot - lastRot > 180f) //to smooth out the transition between 0 and 360
                lastRot += 360f;
            else if (rot - lastRot < -180f)
                lastRot -= 360f;

            lastAlpha = alpha;
            alpha = Custom.LerpAndTick(alpha, nextAlpha, 0.1f, 0.05f);

            if (posDirty) //snap it into place; don't let it fly across the screen whenever the sprites are initialized
            {
                lastPos = pos;
                lastRot = rot;
                lastAlpha = alpha;
                posDirty = false;
            }
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
            posDirty = true;

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
