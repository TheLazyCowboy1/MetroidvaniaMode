using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.AI;

public static class AIHooks
{
    public static void ApplyHooks()
    {
        On.ArtificialIntelligence.StaticRelationship += ArtificialIntelligence_StaticRelationship;
        On.ArtificialIntelligence.DynamicRelationship_CreatureRepresentation_AbstractCreature += ArtificialIntelligence_DynamicRelationship;
    }

    public static void RemoveHooks()
    {
        On.ArtificialIntelligence.StaticRelationship -= ArtificialIntelligence_StaticRelationship;
        On.ArtificialIntelligence.DynamicRelationship_CreatureRepresentation_AbstractCreature -= ArtificialIntelligence_DynamicRelationship;
    }

    private static CreatureTemplate.Relationship ArtificialIntelligence_StaticRelationship(On.ArtificialIntelligence.orig_StaticRelationship orig, ArtificialIntelligence self, AbstractCreature otherCreature)
    {
        return orig(self, otherCreature);
    }

    private static CreatureTemplate.Relationship ArtificialIntelligence_DynamicRelationship(On.ArtificialIntelligence.orig_DynamicRelationship_CreatureRepresentation_AbstractCreature orig, ArtificialIntelligence self, Tracker.CreatureRepresentation rep, AbstractCreature absCrit)
    {
        return orig(self, rep, absCrit);
    }
}
