namespace SQLHELPER.Models;

public class ConnectionProfile
{
    public string Server { get; set; } = string.Empty;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DefaultTargetDb { get; set; } = "SIN";

    public bool Encrypt { get; set; }

    public bool TrustServerCertificate { get; set; } = true;

    public bool HasRequiredValues =>
        !string.IsNullOrWhiteSpace(Server) &&
        !string.IsNullOrWhiteSpace(User) &&
        !string.IsNullOrWhiteSpace(Password);

    public ConnectionProfile Clone() => new()
    {
        Server = Server,
        User = User,
        Password = Password,
        DefaultTargetDb = string.IsNullOrWhiteSpace(DefaultTargetDb) ? "SIN" : DefaultTargetDb,
        Encrypt = Encrypt,
        TrustServerCertificate = TrustServerCertificate
    };
}
