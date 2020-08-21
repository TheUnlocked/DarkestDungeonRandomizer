using System;
using System.Collections.Generic;
using System.Text;

namespace DarkestDungeonRandomizer.DDTypes
{
    public record Curio(
        /// <summary>
        /// The internal ID number for the curio.
        /// </summary>
        int Id,
        /// <summary>
        /// The formatted English name for the curio.
        /// </summary>
        string Name,
        /// <summary>
        /// The internal ID name for the curio.
        /// </summary>
        string IdString,
        /// <summary>
        /// Whether the curio is considered "good," "bad," or "mixed."
        /// </summary>
        CurioQuality Quality,
        /// <summary>
        /// The region in which the curio is found.
        /// </summary>
        Region RegionFound,
        /// <summary>
        /// (UNCONFIRMED)
        /// True if the curio can be found in dungeons normally, false if it is a special curio.
        /// </summary>
        bool FullCurio
    )
    {
        /// <summary>
        /// (UNCONFIRMED)
        /// A list of tags describing how the curio interacts with quirks. A curio can have at most six tags and should include the tag "All".
        /// "CCrave" may be used on curios which interact with a craving hero with the crimson curse.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; } = new[] { "All" };
        /// <summary>
        /// A weighted list of curio effects.
        /// </summary>
        public CurioEffectList Effects { get; init; } = new CurioEffectList();
        /// <summary>
        /// A list of bindings between items and curio interaction effects. There may be a maximum of 4 item interactions.
        /// </summary>
        public IReadOnlyList<(string item, CurioEffect effect)> ItemIteractions { get; init; } = new (string item, CurioEffect effect)[0];
    }

    public enum CurioQuality
    {
        Good,
        Mixed,
        Bad
    }

    public enum CurioEffectType
    {
        Nothing,
        Loot,
        Quirk,
        Effect,
        Purge,
        Scouting,
        Teleport,
        Disease,
        Summon
    }

    public enum CurioTracker
    {
        Nothing,
        Loot,
        HealGeneral,
        HealStress,
        PurgeNegative,
        Buff,
        Debuff,
        QuirkPositive,
        QuirkNegative,
        Summon
    }

    public record CurioEffectList
    {
        public WeightedCurioEffect Nothing { get; init; } = new WeightedCurioEffect(CurioEffectType.Nothing);
        public WeightedCurioEffect Loot { get; init; } = new WeightedCurioEffect(CurioEffectType.Loot);
        public WeightedCurioEffect Quirk { get; init; } = new WeightedCurioEffect(CurioEffectType.Quirk);
        public WeightedCurioEffect Effect { get; init; } = new WeightedCurioEffect(CurioEffectType.Effect);
        public WeightedCurioEffect Purge { get; init; } = new WeightedCurioEffect(CurioEffectType.Purge);
        public WeightedCurioEffect Scouting { get; init; } = new WeightedCurioEffect(CurioEffectType.Scouting);
        public WeightedCurioEffect Teleport { get; init; } = new WeightedCurioEffect(CurioEffectType.Teleport);
        public WeightedCurioEffect Disease { get; init; } = new WeightedCurioEffect(CurioEffectType.Disease);
    }

    public record CurioEffect(CurioEffectType Type)
    {
        /// <summary>
        /// Result fields mean different things depending on the effect type.
        /// <list type="table">
        ///     <item>
        ///         <term>Nothing</term>
        ///         <description>Does Nothing.</description>
        ///     </item>
        ///     <item>
        ///         <term>Loot</term>
        ///         <description>The loot table to pull from.</description>
        ///     </item>
        ///     <item>
        ///         <term>Quirk</term>
        ///         <description>
        ///             The internal ID string for the quirk to be added. "positive" or "negative" may also be used
        ///             to add a random positive or negative quirk respectively. Exact behavior unknown.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Effect</term>
        ///         <description>
        ///             The effect to apply. This can be things like "Bleed 4", "Stress 3", "PristineFountainHealStress, or "fish_idol_curse_1".
        ///             Exact behavior unknown.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Purge</term>
        ///         <description>The base game only uses the term "negative", presumably purging a random negative perk.</description>
        ///     </item>
        ///     <item>
        ///         <term>Scouting</term>
        ///         <description>
        ///             Scouts ahead using the format "[number] - [kind]". The kinds used in the base game are "all", "hall_battles", "room_battles", 
        ///             "obstacles", "traps, and "curios". The numbers start from 0 and affect the distance scouted. Exact behavior unknown.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>Teleport</term>
        ///         <description>Unused and does nothing.</description>
        ///     </item>
        ///     <item>
        ///         <term>Disease</term>
        ///         <description>The internal ID of the disease to be added, or "random" for a random disease.</description>
        ///     </item>
        /// </list>
        /// </summary>
        public string? Result1 { get; init; }
        /// <summary>
        /// For loot represents the number of pulls from the specified loot table to perform.
        /// For other effects represents a weight of selecting that result over other listed results.
        /// </summary>
        public int? Result1Weight { get; init; }
        /// <inheritdoc cref="Result1"/>
        public string? Result2 { get; init; }
        /// <inheritdoc cref="Result1Weight"/>
        public int? Result2Weight { get; init; }
        /// <inheritdoc cref="Result1"/>
        public string? Result3 { get; init; }
        /// <inheritdoc cref="Result1Weight"/>
        public int? Result3Weight { get; init; }
        /// <summary>
        /// The icon to be used for curio tracking.
        /// </summary>
        public CurioTracker? CurioTrackerId { get; init; }
        /// <summary>
        /// Unused?
        /// </summary>
        public string? Notes { get; init; }
    }

    public record WeightedCurioEffect(CurioEffectType Type) : CurioEffect(Type)
    {
        /// <summary>
        /// The selection weight when the game is choosing which curio effect to apply.
        /// </summary>
        public int Weight { get; init; }
    }
}
