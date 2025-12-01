using Epic.OnlineServices.Presence;
using RWCustom;
using System;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Shield
{
    public static void ApplyHooks()
    {
        //On.Player.Update += Player_Update;
        On.Player.checkInput += Player_checkInput;
        On.Creature.Violence += Creature_Violence;
    }

    public static void RemoveHooks()
    {
        On.Player.checkInput -= Player_checkInput;
        On.Creature.Violence -= Creature_Violence;
    }


    private static float GetShieldStrength(PlayerInfo info)
    {
        float maxStrength = Mathf.Clamp01((Options.ShieldMaxTime - info.ShieldCounter) / (float)(Options.ShieldMaxTime - Options.ShieldFullTime));
        return Mathf.Min(info.ShieldStrength, maxStrength);
    }


    //checkInput is used so that we can prevent throwing or grabbing
    private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);

        try
        {
            if (self.isNPC) return; //just in case

            PlayerInfo info = self.GetInfo();

            float prevStrength = info.ShieldStrength;

            info.ShieldStrength = 0;
            if (CurrentAbilities.HasShield && !self.Stunned && !self.dead)
            {
                info.ShieldStrength = Tools.Keybinds.GetAxis(Tools.Keybinds.SHIELD_ID, self.playerState.playerNumber);
            }

            if (info.Shield != null)
                info.Shield.nextWhite = 0; //don't let it stay white when stunned

            if (info.ShieldStrength > 0)
            {
                info.ShieldStrength = GetShieldStrength(info);

                if (info.Shield != null && info.Shield.slatedForDeletetion)
                    info.Shield = null; //we need a new shield

                if (info.Shield == null) //create a new shield
                {
                    info.Shield = new(self);
                    self.room.AddObject(info.Shield);
                    Plugin.Log("Added shield!", 2);
                }

                //add a bit of white when turning it on
                info.Shield.nextWhite = Mathf.Clamp01(info.ShieldStrength - prevStrength);

                //prevent the player from grabbing or throwing and stuff like that
                self.input[0].thrw = false;
                self.input[0].pckp = false;

                //give i-frames
                info.iFrames = Mathf.Max(info.iFrames, 1);

                //count how long the shield has been up
                info.ShieldCounter += info.ShieldStrength;
            }
            else //if the shield is down, decrement the counter
                info.ShieldCounter = Mathf.Max(0, info.ShieldCounter - Options.ShieldRecoverySpeed);


            //set dir
            if (self.input[0].analogueDir != new Vector2(0, 0)) //for now, don't give myself the headache of dealing with no input
                info.ShieldDir = Custom.VecToDeg(self.input[0].analogueDir);


            //apply visuals
            if (info.Shield != null)
            {
                info.Shield.nextAlpha = info.ShieldStrength;
                info.Shield.nextRot = info.ShieldDir - 90f;
            }

        }
        catch (Exception ex) { Plugin.Error(ex); }
    }


    private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
    {
        try
        {
            if (self is Player player)
            {
                PlayerInfo info = player.GetInfo();
                if (info.ShieldStrength > 0)
                {
                    //get direction
                    Vector2 shieldDir = Custom.DegToVec(info.ShieldDir);
                    Vector2 hitDir = directionAndMomentum ?? -shieldDir;

                    if (Vector2.Dot(shieldDir, hitDir) < 0) //if the shield was actually hit
                    {
                        //set shield strength
                        float hitStrength = damage + stunBonus / 80f;

                        HitShield(player, hitChunk, info, hitStrength);

                        directionAndMomentum = hitDir * (3 - 2 * info.ShieldStrength);
                        if (info.ShieldStrength > 0)
                        {
                            damage = 0;
                            stunBonus = -40f;
                        }
                        else
                        {
                            //increase stunBonus
                            stunBonus += 20f;
                        }
                    }
                    else //shield was NOT hit
                    {
                        //break shield
                        info.ShieldCounter = Options.ShieldMaxTime + Options.ShieldFullTime;
                        info.ShieldStrength = 0;
                        if (info.Shield != null)
                            info.Shield.nextWhite = 1;

                        //play sound
                        self.room.PlaySound(MoreSlugcats.MoreSlugcatsEnums.MSCSoundID.Chain_Break, hitChunk);

                        //increase stunBonus
                        stunBonus += 20f;

                        Plugin.Log("Shielding player hit, but the shield was missed", 2);
                    }
                }
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    public static void HitShield(Player self, BodyChunk hitChunk, PlayerInfo info, float hitStrength)
    {

        info.ShieldCounter = Mathf.Min(info.ShieldCounter + Options.ShieldFullTime * hitStrength / Options.ShieldDamageFac, Options.ShieldMaxTime + Options.ShieldFullTime); //can go above normal max time!!
        info.ShieldStrength = GetShieldStrength(info);

        //visual effect
        if (info.Shield != null)
            info.Shield.nextWhite = Mathf.Clamp01(hitStrength * 5);

        //audio effect
        if (info.ShieldStrength > 0)
        {
            //alter sound start time
            ChunkSoundEmitter sound = self.room.PlaySound(SoundID.Zapper_Zap, hitChunk, false, 0.8f, 0.6f + UnityEngine.Random.value * 0.2f);
            sound.currentSoundObject.audioSource.time = 0.5f * sound.currentSoundObject.audioSource.clip.length;
        }
        else
            self.room.PlaySound(MoreSlugcats.MoreSlugcatsEnums.MSCSoundID.Chain_Break, hitChunk);

        Plugin.Log("Shield hit!", 2);
    }


    public class ShieldSprite : UpdatableAndDeletable, IDrawable
    {
        private Player player;
        private Vector2 lastPos, pos;
        private float lastRot, rot;
        public float nextRot = 0;
        private float lastAlpha, alpha;
        public float nextAlpha = 0;
        private float lastWhite, white;
        public float nextWhite = 0;

        private static Color baseColor = new(0.2f, 0.4f, 1f); //blue
        private static Color whiteColor = new(1, 1, 1);

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

            AdjustAngle(ref rot, nextRot); //don't have it snap weirdly from 0 to 360

            rot = Custom.LerpAndTick(rot, nextRot, 0.1f, 5f);

            AdjustAngle(ref lastRot, rot); //don't have it snap weirdly from 0 to 360

            lastAlpha = alpha;
            alpha = Custom.LerpAndTick(alpha, nextAlpha, 0.1f, 0.05f);

            lastWhite = white;
            if (nextWhite > white) white = nextWhite; //instantly brighten
            else white = Custom.LerpAndTick(white, nextWhite, 0.1f, 0.05f); //slowly fade

            if (posDirty) //snap it into place; don't let it fly across the screen whenever the sprites are initialized
            {
                lastPos = pos;
                lastRot = rot;
                lastAlpha = alpha;
                lastWhite = white;
                posDirty = false;
            }
        }

        private static void AdjustAngle(ref float a, float b)
        {
            if (b - a > 180f) //to smooth out the transition between 0 and 360
                a += 360f;
            else if (b - a < -180f)
                a -= 360f;
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
            float curWhite = Mathf.Lerp(lastWhite, white, timeStacker);

            sLeaser.sprites[0].SetPosition(curPos - camPos);
            sLeaser.sprites[0].rotation = curRot;
            sLeaser.sprites[0].alpha = curAlpha + curWhite * 0.5f;
            sLeaser.sprites[0].color = Color.Lerp(baseColor, whiteColor, curWhite);

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
                color = baseColor,
                alpha = 0
            };

            AddToContainer(sLeaser, rCam, null);
        }
    }
}
