namespace SQLHELPER.Models;

public record ServerInformation(
    string ServerName,
    string Edition,
    string ProductVersion,
    string ProductLevel);

public record DatabaseOverview(
    string Name,
    string State,
    string RecoveryModel,
    int CompatibilityLevel,
    decimal SizeMb);
