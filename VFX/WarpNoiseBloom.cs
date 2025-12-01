using MetroidvaniaMode.Tools;
using System;

namespace MetroidvaniaMode.VFX;

public static class WarpNoiseBloom
{
    [EasyExtEnum]
    public static RoomSettings.RoomEffect.Type WarpNoiseEffectType;

    public static void ApplyHooks()
    {
        On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
        On.RoomSettings.RoomEffect.GetSliderCount += RoomEffect_GetSliderCount;
        On.RoomSettings.RoomEffect.GetSliderDefault += RoomEffect_GetSliderDefault;
        On.RoomSettings.RoomEffect.GetSliderName += RoomEffect_GetSliderName;
    }

    public static void RemoveHooks()
    {
        On.RoomCamera.ApplyPalette -= RoomCamera_ApplyPalette;
        On.RoomSettings.RoomEffect.GetSliderCount -= RoomEffect_GetSliderCount;
        On.RoomSettings.RoomEffect.GetSliderDefault -= RoomEffect_GetSliderDefault;
        On.RoomSettings.RoomEffect.GetSliderName -= RoomEffect_GetSliderName;
    }

    private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
    {
        orig(self);

        try
        {
            if (self.room == null) return;

            RoomSettings.RoomEffect effect = self.room.roomSettings.GetEffect(WarpNoiseEffectType);
            if (effect == null) return;

            if (effect.amount > 0f)
            {
                self.SetUpFullScreenEffect("Bloom");
                self.fullScreenEffect.shader = Assets.WarpNoiseBloom;
                self.lightBloomAlphaEffect = RoomSettings.RoomEffect.Type.Bloom;
                self.lightBloomAlpha = effect.amount;

                //apply colors
                self.fullScreenEffect.color = new(effect.extraAmounts[0], effect.extraAmounts[1], effect.extraAmounts[2]);

                Plugin.Log($"Set up WarpNoiseBloom effect in room {self.room.abstractRoom.name}. Amounts: {effect.amount}, {effect.extraAmounts[0]}, {effect.extraAmounts[1]}, {effect.extraAmounts[2]}", 2);
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }


    private static int RoomEffect_GetSliderCount(On.RoomSettings.RoomEffect.orig_GetSliderCount orig, RoomSettings.RoomEffect.Type type)
    {
        if (type == WarpNoiseEffectType) return 4;

        return orig(type);
    }

    private static float RoomEffect_GetSliderDefault(On.RoomSettings.RoomEffect.orig_GetSliderDefault orig, RoomSettings.RoomEffect.Type type, int index)
    {
        if (type == WarpNoiseEffectType)
        {
            return index switch
            {
                1 => 1f,
                2 => 0.8f,
                _ => 0.5f
            };
        }

        return orig(type, index);
    }

    private static string RoomEffect_GetSliderName(On.RoomSettings.RoomEffect.orig_GetSliderName orig, RoomSettings.RoomEffect.Type type, int index)
    {
        if (type == WarpNoiseEffectType)
        {
            return index switch
            {
                0 => "Intensity",
                1 => "Red",
                2 => "Green",
                3 => "Blue",
                _ => "Unknown"
            };
        }

        return orig(type, index);
    }

}
