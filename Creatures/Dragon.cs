using MetroidvaniaMode.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Creatures;

public class Dragon : Lizard
{
    [EasyExtEnum]
    public static CreatureTemplate.Type PinkDragon;

    /// <summary>
    /// Basic constructor, never intended to be used directly
    /// </summary>
    public Dragon(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {
    }

}
