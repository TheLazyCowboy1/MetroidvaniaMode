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

            Plugin.Log("Loaded assets");

        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

}
