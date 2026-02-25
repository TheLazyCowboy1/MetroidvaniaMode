using EasyModSetup.MeadowCompat;
using MetroidvaniaMode.Tools;
using RainMeadow;
using PlayerInfo = MetroidvaniaMode.Tools.PlayerInfo;

namespace MetroidvaniaMode;

public class PlayerInfoSyncState : EasyEntityState
{
    public override bool AttachTo(OnlineEntity entity) => (entity is OnlineCreature oc) && oc.creature.state is PlayerState; //players

    [OnlineField]
    bool Gliding;
    [OnlineFieldHalf(group = "shieldStrength")]
    float ShieldStrength;
    [OnlineFieldHalf(group = "shieldDir")]
    float ShieldDir;

    public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
    {
        PlayerInfo info = ((onlineEntity as OnlineCreature)?.apo?.realizedObject as Player)?.GetInfo();
        if (info == null) return;
        info.Gliding = Gliding;
        info.ShieldStrength = ShieldStrength;
        info.ShieldDir = ShieldDir;
    }

    public override void WriteTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
    {
        PlayerInfo info = ((onlineEntity as OnlineCreature)?.apo?.realizedObject as Player)?.GetInfo();
        if (info == null) return;
        Gliding = info.Gliding;
        ShieldStrength = info.ShieldStrength;
        ShieldDir = info.ShieldDir;
    }
}
