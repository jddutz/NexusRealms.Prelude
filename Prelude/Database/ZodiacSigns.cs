namespace NexusRealms.Prelude.Database;

/// <summary>Descriptive zodiac records presented during character creation.</summary>
public static class ZodiacSigns
{
    /// <summary>The cardinal fire sign.</summary>
    public static ZodiacSign Aries { get; } =
        new()
        {
            Id = ZodiacSignId.Aries,
            Name = "Aries",
            Description = "A bold pioneer who meets challenges head-on.",
            PlayStyle = "Take the initiative and maintain pressure.",
            Element = ZodiacElement.Fire,
            Modality = ZodiacModality.Cardinal,
            Strengths = ["Initiative", "Courage"],
            Weaknesses = ["Impatience", "Overextension"],
        };

    /// <summary>The fixed earth sign.</summary>
    public static ZodiacSign Taurus { get; } =
        new()
        {
            Id = ZodiacSignId.Taurus,
            Name = "Taurus",
            Description = "A steadfast presence who values stability and resolve.",
            PlayStyle = "Hold your ground and build a lasting advantage.",
            Element = ZodiacElement.Earth,
            Modality = ZodiacModality.Fixed,
            Strengths = ["Endurance", "Consistency"],
            Weaknesses = ["Inflexibility", "Slow adaptation"],
        };

    /// <summary>The mutable air sign.</summary>
    public static ZodiacSign Gemini { get; } =
        new()
        {
            Id = ZodiacSignId.Gemini,
            Name = "Gemini",
            Description = "A curious thinker who explores different approaches.",
            PlayStyle = "Switch between complementary tactics to exploit changing opportunities.",
            Element = ZodiacElement.Air,
            Modality = ZodiacModality.Mutable,
            Strengths = ["Versatility", "Resourcefulness"],
            Weaknesses = ["Indecision", "Divided attention"],
        };

    /// <summary>The cardinal water sign.</summary>
    public static ZodiacSign Cancer { get; } =
        new()
        {
            Id = ZodiacSignId.Cancer,
            Name = "Cancer",
            Description = "An intuitive guardian who protects what matters.",
            PlayStyle = "Anticipate threats and preserve your resources.",
            Element = ZodiacElement.Water,
            Modality = ZodiacModality.Cardinal,
            Strengths = ["Protection", "Intuition"],
            Weaknesses = ["Overcaution", "Attachment"],
        };

    /// <summary>The fixed fire sign.</summary>
    public static ZodiacSign Leo { get; } =
        new()
        {
            Id = ZodiacSignId.Leo,
            Name = "Leo",
            Description = "A confident leader who faces adversity with determination.",
            PlayStyle =
                "Assert dominance through powerful actions and sustained offensive pressure.",
            Element = ZodiacElement.Fire,
            Modality = ZodiacModality.Fixed,
            Strengths = ["Confidence", "Determination"],
            Weaknesses = ["Pride", "Predictability"],
        };

    /// <summary>The mutable earth sign.</summary>
    public static ZodiacSign Virgo { get; } =
        new()
        {
            Id = ZodiacSignId.Virgo,
            Name = "Virgo",
            Description = "A practical analyst who finds value in careful preparation.",
            PlayStyle = "Plan precisely and make efficient choices.",
            Element = ZodiacElement.Earth,
            Modality = ZodiacModality.Mutable,
            Strengths = ["Precision", "Efficiency"],
            Weaknesses = ["Overanalysis", "Perfectionism"],
        };

    /// <summary>The cardinal air sign.</summary>
    public static ZodiacSign Libra { get; } =
        new()
        {
            Id = ZodiacSignId.Libra,
            Name = "Libra",
            Description = "A thoughtful mediator who seeks balance in every decision.",
            PlayStyle = "Control the exchange of advantages through measured offense and defense.",
            Element = ZodiacElement.Air,
            Modality = ZodiacModality.Cardinal,
            Strengths = ["Balance", "Judgment"],
            Weaknesses = ["Hesitation", "Overcompromise"],
        };

    /// <summary>The fixed water sign.</summary>
    public static ZodiacSign Scorpio { get; } =
        new()
        {
            Id = ZodiacSignId.Scorpio,
            Name = "Scorpio",
            Description = "A focused strategist who pursues a goal with intensity.",
            PlayStyle = "Study an opening and exploit it with commitment.",
            Element = ZodiacElement.Water,
            Modality = ZodiacModality.Fixed,
            Strengths = ["Focus", "Persistence"],
            Weaknesses = ["Tunnel vision", "Rigidity"],
        };

    /// <summary>The mutable fire sign.</summary>
    public static ZodiacSign Sagittarius { get; } =
        new()
        {
            Id = ZodiacSignId.Sagittarius,
            Name = "Sagittarius",
            Description = "An adventurous explorer who embraces possibility.",
            PlayStyle =
                "Maintain freedom of action and exploit opportunities beyond conventional reach.",
            Element = ZodiacElement.Fire,
            Modality = ZodiacModality.Mutable,
            Strengths = ["Adaptability", "Optimism"],
            Weaknesses = ["Restlessness", "Risk-taking"],
        };

    /// <summary>The cardinal earth sign.</summary>
    public static ZodiacSign Capricorn { get; } =
        new()
        {
            Id = ZodiacSignId.Capricorn,
            Name = "Capricorn",
            Description = "A disciplined builder who works steadily toward a goal.",
            PlayStyle = "Manage resources and build an advantage over time.",
            Element = ZodiacElement.Earth,
            Modality = ZodiacModality.Cardinal,
            Strengths = ["Discipline", "Planning"],
            Weaknesses = ["Overcaution", "Inflexibility"],
        };

    /// <summary>The fixed air sign.</summary>
    public static ZodiacSign Aquarius { get; } =
        new()
        {
            Id = ZodiacSignId.Aquarius,
            Name = "Aquarius",
            Description =
                "An independent thinker who challenges established rules and disrupts opposing forces.",
            PlayStyle = "Disrupt enemy capabilities and reshape the rules of engagement.",
            Element = ZodiacElement.Air,
            Modality = ZodiacModality.Fixed,
            Strengths = ["Disruption", "Control"],
            Weaknesses = ["Indirect damage", "Specialization"],
        };

    /// <summary>The mutable water sign.</summary>
    public static ZodiacSign Pisces { get; } =
        new()
        {
            Id = ZodiacSignId.Pisces,
            Name = "Pisces",
            Description = "An imaginative observer who responds to changing circumstances.",
            PlayStyle = "Anticipate threats and turn an opponent's momentum to your advantage.",
            Element = ZodiacElement.Water,
            Modality = ZodiacModality.Mutable,
            Strengths = ["Intuition", "Flexibility"],
            Weaknesses = ["Indecision", "Lack of focus"],
        };

    /// <summary>Every authored sign, ordered by its stable identity.</summary>
    public static ZodiacSign[] All { get; } =
    [
        Aries,
        Taurus,
        Gemini,
        Cancer,
        Leo,
        Virgo,
        Libra,
        Scorpio,
        Sagittarius,
        Capricorn,
        Aquarius,
        Pisces,
    ];
}
