namespace nlwc.ems.Core;

/// <summary>
/// The admin-facing lifecycle mapped onto three separately stored fields — visibility, registration window and live
/// state — so the three can be validated and queried independently (GAP-02). Archiving is a stage, never a delete.
/// </summary>
public static class EditionLifecycle
{
    public sealed record Fields(EditionVisibility Visibility, RegistrationWindow Registration, EditionLiveState Live);

    public static readonly IReadOnlyDictionary<EditionStage, Fields> Stages = new Dictionary<EditionStage, Fields>
    {
        [EditionStage.Draft] = new(EditionVisibility.Draft, RegistrationWindow.NotOpen, EditionLiveState.NotLive),
        [EditionStage.Preview] = new(EditionVisibility.Preview, RegistrationWindow.NotOpen, EditionLiveState.NotLive),
        [EditionStage.RegistrationOpen] = new(EditionVisibility.Public, RegistrationWindow.Open, EditionLiveState.NotLive),
        [EditionStage.Closed] = new(EditionVisibility.Public, RegistrationWindow.Closed, EditionLiveState.NotLive),
        [EditionStage.Live] = new(EditionVisibility.Public, RegistrationWindow.Closed, EditionLiveState.Live),
        [EditionStage.Completed] = new(EditionVisibility.Public, RegistrationWindow.Closed, EditionLiveState.Completed),
        [EditionStage.Archived] = new(EditionVisibility.Archived, RegistrationWindow.Closed, EditionLiveState.Completed),
    };

    private static readonly Dictionary<EditionStage, EditionStage[]> Allowed = new()
    {
        [EditionStage.Draft] = [EditionStage.Preview],
        [EditionStage.Preview] = [EditionStage.Draft, EditionStage.RegistrationOpen],
        [EditionStage.RegistrationOpen] = [EditionStage.Closed],
        [EditionStage.Closed] = [EditionStage.RegistrationOpen, EditionStage.Live],
        [EditionStage.Live] = [EditionStage.Completed],
        [EditionStage.Completed] = [EditionStage.Archived],
        [EditionStage.Archived] = [],
    };

    public static EditionStage? StageOf(EditionVisibility v, RegistrationWindow r, EditionLiveState l)
    {
        var fields = new Fields(v, r, l);
        foreach (var (stage, f) in Stages) if (f == fields) return stage;
        return null;
    }

    public static bool CanMove(EditionStage from, EditionStage to) => Allowed.TryGetValue(from, out var next) && next.Contains(to);
}
