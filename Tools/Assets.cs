using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MetroidvaniaMode.Tools;

public static class Assets
{
    private static Shader _shieldEffect;
    public static FShader ShieldEffect;

    public static void Load()
    {
        try
        {
            AssetBundle assets = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(Path.Combine("AssetBundles", "MVM.assets")));

            _shieldEffect = assets.LoadAsset<Shader>("ShieldEffect.shader");
            if (_shieldEffect == null) Plugin.Error("ShieldEffect.shader is null!");
            ShieldEffect = FShader.CreateShader("MVM_ShieldEffect", _shieldEffect);

            //load the other stuff too of course
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

}
