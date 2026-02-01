using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.AI;
/// <summary>
/// An AI that exists within a region solely to manipulate the behaviors of other AIs
/// </summary>
public abstract class WorldAI : ArtificialIntelligence
{
    public WorldAI(World world) : base( //creature is initialized as a WorldAITemplate creature trapped in the OffScreenDen
        new(world, StaticWorld.GetCreatureTemplate(Creatures.CustomCreatures.WorldAITemplate), null, new(world.offScreenDen.index, -1, -1, -1), world.game.GetNewID()),
        world)
    {
        //...do I even need anything more for this AI? Isn't this good enough?
    }

    #region CreatureOwnership

    private readonly static Hashtable CreatureOwnerships = new(new EntityIDComparer());

    private class EntityIDComparer : IEqualityComparer
    {
        public new bool Equals(object x, object y) => ((EntityID)x).number == ((EntityID)y).number;
        public int GetHashCode(object obj) => ((EntityID)obj).number;
    }

    public static void ClearStaticData() => CreatureOwnerships.Clear();

    public static WorldAI GetCreatureOwner(AbstractCreature crit) => GetCreatureOwner(crit.ID);
    public static WorldAI GetCreatureOwner(EntityID id) => CreatureOwnerships[id] as WorldAI; //CreatureOwnerships.ContainsKey(id) ? CreatureOwnerships[id] as WorldAI : null;

    public void ClaimCreatureOwnership(AbstractCreature crit) => ClaimCreatureOwnership(crit.ID);
    public void ClaimCreatureOwnership(EntityID id)
    {
        //if (CreatureRelationshipControllers.ContainsKey(id))
            CreatureOwnerships[id] = this; //this should work in theory
        //else
            //CreatureRelationshipControllers.Add(id, this);
    }

    public abstract CreatureTemplate.Relationship RelationshipForOwnedCreature(AbstractCreature ownedCreature, AbstractCreature otherCreature, CreatureTemplate.Relationship defaultRelationship);

    #endregion

}
