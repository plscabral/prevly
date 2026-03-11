namespace Prevly.Application.SocialSecurityRegistration.Integrations.Settings;

public sealed class NitOwnershipCheckerSettings
{
    public string Provider { get; init; } = "Playwright";
    public bool Headless { get; init; } = true;
    public int TimeoutMs { get; init; } = 30000;
    public string? QueryUrl { get; init; }
    public string NitInputSelector { get; init; } = "input[name='nit']";
    public string SubmitButtonSelector { get; init; } = "button[type='submit']";
    public string? ResultReadySelector { get; init; }
    public IReadOnlyCollection<string> OwnedIndicators { get; init; } = ["ja cadastrado", "vinculado", "pertence a outra pessoa"];
    public IReadOnlyCollection<string> NotOwnedIndicators { get; init; } = ["nao localizado", "sem vinculo", "disponivel"];
}
