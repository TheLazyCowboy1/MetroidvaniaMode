using System;
using System.IO;
using UnityEngine;

namespace MetroidvaniaMode.Tools;

public static class Assets
{
    private static Shader _shieldEffect;
    public static FShader ShieldEffect;

    public static Texture2D ColoredNoiseTex;

    private static Shader _warpNoiseBloom;
    public static FShader WarpNoiseBloom;

    public const string WingTexName = "MVM_Wing";
    //private static Texture2D wingTex;

    private static Shader _wingEffect;
    public static FShader WingEffect;

    public static void Load()
    {
        try
        {
            AssetBundle assets = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(Path.Combine("AssetBundles", "MVM.assets")));

            _shieldEffect = assets.LoadAsset<Shader>("ShieldEffect.shader");
            if (_shieldEffect == null) Plugin.Error("ShieldEffect.shader is null!");
            ShieldEffect = FShader.CreateShader("MVM_ShieldEffect", _shieldEffect);

            ColoredNoiseTex = assets.LoadAsset<Texture2D>("ColoredFractalNoise.png");
            if (ColoredNoiseTex == null) Plugin.Error("ColoredFractalNoise.png is null!");
            Shader.SetGlobalTexture("TheLazyCowboy1_ColoredNoiseTex", ColoredNoiseTex);

            _warpNoiseBloom = assets.LoadAsset<Shader>("WarpNoiseTex.shader");
            if (_warpNoiseBloom == null) Plugin.Error("WarpNoiseTex.shader is null!");
            WarpNoiseBloom = FShader.CreateShader("MVM_WarpNoiseBloom", _warpNoiseBloom);

            Texture2D wingTex = assets.LoadAsset<Texture2D>("Wing.png");
            if (wingTex == null) Plugin.Error("Wing.png is null!");
            Futile.atlasManager.LoadAtlasFromTexture(WingTexName, wingTex, false);

            _wingEffect = assets.LoadAsset<Shader>("Wing.shader");
            if (_wingEffect == null) Plugin.Error("Wing.shader is null!");
            WingEffect = FShader.CreateShader("MVM_WingEffect", _wingEffect);

            Plugin.Log("Loaded assets", 0);

            //TEMP
            //string wingFile = AssetManager.ResolveFilePath(Path.Combine("AssetBundles", "Wing.png"));
            //wingFile = wingFile.Substring(0, wingFile.Length - ".png".Length); //cut off the last 4 characters: ".png"
            //var at = Futile.atlasManager.ActuallyLoadAtlasOrImage(WingTexName, wingFile, "");
            //Plugin.Log("Wing tex name: " + at?.name, 0);

        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

}
