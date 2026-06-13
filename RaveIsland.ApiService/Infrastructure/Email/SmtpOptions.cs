namespace RaveIsland.ApiService.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.example.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "placeholder";
    public string Password { get; set; } = "placeholder";
    public string FromAddress { get; set; } = "noreply@raveisland.local";
    public string FromName { get; set; } = "Rave Island";
    public bool EnableSsl { get; set; } = true;
}
