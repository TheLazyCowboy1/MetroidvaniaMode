using BepInEx;
using EasyModSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode;

[BepInPlugin("Author.TestMod", "Test Mod", "0.0.1")]
public class TestPlugin : SimplerPlugin
{
    public TestPlugin() : base(new Options())
    {

    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ApplyHooks()
    {
        throw new NotImplementedException();
    }

    public override void RemoveHooks()
    {
        throw new NotImplementedException();
    }

}
