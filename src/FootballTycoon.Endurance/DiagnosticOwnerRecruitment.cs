using FootballTycoon.Core;

namespace FootballTycoon.Endurance;

public enum OwnerRecruitmentMode { None, CoverShortages }
public sealed record OwnerRecruitmentDecision(int Week, string Outcome, int? PlayerId, string? PlayerName, string[] BlockingReasons);
public sealed record OwnerRecruitmentCounts(int Approved, int Blocked, int NoRecommendation);

public static class DiagnosticOwnerRecruitment
{
    // A disclosed diagnostic owner policy. Production still requires an explicit owner decision.
    public static OwnerRecruitmentDecision? TryApprove(World world, OwnerRecruitmentMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (mode == OwnerRecruitmentMode.None || Contracts.Shortages(world.OwnedClub.Players).IsEmpty || !FreeAgents.CanReview(world)) return null;
        var proposal = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        var player = proposal.Recruitment?.Player;
        if (player is null) return new(world.Week, "NoRecommendation", null, null, proposal.BlockingReasons.ToArray());
        if (!proposal.BlockingReasons.IsEmpty)
            return new(world.Week, "Blocked", player.Id.Value, player.Name, proposal.BlockingReasons.ToArray());
        Proposals.Commit(world, $"endurance-owner/{world.Season}/{world.Week}/{world.History.Count}", world.Revision, proposal);
        return new(world.Week, "Approved", player.Id.Value, player.Name, []);
    }
}
