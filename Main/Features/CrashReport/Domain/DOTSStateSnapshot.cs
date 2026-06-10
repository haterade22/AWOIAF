using System.Collections.Generic;

namespace DOTS.Features.CrashReport.Domain;

// DOTS feature state captured at crash time. Each subsystem snapshot is nullable
// so a crash before the campaign loads still produces a coherent report.
public sealed record DotsStateSnapshot(
    CareerStateSnapshot? Career,
    IReadOnlyList<SpecialResourceEntry> SpecialResources,
    CulturalFeatsStateSnapshot? CulturalFeats,
    RevoltTuningStateSnapshot? RevoltTuning,
    int? ActiveMessengersCount);

public sealed record CareerStateSnapshot(
    string? SelectedCareerId,
    int CareerLevel,
    IReadOnlyList<string> ActivePassives);

public sealed record SpecialResourceEntry(
    string KingdomId,
    string ResourceId,
    int Balance);

public sealed record CulturalFeatsStateSnapshot(
    string? PlayerCultureId,
    IReadOnlyList<string> ActiveFeats);

public sealed record RevoltTuningStateSnapshot(
    float LoyaltyThresholdToRevolt,
    float CooldownDaysBetweenRevolts,
    float DifferentCultureLoyaltyPenalty,
    float OccupierKingdomLoyaltyPenalty);
