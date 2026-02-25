using BepInEx;
using EasyModSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode;

//[BepInPlugin("idk.thingy", "Test Plugin", "0.0.1")] //disabled
public class TestPlugin : SimplerPlugin
{
    public override void ApplyHooks()
    {
        Logger.LogInfo("Test plugin enabled!");
    }

    public override void RemoveHooks()
    {
        throw new NotImplementedException();
    }

    public TestPlugin() : base(null) { }
}
