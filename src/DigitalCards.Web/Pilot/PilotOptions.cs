namespace DigitalCards.Web.Pilot;

public sealed class PilotOptions
{
    public const string SectionName = "DigitalCards:Pilot";

    public bool Enabled { get; set; }

    public string[] AllowedBusinessIds { get; set; } = [];

    public string[] AllowedBusinessEmails { get; set; } = [];

    public string[] AllowedClientEmails { get; set; } = [];

    public string[] AllowedClientEmailDomains { get; set; } = [];

    public string BlockedBusinessMessage { get; set; } =
        "Este negocio no esta habilitado para el piloto moderno.";

    public string BlockedClientMessage { get; set; } =
        "Este cliente no esta habilitado para el piloto moderno.";
}
