using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FootballTycoon.Core;

public static class MandateCommands
{
    public static MandateProposal Preview(World world, ChangeMandates command)
    {
        ArgumentNullException.ThrowIfNull(command);
        WorldFactory.Validate(world);
        var reasons = ImmutableArray.CreateBuilder<string>();
        if (string.IsNullOrWhiteSpace(command.CommandId) || command.CommandId.Length > 128)
            reasons.Add("A bounded command ID is required.");
        if (command.ExpectedRevision != world.Revision) reasons.Add("The world changed. Refresh the mandate proposal.");
        if (!Enum.IsDefined(command.Manager) || !Enum.IsDefined(command.Director)) reasons.Add("Unknown staff mandate.");
        if (world.Status != CareerStatus.Active && !(world.Status == CareerStatus.SeasonReview && world.Season < Seasons.PlayableSeasons))
            reasons.Add("Mandates can change only during active ownership or a nonterminal season review.");
        if (command.Manager == world.Mandates.Manager && command.Director == world.Mandates.Director)
            reasons.Add("These mandates are already in force.");
        var pending = ImmutableArray.CreateBuilder<string>();
        var names = PlayerHistory.Names(world);
        foreach (var bid in world.Negotiations.OrderBy(b => b.DecisionId))
            pending.Add($"Approved paid approach for {names[bid.PlayerId]}: fee ceiling {Money.Format(bid.FeeCeiling)}, "
                + $"wage {Money.Format(bid.WeeklyWage)}/week, expiry week {bid.ExpiryWeek}. Its existing terms and acceptance attempt remain in force.");
        foreach (var bid in world.FreeAgentBids.OrderBy(b => b.DecisionId))
            pending.Add($"Approved free-agent approach for {names[bid.PlayerId]}: wage {Money.Format(bid.WeeklyWage)}/week "
                + $"through week {bid.ContractEndWeek}, expiry week {bid.ExpiryWeek}. Its recorded origin (unknown for legacy approvals), terms and acceptance attempt remain in force.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(WorldCodec.Encode(world)));
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"mandates:{fingerprint}:{JsonSerializer.Serialize(command)}")));
        return new(id, fingerprint, command, world.Mandates, pending.ToImmutable(), reasons.ToImmutable(),
            "Changes apply to future owned-club selection and free-agent recommendations. Paid recruitment is unchanged. "
            + "There is no immediate charge or guaranteed football or development result. Existing commitments and required capital/renewal decisions remain in force.");
    }

    // Like investment commands, a used ID returns its original receipt, even across command families.
    // The caller supplies a private candidate and publishes it only after durable persistence succeeds.
    public static CommitReceipt Commit(World world, MandateProposal supplied)
    {
        ArgumentNullException.ThrowIfNull(supplied);
        ArgumentNullException.ThrowIfNull(supplied.Command);
        var command = supplied.Command;
        if (string.IsNullOrWhiteSpace(command.CommandId) || command.CommandId.Length > 128)
            throw new InvalidOperationException("A bounded command ID is required.");
        if (world.Commands.TryGetValue(command.CommandId, out var previous)) return previous;
        var proposal = Preview(world, command);
        if (proposal.Id != supplied.Id || proposal.WorldFingerprint != supplied.WorldFingerprint || proposal.Before != supplied.Before
            || proposal.Effect != supplied.Effect || supplied.PendingCommitments.IsDefault || supplied.BlockingReasons.IsDefault
            || !proposal.PendingCommitments.SequenceEqual(supplied.PendingCommitments) || !proposal.BlockingReasons.SequenceEqual(supplied.BlockingReasons))
            throw new InvalidOperationException("Mandate proposal identity does not match these terms. Refresh the proposal.");
        if (!proposal.BlockingReasons.IsEmpty) throw new InvalidOperationException(string.Join(" ", proposal.BlockingReasons));
        var revision = checked(world.Revision + 1);
        var after = new MandateState(command.Manager, command.Director, checked(world.Mandates.Revision + 1));
        var receipt = new CommitReceipt(command.CommandId, proposal.Id, revision,
            $"Mandates changed: manager {after.Manager}, director {after.Director}. Effective prospectively from week {world.Week}.");
        world.MandateHistory.Add(new(command.CommandId, world.Week, revision, world.Mandates, after));
        world.Mandates = after;
        world.Revision = revision;
        world.Commands.Add(command.CommandId, receipt);
        WorldFactory.Validate(world);
        return receipt;
    }
}
