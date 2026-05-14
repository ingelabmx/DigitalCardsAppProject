namespace DigitalCards.Web.Pilot;

public sealed class PilotOptions
{
    public const string SectionName = "DigitalCards:Pilot";

    public bool Enabled { get; set; }

    public string[] AllowedBusinessIds { get; set; } = [];

    public string[] AllowedBusinessEmails { get; set; } = [];

    public string BlockedBusinessMessage { get; set; } =
        "Este negocio no esta activo en Puntelio. Contacta al administrador.";
}
