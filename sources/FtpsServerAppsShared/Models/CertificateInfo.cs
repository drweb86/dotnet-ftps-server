namespace FtpsServerAppsShared.Models;

public record CertificateInfo
{
    public required bool IsSelfSigned { get; init; }
    public required string Subject { get; init; }
    public required string Issuer { get; init; }
    public required string ValidFrom { get; init; }
    public required string ValidTo { get; init; }
    public required string SerialNumber { get; init; }
    public required string Sha256Fingerprint { get; init; }
    public required string Sha1Fingerprint { get; init; }
}
