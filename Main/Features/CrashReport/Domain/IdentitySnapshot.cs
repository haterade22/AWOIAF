namespace DOTS.Features.CrashReport.Domain;

public sealed record IdentitySnapshot(
    string BannerlordVersion,
    string BannerlordExeFileVersion,
    string DotsVersion,
    string DotsDllSha1,
    string OriginatingPatchTarget,
    string LanguageCode);
