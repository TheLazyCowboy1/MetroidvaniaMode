using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode;

public class Options : AutoConfigOptions
{
    public Options() : base(new string[] { "Options", "Abilities" })
    {

    }

    [TabAtt("Abilities", "Jump Boost", "Multiplies the jump boost, which heavily affects jump height"), LimitRange(0, 10f)]
    public static float JumpBoost = 0;

    [TabAtt("Abilities", "Climb Vertical Poles", "Allows the slugcat to grab vertical poles")]
    public static bool ClimbVerticalPoles = false;

    [TabAtt("Abilities", "Can Wall Jump", "Allows the slugcat to wall jump")]
    public static bool WallJump = false;
}
