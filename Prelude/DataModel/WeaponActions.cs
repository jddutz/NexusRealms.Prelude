namespace NexusRealms.Prelude.DataModel;

/// <summary>A weapon capability whose current damage, timing, and ammunition are supplied by its owned item.</summary>
public sealed record WeaponAttackAction : ActionData
{
    /// <summary>Defines a named weapon attack without learning a progression ability.</summary>
    public WeaponAttackAction(string id, ItemId item)
    {
        Id = id; ItemId = item; Classifications = ["weapon"]; Targeting = new(TargetShape.Single, TargetRelationship.OpposingTeam, TargetingFlags.Visual);
    }
}
/// <summary>A thrown-item capability whose release is an execution commitment.</summary>
public sealed record ThrowItemAction : ActionData
{
    /// <summary>Defines a named throw using an owned item stack.</summary>
    public ThrowItemAction(string id, ItemId item)
    {
        Id = id; ItemId = item; Throw = true; Classifications = ["weapon", "throw"]; Targeting = new(TargetShape.Single, TargetRelationship.OpposingTeam, TargetingFlags.Visual);
    }
}
