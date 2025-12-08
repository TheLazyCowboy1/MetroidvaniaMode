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
        On.Player.SpearStick += Player_SpearStick;
    }

    public static void RemoveHooks()
    {
        On.Player.checkInput -= Player_checkInput;
        On.Creature.Violence -= Creature_Violence;
        On.Player.SpearStick -= Player_SpearStick;
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
            if (self.isNPC || self.playerState.playerNumber < 0) return; //just in case

            PlayerInfo info = self.GetInfo();

            float prevStrength = info.ShieldStrength;

            info.ShieldStrength = 0;
            if (CurrentAbilities.HasShield && !self.Stunned && !self.dead)
            {
                info.ShieldStrength = Tools.Keybinds.GetAxis(Tools.Keybinds.SHIELD_ID, self.playerState.playerNumber);
            }

            if (info.Shield != null)
                info.Shield.nextWhite = 0; //don't let it stay white when stunned

            //set dir
            if (self.input[0].analogueDir != new Vector2(0, 0)) //for now, don't give myself the headache of dealing with no input
                info.ShieldDir = Custom.VecToDeg(self.input[0].analogueDir);

            if (info.ShieldStrength > 0)
            {
                info.ShieldStrength = GetShieldStrength(info);

                if (info.Shield != null && (info.Shield.slatedForDeletetion || info.Shield.room != self.room))
                {
                    info.Shield.Destroy();
                    info.Shield = null; //we need a new shield
                }

                if (info.Shield == null) //create a new shield
                {
                    info.Shield = new(self);
                    self.room.AddObject(info.Shield);
                    Plugin.Log("Added shield!", 2);
                }

                //add a bit of white when turning it on
                info.Shield.nextWhite = Mathf.Clamp01(info.ShieldStrength - prevStrength) * 0.75f; //only up to 3/4 white

                //add a sound for turning the shield on
                if (info.ShieldStrength >= 0.5f && prevStrength < 0.5f)
                    self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, self.mainBodyChunk, false, 1f, 0.6f + 0.15f * UnityEngine.Random.value);

                //make the slugcat put its arms out
                if (self.graphicsModule is PlayerGraphics graph)
                {
                    graph.blink = Mathf.Max(graph.blink, 2); //keep eyes shut
                    Vector2 reachPos = graph.head.pos + 120f * Custom.DegToVec(info.ShieldDir); //6 tiles away from player head
                    graph.LookAtPoint(reachPos, 0.5f); //look at the shield
                    foreach (SlugcatHand hand in graph.hands)
                    {
                        hand.reachingForObject = true; //put hands out towards the shield
                        hand.absoluteHuntPos = reachPos;
                    }
                }

                //prevent the player from grabbing or throwing and stuff like that
                self.input[0].thrw = false;
                self.input[0].pckp = false;

                //count how long the shield has been up
                info.ShieldCounter += info.ShieldStrength * 0.5f * (info.ShieldStrength + 1); //squared-ish, so lowering shield is encouraged
            }
            else //if the shield is down, decrement the counter
                info.ShieldCounter = Mathf.Max(0, info.ShieldCounter - Options.ShieldRecoverySpeed);


            //give i-frames
            if (info.ShieldStrength > 0.01f)
                info.iFrames = Mathf.Max(info.iFrames, 1);


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
            if (CurrentAbilities.HasShield && self is Player player)
            {
                PlayerInfo info = player.GetInfo();

                if (info.ShieldStrength > 0)
                {
                    //get direction
                    Vector2 shieldDir = Custom.DegToVec(info.ShieldDir);
                    Vector2 hitDir = directionAndMomentum ?? -shieldDir;

                    if (Vector2.Dot(shieldDir, hitDir) < 0) //if the shield was actually hit
                    {
                        //impact shield
                        HitShield(player, hitChunk, info, damage + stunBonus / 80f);

                        directionAndMomentum = hitDir * (3 - 2 * info.ShieldStrength);
                    }
                    else //shield was NOT hit
                    {
                        //break shield
                        HitShield(player, hitChunk, info, 2f);

                        Plugin.Log("Shielding player hit, but the shield was missed", 2);
                    }


                    //prevent the player from being stunned or taking damage

                    int oldStun = player.stun;
                    float oldAerobicLevel = player.aerobicLevel;
                    //if (Health.CurrentHealth > 0)
                    //player.playerState.permanentDamageTracking *= 0.5f;

                    orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, 0, 0);

                    player.stun = oldStun;
                    player.aerobicLevel = oldAerobicLevel;

                    return;
                }
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }

    //prevent players from becoming pincushions
    private static bool Player_SpearStick(On.Player.orig_SpearStick orig, Player self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
    {
        try
        {
            if (CurrentAbilities.HasShield)
            {
                PlayerInfo info = self.GetInfo();

                if (info.ShieldStrength > 0)
                {
                    //get direction
                    Vector2 shieldDir = Custom.DegToVec(info.ShieldDir);

                    if (Vector2.Dot(shieldDir, direction) < 0) //if the shield was actually hit
                    {
                        //set shield strength

                        HitShield(self, chunk, info, 2f * dmg);

                        //add extra force to the hit
                        chunk.vel += source.firstChunk.vel * source.firstChunk.mass / chunk.mass * (2 - 2 * info.ShieldStrength);

                        Plugin.Log("Spear deflected by a shield!", 2);

                        return false; //DO NOT STAB THE PLAYER
                    }
                    else //shield was NOT hit
                    {
                        //break shield
                        HitShield(self, chunk, info, 2f);

                        Plugin.Log("Shielding player stabbed, but the shield was missed", 2);
                    }
                }

            }
        } catch (Exception ex) { Plugin.Error(ex); }

        return orig(self, source, dmg, chunk, appPos, direction);
    }


    public static void HitShield(Player self, BodyChunk hitChunk, PlayerInfo info, float hitStrength)
    {

        info.ShieldCounter = Mathf.Clamp(info.ShieldCounter + Options.ShieldFullTime * hitStrength / Options.ShieldDamageFac / info.ShieldStrength, //hit harder when at lower shield strength
            0, Options.ShieldMaxTime + Options.ShieldFullTime); //can go above normal max time!!
        info.ShieldStrength = GetShieldStrength(info);

        //visual effect
        if (info.Shield != null)
            info.Shield.nextWhite = Mathf.Clamp01(hitStrength * 5);

        //audio effect
        if (info.ShieldStrength > 0)
        {
            self.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, hitChunk, false, Mathf.Clamp01(hitStrength), 1.3f + UnityEngine.Random.value * 0.2f);
            //actually, have a sound managed by the shield...?
        }
        else
        {
            self.room.PlaySound(MoreSlugcats.MoreSlugcatsEnums.MSCSoundID.Chain_Break, hitChunk, false, 1f, 1.3f + UnityEngine.Random.value * 0.2f);
        }

        //stun player if broken
        if (info.ShieldStrength <= 0)
            self.Stun(Mathf.RoundToInt(Options.ShieldStunTime * (info.ShieldCounter - Options.ShieldMaxTime) / Options.ShieldFullTime));

        Plugin.Log("Shield hit! Force = " + hitStrength, 2);
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
        private float lastVol, vol = 0;

        private static Color baseColor = new(0.2f, 0.4f, 1f); //blue
        private static Color whiteColor = new(1, 1, 1);

        private bool posDirty = false;

        private RectangularDynamicSoundLoop soundLoop = null;

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

            AdjustAngle(ref nextRot, rot); //don't have it snap weirdly from 0 to 360

            rot = Custom.LerpAndTick(rot, nextRot, 0.1f, 5f);

            AdjustAngle(ref lastRot, rot); //don't have it snap weirdly from 0 to 360

            lastAlpha = alpha;
            alpha = Custom.LerpAndTick(alpha, nextAlpha, 0.1f, 0.05f);

            lastWhite = white;
            if (nextWhite > white) white = nextWhite; //instantly brighten
            else white = Custom.LerpAndTick(white, nextWhite, 0.1f, 0.05f); //slowly fade

            //volume
            lastVol = vol;
            if (white > vol) vol = Custom.LerpAndTick(vol, white, 0.2f, 0.1f); //quickly fade up
            else vol = Custom.LerpAndTick(vol, white, 0.05f, 0.025f); //very slowly fade

            if (posDirty) //snap it into place; don't let it fly across the screen whenever the sprites are initialized
            {
                lastPos = pos;
                lastRot = rot;
                lastAlpha = alpha;
                lastWhite = white;
                posDirty = false;
            }

            //sound
            if (vol > 0 || lastVol > 0)
            {
                Vector2 soundPos = pos + DrawOffset(rot);
                FloatRect rect = new(soundPos.x - 60f, soundPos.y - 60f, soundPos.x + 60f, soundPos.y + 60f);
                soundLoop ??= new(this, rect, room); //create the sound loop, if it's null

                if (lastVol <= 0) soundLoop.Pitch = 0.8f + 0.2f * UnityEngine.Random.value; //reset pitch when starting up sound again
                soundLoop.rect = rect; //position
                soundLoop.sound = vol > 0 ? SoundID.Electricity_Loop : SoundID.None; //sound
                soundLoop.Volume = vol; //volume
                soundLoop.Update();
            }
        }

        private Vector2 DrawOffset(float rotation) => Custom.DegToVec(rotation + 90f) * 15f;

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
            Vector2 curPos = Vector2.LerpUnclamped(lastPos, pos, timeStacker);
            float curRot = Mathf.LerpUnclamped(lastRot, rot, timeStacker);
            float curAlpha = Mathf.LerpUnclamped(lastAlpha, alpha, timeStacker);
            float curWhite = Mathf.LerpUnclamped(lastWhite, white, timeStacker);

            curPos += DrawOffset(curRot); //make the shield be in FRONT of the player, not inside the player

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
                height = 100,
                color = baseColor,
                alpha = 0
            };

            AddToContainer(sLeaser, rCam, null);
        }
    }
}
