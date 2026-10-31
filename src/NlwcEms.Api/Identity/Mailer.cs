namespace NlwcEms.Api.Identity;

public interface IMailer
{
    Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data);
}

/// <summary>Placeholder until the notifications outbox lands. Writes nothing outside development.</summary>
public sealed class ConsoleMailer(IHostEnvironment env, ILogger<ConsoleMailer> log) : IMailer
{
    public Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data)
    {
        if (env.IsDevelopment()) log.LogInformation("[mail] {Template} -> {To} {Data}", template, to, data);
        return Task.CompletedTask;
    }
}
