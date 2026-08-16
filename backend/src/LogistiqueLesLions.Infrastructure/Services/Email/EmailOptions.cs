namespace LogistiqueLesLions.Infrastructure.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Resend" | "Console". Default: Console.</summary>
    public string Provider { get; set; } = "Console";

    /// <summary>
    /// Remitente por defecto. Se sobreescribe con <c>Email__FromAddress</c>, y tiene que
    /// estar en un dominio verificado ante el proveedor: si no, rechaza el envío.
    /// </summary>
    public string FromAddress { get; set; } = "no-reply@yoonuauto.com";
    public string FromName    { get; set; } = "Yoon u Auto";

    public ResendOptions Resend { get; set; } = new();
}

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.resend.com";
}
