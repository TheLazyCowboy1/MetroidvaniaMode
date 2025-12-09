using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Glide
{
    private static Hook PlayerGravHook;
    public static void ApplyHooks()
    {
        On.Player.MovementUpdate += Player_MovementUpdate;
        try
        {
            PlayerGravHook = new(typeof(Player).GetProperty(nameof(Player.EffectiveRoomGravity)).GetGetMethod(), Player_EffectiveRoomGravity);
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    public static void RemoveHooks()
    {
        On.Player.MovementUpdate -= Player_MovementUpdate;
        PlayerGravHook?.Undo();
    }


    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        try
        {
            if (!CurrentAbilities.CanGlide)
                return;

            PlayerInfo info = self.GetInfo();

            //check if we should stop gliding
            bool allowedBodyMode = self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.Crawl
                    || self.bodyMode == Player.BodyModeIndex.Stand || self.bodyMode == Player.BodyModeIndex.ZeroG;
            bool allowedAnimation = self.animation == Player.AnimationIndex.BellySlide || self.animation == Player.AnimationIndex.DownOnFours
                    || self.animation == Player.AnimationIndex.Flip || self.animation == Player.AnimationIndex.GrapplingSwing
                    || self.animation == Player.AnimationIndex.None || self.animation == Player.AnimationIndex.RocketJump
                    || self.animation == Player.AnimationIndex.Roll || self.animation == Player.AnimationIndex.StandUp;

            if (info.Gliding && (!allowedBodyMode || !allowedAnimation || !self.input[0].jmp || self.canJump > 0))
            {
                info.Gliding = false; //stop gliding if we're not holding jump, or if we regain our ability to jump
            }

            //check if we should start gliding
            if (!info.Gliding && allowedBodyMode && allowedAnimation && self.wantToJump > 0 && self.canJump <= 0 && self.input[0].jmp)
            {
                info.Gliding = true; //start gliding
            }

            if (info.Gliding)
            {
                //input
                Vector2 dir;
                if (self.input[0].gamePad)
                    dir = self.input[0].analogueDir; //NOT normalized
                else //keyboard logic
                {
                    if (self.input[0].x == 0)
                        dir = new Vector2(0, self.input[0].y);
                    else
                        dir = new Vector2(self.input[0].x, self.input[0].y * Options.GlideKeyboardYFac).normalized; //multiply y by 0.5
                }

                //glide physics
                foreach (BodyChunk chunk in self.bodyChunks)
                {

                    //add thrust lol
                    chunk.vel += dir * Options.GlideThrust;

                    //physics-based approach v2

                    //calc drag
                    Vector2 nVel = chunk.vel.normalized;
                    Vector2 dragDir;
                    if (Options.EasierGlideMode && self.EffectiveRoomGravity > 0) //don't apply in 0-g
                    {
                        //shift dragDir to feel more natural to fly with.
                        //shift dir down slightly (so that the slugcat normally moves forward)
                        Vector2 dir2 = dir + new Vector2(0, Options.GlideBaseDirY);
                        if (dir2.sqrMagnitude > 0.0001f) //don't let it explode by dividing by 0
                            dir2 *= Mathf.Sqrt(dir.sqrMagnitude / dir2.sqrMagnitude); //set dir2's magnitude to dir1's
                        dragDir = Perpendicular(dir2);
                        //if dragDir.magnitude < 1, lerp it towards (0, 1)
                        if (nVel.y < 0) //don't stop upwards speed, though
                            dragDir = Vector2.LerpUnclamped(new(0, 1), dragDir, dragDir.magnitude);
                        //if dir is up and vel is down, lerp dragDir up
                        if (nVel.y < 0 && dir.y > 0)
                            dragDir = Vector2.LerpUnclamped(dragDir, -nVel, dir.y * -nVel.y);
                        else if (nVel.y > 0 && dir.y > 0) //don't slow me down when trying to go upwards (e.g: double jump)
                            dragDir.y *= 1 - dir.y;

                        //normalize dragDir
                        dragDir.Normalize();
                    }
                    else
                        dragDir = Perpendicular(dir); //this is the proper one, but not easy to fly with

                    float dragFac = -(dragDir.x * nVel.x + dragDir.y * nVel.y) * Options.GlideDragCoef; 
                    Vector2 drag = (dragFac < 0 ? -dragDir : dragDir) * (chunk.vel * dragFac).sqrMagnitude;

                    //apply drag
                    if (drag.sqrMagnitude > chunk.vel.sqrMagnitude)
                        chunk.vel = new(0, 0); //don't let drag exceed velocity
                    else
                        chunk.vel += drag;

                    //calc lift
                    nVel = chunk.vel.normalized;
                    dragDir = Perpendicular(dir); //use the proper calculation for lift
                    float liftFac = -(dragDir.x * nVel.x + dragDir.y * nVel.y) * Options.GlideLiftCoef;
                    Vector2 lift = Perpendicular(chunk.vel) * liftFac;
                    lift *= lift.sqrMagnitude;

                    //apply lift
                    if (lift.sqrMagnitude > chunk.vel.sqrMagnitude * Options.GlideMaxLift * Options.GlideMaxLift)
                        lift = Vector2.ClampMagnitude(lift, chunk.vel.magnitude * Options.GlideMaxLift); //don't let velocity go supersonic
                    chunk.vel += lift;

                    //apply drag in all directions to prevent supersonic explosions
                    nVel = chunk.vel.normalized;
                    Vector2 omniDrag = -nVel * (chunk.vel * Options.GlideOmniDragCoef).sqrMagnitude;
                    if (omniDrag.sqrMagnitude > chunk.vel.sqrMagnitude)
                        chunk.vel = new(0, 0); //don't let drag exceed velocity
                    else
                        chunk.vel += omniDrag;

                }


                //angle the slugcat towards its movement direction
                Vector2 targetDir = Vector2.LerpUnclamped(Vector2.ClampMagnitude(0.25f * (self.bodyChunks[0].vel + self.bodyChunks[1].vel), 1f),
                    dir, dir.sqrMagnitude * 0.5f);
                Vector2 currentDir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                Vector2 correctionVec = (targetDir - currentDir) * Options.GlideAngleEnforcement;
                self.bodyChunks[0].vel += correctionVec;
                self.bodyChunks[1].vel -= correctionVec;


                //lower gravity
                if (self.mainBodyChunk.vel.y < 1f) //don't give free anti-grav for jumps!
                {
                    float antiGrav = Options.GlideAntiGrav * Mathf.Clamp01(1 + dir.y);
                    self.customPlayerGravity = BaseCustomPlayerGravity * (1f - Options.GlideAntiGrav);
                }

                //appearance
                if (self.EffectiveRoomGravity > 0)
                {
                    /*if (self.mainBodyChunk.vel.y <= Mathf.Abs(self.mainBodyChunk.vel.x))
                    {
                        self.standing = false;
                        self.animation = Player.AnimationIndex.DownOnFours;
                    }
                    else //going upwards (going more up than left/right)
                    {
                        self.standing = true;
                        self.animation = Player.AnimationIndex.None;
                    }*/
                    self.animation = Player.AnimationIndex.None;
                    self.bodyMode = Player.BodyModeIndex.Default;
                    self.standing = false;
                }

                //visual wings
                if (info.Wings != null && info.Wings.NeedsDestroy)
                    info.Wings.Destroy();

                if (info.Wings == null)
                {
                    info.Wings = new(self, info);
                    self.room.AddObject(info.Wings);
                    Plugin.Log("Added PlayerWings", 2);
                }

            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x); //made as my own function so I know it's correct


    //Anti-gravity
    private const float BaseCustomPlayerGravity = 0.9f;
    private static float Player_EffectiveRoomGravity(Func<Player, float> orig, Player self)
    {
        if (!CurrentAbilities.CanGlide) return orig(self);

        return orig(self) * self.customPlayerGravity / BaseCustomPlayerGravity; //apply customPlayerGravity
    }


    //wing sprite
    public class PlayerWings : UpdatableAndDeletable, IDrawable
    {
        public Player player;
        public PlayerInfo info;

        private float lastFlap = 0, flap = 0;
        private const float deltaFlap = (1f/40f) / 0.5f; //0.5f = half a second

        private float lastAlpha = 0, alpha = 0;

        private Vector2 lastVel, vel;

        private static Color baseColor = new(0.2f, 0.4f, 1f); //blue

        public PlayerWings(Player player, PlayerInfo info)
        {
            this.player = player;
            this.info = info;
        }

        public void Flap() => lastFlap = flap = 1;

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (NeedsDestroy)
            {
                this.Destroy();
                return;
            }

            lastFlap = flap;
            if (flap > 0) flap -= deltaFlap;
            if (flap < 0) flap = 0;

            lastVel = vel;
            vel = Vector2.LerpUnclamped(vel, player.mainBodyChunk.vel, 0.1f); //constantly lerping towards player vel

            lastAlpha = alpha;
            float targetAlpha = info.Gliding ? 1 : (1 - (1 - flap) * (1 - flap) * (1 - flap)); //if gliding = 1, else ~= flap (on a steeper curve)
            if (targetAlpha > alpha)
                alpha = Mathf.Min(Mathf.LerpUnclamped(alpha, targetAlpha, 0.2f) + 0.1f, targetAlpha); //lerp UP quickly
            else if (targetAlpha < alpha)
                alpha = Mathf.Max(Mathf.LerpUnclamped(alpha, targetAlpha, 0.1f) - 0.05f, targetAlpha); //lerp DOWN slower

        }

        public bool NeedsDestroy => player == null || room == null || player.room != room || info == null;

        public override void Destroy()
        {
            if (info != null && info.Wings == this) //remove reference in PlayerInfo
                info.Wings = null;
            base.Destroy();
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            newContainer ??= rCam.ReturnFContainer("Background");
            newContainer.AddChild(sLeaser.sprites[0]);
            newContainer.AddChild(sLeaser.sprites[1]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            //do nothing so far
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //get player pos
            Vector2 chunk0Pos = Vector2.LerpUnclamped(player.bodyChunks[0].lastPos, player.bodyChunks[0].pos, timeStacker);
            Vector2 chunk1Pos = Vector2.LerpUnclamped(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker);
            Vector2 wingDrawPos = Vector2.LerpUnclamped(chunk0Pos, chunk1Pos, 0.25f) - camPos; //mostly chunk 0, a bit of chunk1

            Vector2 chunkDir = Custom.DirVec(chunk1Pos, chunk0Pos);
            Vector2 wingDir = Perpendicular(chunkDir);

            float drawFlap = Mathf.LerpUnclamped(lastFlap, flap, timeStacker);
            float sqrFlapMod = (1 - drawFlap) * (1 - drawFlap);
            float flapOffset = drawFlap > 0
                ? -Mathf.Sin(drawFlap * 1.5f * Mathf.PI) * (1 - sqrFlapMod)
                : 0;

            Vector2 playerVel = Vector2.LerpUnclamped(lastVel, vel, timeStacker);
            float dirSqrVel = Vector2.Dot(chunkDir, playerVel.normalized) * playerVel.sqrMagnitude; //actually can be negative
            float diveOffset = (0.5f - dirSqrVel / (Mathf.Abs(dirSqrVel) + 40f)) * sqrFlapMod * 0.5f;

            playerVel.y -= room.gravity * 2; //wings go against the direction of gravity
            float sqrVel = playerVel.sqrMagnitude;
            Vector2 velOffset = -playerVel.normalized * sqrVel / (sqrVel + 20f) * sqrFlapMod * 0.5f;

            //determine which wing is "in the front"
            //int frontWing = wingDir.y < 0 ? 0 : 1;
            //int backWing = 1 - frontWing;

            float wingFold = chunkDir.y > 0
                ? 1 - chunkDir.y //unfold when pointing upwards
                    : (chunkDir.y < -0.5f
                    ? 2 * (1 + chunkDir.y) //unfold when below -0.5
                    : 1);
            wingFold *= wingFold;
            float wingWidth = 15f + 15f * (chunkDir.y < 0 ? 0 : 1 - wingFold); //from 15 to 30
            float wingHeight = 20f;
            float wingOffset = 5f; //how far out from center of slugcat

            //set vertices
            for (int y = 0; y <= 1; y++)
            {
                for (int x = 0; x <= 2; x++)
                {
                    float relX = x * 0.5f;
                    relX = 1 - (1 - relX) * (1 - relX); //put it on a curve so that 0.5 increases to 0.75
                    float offsetMod = relX * relX;
                    float relY = y - 1 + offsetMod * (flapOffset + diveOffset); //add the -1 to position the wing below the slugcat's head

                    Vector2 basePos = wingDrawPos + chunkDir * relY * wingHeight
                        + new Vector2(0, wingHeight * offsetMod * flapOffset) //flap also directly moves wings up/down
                        + velOffset * wingHeight * offsetMod; //velocity directly shifts it too
                    Vector2 offset1 = wingDir * (relX * wingWidth + wingOffset * (1 - wingFold)) * (wingDir.y < 0 ? -1 : 1); //must be positive y
                    Vector2 backOffset = chunkDir * wingOffset * Mathf.LerpUnclamped(0, -1, wingFold); //offset back wing back slightly
                    (sLeaser.sprites[0] as TriangleMesh).MoveVertice(x + y * 3, basePos + offset1 * Mathf.LerpUnclamped(-1, 0.9f, wingFold) + backOffset);
                    (sLeaser.sprites[1] as TriangleMesh).MoveVertice(x + y * 3, basePos + offset1);
                }
            }

            //set alpha
            float drawAlpha = Mathf.LerpUnclamped(lastAlpha, alpha, timeStacker);
            (sLeaser.sprites[0] as TriangleMesh).alpha = drawAlpha;
            (sLeaser.sprites[1] as TriangleMesh).alpha = drawAlpha;

            //reset container if necessary
            bool anyFront = Mathf.Abs(wingDir.y) > 0.1f; //whether any wing should be put in the foreground
            //ChangeContainer((sLeaser.sprites[0] as TriangleMesh), rCam, frontWing == 0 && anyFront); //wing 0 is always in background
            ChangeContainer((sLeaser.sprites[1] as TriangleMesh), rCam, anyFront ? "Midground" : "Background"); //wing 1 is sometimes in foreground

        }
        private static void ChangeContainer(TriangleMesh mesh, RoomCamera rCam, string containerName)
        {
            FContainer container = rCam.ReturnFContainer(containerName);
            if (mesh.container != container)
            {
                mesh.RemoveFromContainer();
                container.AddChild(mesh);
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];

            TriangleMesh.Triangle[] tris = TriangleMesh.GridTriangles(1, 2);
            Vector2[] uvs = new Vector2[]
            {
                new(0, 0), new(0.75f, 0), new(1, 0),
                new(0, 1), new(0.75f, 1), new(1, 1)
            };
            sLeaser.sprites[0] = new TriangleMesh(Tools.Assets.WingTexName, tris, false)
                { UVvertices = uvs, alpha = 0, color = baseColor, shader = Tools.Assets.WingEffect };
            sLeaser.sprites[1] = new TriangleMesh(Tools.Assets.WingTexName, tris, false)
                { UVvertices = uvs, alpha = 0, color = baseColor, shader = Tools.Assets.WingEffect };

            AddToContainer(sLeaser, rCam, null);
        }
    }

}
