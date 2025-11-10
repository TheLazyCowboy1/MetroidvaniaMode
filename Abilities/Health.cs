using System;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Health
{
    public static int CurrentHealth = 0;

    public static void ApplyHooks()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Die += Player_Die;
        On.Player.Destroy += Player_Destroy;
        On.Player.Grabbed += Player_Grabbed;
        On.Player.Update += Player_Update;

        On.RainWorldGame.Update += RainWorldGame_Update;

        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
    }

    public static void RemoveHooks()
    {
        On.Player.ctor -= Player_ctor;
        On.Player.Die -= Player_Die;
        On.Player.Destroy -= Player_Destroy;
        On.Player.Grabbed -= Player_Grabbed;
        On.Player.Update -= Player_Update;

        On.RainWorldGame.Update -= RainWorldGame_Update;

        On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
    }

    /// <summary>
    /// Deals damage to the player
    /// </summary>
    /// <param name="self">The player to deal damage to</param>
    /// <param name="damage">The amount of damage to deal</param>
    public static void TakeDamage(this Player self, int damage)
    {
        PlayerInfo info = self.GetInfo();
        if (info.iFrames <= 0)
        {
            self.room.AddObject(new TemporaryLight(self.mainBodyChunk.pos, false, Color.red, self, 40, 10)
                { blinkType = PlacedObject.LightSourceData.BlinkType.Fade, blinkRate = 1.005f, //blink every 5 ticks
                setAlpha = 1f, colorAlpha = 2f, setRad = 80f * damage, affectedByPaletteDarkness = 0 }); //add a flashing red light for 1 second

            if (damage > 1)
                self.room.PlaySound(SoundID.MENU_Start_New_Game, self.mainBodyChunk, false, 0.8f + damage * 0.2f, 1.1f + damage * 0.2f);
            else
                self.room.PlaySound(SoundID.HUD_Game_Over_Prompt, self.mainBodyChunk, false, 1f, 1.35f); //play an impactful sound

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

            if (CurrentHealth == 0)
                self.playerState.permanentDamageTracking = 0.98f; //make the player injured
        }
        info.iFrames = 40; //1 second of i-frames
        self.showKarmaFoodRainTime = 40; //show the HUD
        UI.HealthMeter.HealthFlash = 40;

        Plugin.Log("Player damaged! Damage = " + damage);
    }

    private class TemporaryLight : LightSource
    {
        public int lifeRemaining = 0;
        public int fadeTime = 0;

        public TemporaryLight(Vector2 pos, bool environmental, Color color, UpdatableAndDeletable tiedToObject, int length, int fadeTime = 0) : base(pos, environmental, color, tiedToObject)
        {
            lifeRemaining = length;
            this.fadeTime = fadeTime;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            lifeRemaining--;
            if (lifeRemaining <= 0)
                this.Destroy();
            else if (lifeRemaining <= fadeTime)
            {
                this.alpha *= 1f - 2f / fadeTime;
                this.rad *= 1f - 2f / fadeTime;
            }
        }
    }


    //Set the health when player 0 is spawned
    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (self.playerState.playerNumber == 0)
        {
            CurrentHealth = CurrentAbilities.MaxHealth;
            Plugin.Log("Set player health: " + CurrentHealth);
        }
    }

    //Prevent the player from dying if he has > 1 health
    private static bool dieAnyway = false; //used to kill the player anyway when destroyed
    private static void Player_Die(On.Player.orig_Die orig, Player self)
    {
        if (!CurrentAbilities.HasHealth)
        {
            orig(self);
            return;
        }

        try
        {
            if (dieAnyway
                || ((CurrentHealth < 2 || self.abstractCreature.InDen || self.playerState.permaDead || (!CurrentAbilities.CanSwim && self.drown >= 1))
                    && self.GetInfo().iFrames <= 0)
                )
            {
                Plugin.Log("Player died!");
                orig(self);

                if (CurrentHealth > 0)
                    self.TakeDamage(CurrentHealth); //visually show the health bar at 0
            }
            else
            {
                Plugin.Log("Aborting player death. Health = " + CurrentHealth);

                if (self.airInLungs < 0.2f)
                    self.airInLungs += 0.1f; //add a little air if the player is almost out of breath
                if (self.drown > 0)
                    self.drown = 0; //stop the player from drowning

                self.TakeDamage(2);
            }
            dieAnyway = false;
        } catch (Exception ex) { Plugin.Error(ex); orig(self); }
    }

    private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        if (!CurrentAbilities.HasHealth)
        {
            orig(self);
            return;
        }

        dieAnyway = true;
        self.Die();

        orig(self);
    }

    private static void Player_Grabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
    {
        if (!CurrentAbilities.HasHealth)
        {
            orig(self, grasp);
            return;
        }

        orig(self, grasp);

        try
        {
            PlayerInfo info = self.GetInfo();
            if (CurrentHealth > 0 || info.iFrames > 0)
            {
                info.ReleaseQueued = true;
                //TakeDamage(self, 1); //take damage on Player.Update() instead, so that lethal lizard bites deal 2 damage still
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (!CurrentAbilities.HasHealth)
        {
            orig(self, eu);
            return;
        }

        try
        {
            PlayerInfo info = self.GetInfo();
            if (info.ReleaseQueued)
            {
                info.ReleaseQueued = false;

                Creature.Grasp[] tempList = self.grabbedBy.ToArray();
                foreach (var g in tempList)
                {
                    g.grabber.Stun(40);
                    //self.room.AddObject(new CreatureSpasmer(g.grabber, false, g.grabber.stun));
                    g.grabber.LoseAllGrasps();
                }
                Plugin.Log("Released grasps on player");

                self.TakeDamage(1);
            }

            //decrement iFrames
            if (info.iFrames > 0 && !self.Stunned) //don't decrement i-frames while stunned
                info.iFrames--;

        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, eu);
    }


    //Set CoopAvailable during game updates so that PermaDie is used instead of just Die
    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        if (CurrentAbilities.HasHealth && !ModManager.CoopAvailable)
        {
            ModManager.CoopAvailable = true;
            orig(self);
            ModManager.CoopAvailable = false;
        }

        orig(self);
    }


    //Add the health meter
    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);

        try
        {
            if (CurrentAbilities.HasHealth)
            {
                self.AddPart(new UI.HealthMeter(self));
                Plugin.Log("Added health meter");
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
