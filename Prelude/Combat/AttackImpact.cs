namespace NexusRealms.Prelude.Combat;

/// <summary>Source artwork for a brief target overlay, independent of typed damage.</summary>
public sealed record AttackImpact(string? EffectTexture = null, string? IconTexture = null)
{
    public static AttackImpact Blocked { get; } = new("effects.blocked.png");

    public static AttackImpact ForStrike(HandStrike strike) => new(
        strike.Item?.Weapon?.ImpactTexture ?? "effects.impact_01.png",
        strike.Item?.Weapon is { IsMelee: false } ? strike.Ammunition?.Icon : null);
    public static AttackImpact ForThrow(CarriedItem item) => new(IconTexture: item.Icon);
}
