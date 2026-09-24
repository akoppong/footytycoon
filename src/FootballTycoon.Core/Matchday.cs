using System.Collections.Immutable;

namespace FootballTycoon.Core;

public enum Availability { Available, Injured, Suspended }
// ReturnWeek is the first career week the player is fit; a suspension is served over club matches, not weeks.
public sealed record PlayerAvailability(PersonId Player, Availability Status, int ReturnWeek, int MatchesBanned, int SeasonYellows);

// Aggregate matchday model: availability-aware 1-4-4-2 selection, chances, discipline, injuries and ratings.
// Tuned for lower-league rates: about 2.6 goals, 3.7 cautions and 0.13 dismissals per match, 0.3 injuries per side.
public static class Matchday
{
    public const int YellowsPerBan = 5;
    private const int StraightRedBan = 3, SecondYellowBan = 1;
    // Per mille rates. Conversion is per chance; the others are per side per 15-minute segment.
    private const int BaseConversion = 97, HomeConversion = 25, YellowRate = 170, RedRate = 5, InjuryRate = 50;
    private static readonly Role[] Formation = [Role.Goalkeeper, Role.Defender, Role.Defender, Role.Defender, Role.Defender,
        Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Forward, Role.Forward];

    public static bool IsAvailable(Player player, int week) => player.SuspendedMatches == 0 && week >= player.InjuredUntilWeek;
    public static PlayerAvailability Status(Player player, int week) => new(player.Id,
        week < player.InjuredUntilWeek ? Availability.Injured : player.SuspendedMatches > 0 ? Availability.Suspended : Availability.Available,
        Math.Max(week, player.InjuredUntilWeek), player.SuspendedMatches, player.SeasonYellows);

    // Best available player for each slot by role. An empty slot takes the nearest available role
    // (an outfielder only goes in goal as a last resort); with fewer than eleven eligible players the side plays short.
    public static ImmutableArray<(Player Player, Role Position)> Select(Club club, int week)
    {
        var pool = club.Players.Where(p => IsAvailable(p, week)).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).ToList();
        var picks = new List<(Player Player, Role Position)>(); var open = new List<Role>();
        foreach (var slot in Formation)
        {
            var player = pool.FirstOrDefault(p => p.Role == slot);
            if (player is null) { open.Add(slot); continue; }
            pool.Remove(player); picks.Add((player, slot));
        }
        foreach (var slot in open)
        {
            var player = pool.OrderBy(p => Distance(p.Role, slot)).FirstOrDefault();
            if (player is null) break;
            pool.Remove(player); picks.Add((player, slot));
        }
        return picks.OrderBy(p => p.Position).ToImmutableArray();
    }

    private static int Distance(Role role, Role slot) => role == slot ? 0 : slot == Role.Goalkeeper ? (int)role : role == Role.Goalkeeper ? 9 : Math.Abs(role - slot);
    private static int Effective(Player player, Role position) => player.Ability - (player.Role == position ? 0
        : player.Role == Role.Goalkeeper || position == Role.Goalkeeper ? 25 : 8 * Math.Abs(player.Role - position));
    private static int ScoreWeight(Role position) => position switch { Role.Forward => 16, Role.Midfielder => 4, Role.Defender => 2, _ => 0 };
    private static int AssistWeight(Role position) => position switch { Role.Midfielder => 6, Role.Forward => 4, Role.Defender => 3, _ => 1 };
    private static int CardWeight(Role position) => position switch { Role.Defender or Role.Midfielder => 4, Role.Forward => 2, _ => 1 };
    private static int InjuryWeight(Role position) => position == Role.Goalkeeper ? 1 : 3;

    private sealed class Side(Club club, ImmutableArray<(Player Player, Role Position)> lineup)
    {
        public Club Club { get; } = club;
        public ImmutableArray<(Player Player, Role Position)> Lineup { get; } = lineup;
        // A missing slot counts as nothing, so a short side is weaker.
        public decimal Strength { get; } = lineup.Sum(s => Effective(s.Player, s.Position)) / 11m;
        public HashSet<PersonId> Suspended { get; } = club.Players.Where(p => p.SuspendedMatches > 0).Select(p => p.Id).ToHashSet();
        public Dictionary<PersonId, int> Left { get; } = [];
        public HashSet<PersonId> Booked { get; } = [];
        public Dictionary<PersonId, (int Goals, int Assists)> Contributions { get; } = [];
        public int Goals, Shots, SentOff;
        public (Player Player, Role Position)[] OnPitch => Lineup.Where(s => !Left.ContainsKey(s.Player.Id)).ToArray();
        public void Credit(PersonId player, int goals, int assists)
        {
            var current = Contributions.GetValueOrDefault(player);
            Contributions[player] = (current.Goals + goals, current.Assists + assists);
        }
    }

    // Resolves the match and updates both squads' availability. Receipts and attendance are added by the caller.
    internal static MatchResult Play(World world, Fixture fixture, Club home, Club away)
    {
        var stream = $"match/{fixture.Id.Value}"; var people = stream + "/players";
        Side[] sides = [new(home, Select(home, world.Week)), new(away, Select(away, world.Week))];
        var moments = ImmutableArray.CreateBuilder<Moment>(); var events = ImmutableArray.CreateBuilder<MatchEvent>();
        int Conversion(int s) => Math.Clamp(BaseConversion + (s == 0 ? HomeConversion : 0) + (int)Math.Round((sides[s].Strength - sides[1 - s].Strength) * 6.5m)
            - 25 * sides[s].SentOff + 15 * sides[1 - s].SentOff, 40, 320);
        (Player Player, Role Position)? Pick(IEnumerable<(Player Player, Role Position)> pool, Func<Role, int> weight, Func<Player, Role, int>? scale = null)
        {
            var options = pool.Select(x => (Slot: x, Weight: weight(x.Position) * (scale?.Invoke(x.Player, x.Position) ?? 1))).ToArray();
            if (options.Length == 0) return null;
            var total = options.Sum(o => o.Weight);
            if (total == 0) { options = options.Select(o => (o.Slot, 1)).ToArray(); total = options.Length; }
            var roll = RandomStreams.Next(world, people, total);
            foreach (var option in options) { if (roll < option.Weight) return option.Slot; roll -= option.Weight; }
            throw new InvalidOperationException("Weighted selection overflow.");
        }
        void Goal(int s, int minute, bool extraTime)
        {
            var side = sides[s]; var pool = side.OnPitch;
            if (Pick(pool, ScoreWeight, (p, position) => 40 + Effective(p, position)) is not { } scorer) return;
            side.Goals++; side.Credit(scorer.Player.Id, 1, 0);
            PersonId? assist = null;
            if (RandomStreams.Next(world, people, 100) < 70 && Pick(pool.Where(x => x.Player.Id != scorer.Player.Id), AssistWeight) is { } helper)
            {
                assist = helper.Player.Id; side.Credit(helper.Player.Id, 0, 1);
            }
            moments.Add(new(minute, side.Club.Id, scorer.Player.Id, $"{scorer.Player.Name} scores{(extraTime ? " in extra time" : "")} for {side.Club.Name}.") { AssistId = assist });
        }
        void Incident(int s, int minute, MatchEventKind kind)
        {
            var side = sides[s];
            if (Pick(side.OnPitch, kind == MatchEventKind.Injury ? InjuryWeight : CardWeight) is not { } target) return;
            var id = target.Player.Id;
            if (kind == MatchEventKind.Yellow && side.Booked.Contains(id))
            {
                // A booked player usually stays out of trouble; sometimes a second caution follows.
                if (RandomStreams.Next(world, people, 100) >= 15) return;
                kind = MatchEventKind.SecondYellow;
            }
            var duration = kind switch
            {
                MatchEventKind.Yellow => (target.Player.SeasonYellows + 1) % YellowsPerBan == 0 ? (target.Player.SeasonYellows + 1) / YellowsPerBan : 0,
                MatchEventKind.SecondYellow => SecondYellowBan,
                MatchEventKind.Red => StraightRedBan,
                _ => InjuryWeeks()
            };
            if (kind == MatchEventKind.Yellow) side.Booked.Add(id); else side.Left[id] = minute;
            if (kind is MatchEventKind.SecondYellow or MatchEventKind.Red) side.SentOff++;
            events.Add(new(minute, side.Club.Id, id, kind, duration));
        }
        int InjuryWeeks()
        {
            var roll = RandomStreams.Next(world, people, 100);
            return roll < 40 ? 1 : roll < 65 ? 2 : roll < 78 ? 3 : roll < 86 ? 4
                : roll < 95 ? 5 + RandomStreams.Next(world, people, 4) : 9 + RandomStreams.Next(world, people, 18);
        }
        for (var segment = 0; segment < 6; segment++)
        {
            int Minute() => segment * 15 + 1 + RandomStreams.Next(world, stream, 15);
            var incidents = new List<(int Minute, int Side, MatchEventKind? Kind)>();
            var conversion = new[] { Conversion(0), Conversion(1) };
            for (var s = 0; s < 2; s++)
            {
                var chances = 1 + RandomStreams.Next(world, stream, 3);
                sides[s].Shots += chances;
                for (var chance = 0; chance < chances; chance++)
                    if (RandomStreams.Next(world, stream, 1000) < conversion[s]) incidents.Add((Minute(), s, null));
            }
            for (var s = 0; s < 2; s++)
            {
                // Cautions become more frequent as a match goes on.
                for (var roll = 0; roll < 2; roll++)
                    if (RandomStreams.Next(world, stream, 1000) < YellowRate * (70 + 12 * segment) / 100) incidents.Add((Minute(), s, MatchEventKind.Yellow));
                if (RandomStreams.Next(world, stream, 1000) < RedRate) incidents.Add((Minute(), s, MatchEventKind.Red));
                if (RandomStreams.Next(world, stream, 1000) < InjuryRate) incidents.Add((Minute(), s, MatchEventKind.Injury));
            }
            foreach (var incident in incidents.OrderBy(i => i.Minute))
                if (incident.Kind is { } kind) Incident(incident.Side, incident.Minute, kind); else Goal(incident.Side, incident.Minute, false);
        }
        var extraTime = false; string? shootout = null;
        var winner = sides[0].Goals > sides[1].Goals ? home.Id : sides[1].Goals > sides[0].Goals ? away.Id : default;
        if (fixture.Competition == Competition.Cup && sides[0].Goals == sides[1].Goals)
        {
            extraTime = true;
            var extra = stream + "/extra";
            var scores = new[] { 0, 1 }.Select(s => RandomStreams.Next(world, extra, 100) < Math.Clamp(18 + (int)(sides[s].Strength - sides[1 - s].Strength), 8, 35)).ToArray();
            for (var s = 0; s < 2; s++)
                if (scores[s]) { sides[s].Shots++; Goal(s, 91 + RandomStreams.Next(world, extra, 30), true); }
            if (sides[0].Goals == sides[1].Goals)
            {
                var penalties = stream + "/penalties";
                var homePenalties = 3 + RandomStreams.Next(world, penalties, 3);
                var awayPenalties = 3 + RandomStreams.Next(world, penalties, 3);
                if (homePenalties == awayPenalties) { if (RandomStreams.Next(world, penalties, 2) == 0) homePenalties++; else awayPenalties++; }
                shootout = $"{homePenalties}–{awayPenalties}";
                winner = homePenalties > awayPenalties ? home.Id : away.Id;
            }
            else winner = sides[0].Goals > sides[1].Goals ? home.Id : away.Id;
        }
        var ratings = stream + "/ratings";
        ImmutableArray<Appearance> Rate(int s)
        {
            var side = sides[s]; var against = sides[1 - s].Goals;
            var average = side.Lineup.IsEmpty ? 0 : side.Lineup.Average(x => Effective(x.Player, x.Position));
            return side.Lineup.Select(x =>
            {
                var id = x.Player.Id; var (goals, assists) = side.Contributions.GetValueOrDefault(id);
                var rating = 58 + RandomStreams.Next(world, ratings, 13) + (int)Math.Round((Effective(x.Player, x.Position) - average) / 3)
                    + 3 * Math.Sign(side.Goals - against) + 10 * goals + 5 * assists;
                if (x.Position is Role.Goalkeeper or Role.Defender) rating += against == 0 ? (x.Position == Role.Goalkeeper ? 7 : 5) : -2 * Math.Max(0, against - 1);
                foreach (var e in events.Where(e => e.Player == id && e.ClubId == side.Club.Id))
                    rating -= e.Kind switch { MatchEventKind.Yellow => 3, MatchEventKind.SecondYellow => 20, MatchEventKind.Red => 25, _ => 0 };
                return new Appearance(id, x.Position, Math.Clamp(rating, 50, 100));
            }).ToImmutableArray();
        }
        var homeLineup = Rate(0); var awayLineup = Rate(1);
        var best = homeLineup.Select(a => (Appearance: a, Side: sides[0])).Concat(awayLineup.Select(a => (Appearance: a, Side: sides[1])))
            .OrderByDescending(x => x.Appearance.Rating).ThenByDescending(x => x.Side.Club.Id == winner)
            .ThenByDescending(x => x.Side.Contributions.GetValueOrDefault(x.Appearance.Player).Goals).ThenBy(x => x.Appearance.Player.Value)
            .Select(x => (PersonId?)x.Appearance.Player).FirstOrDefault();
        var recorded = events.ToImmutable();
        foreach (var side in sides) UpdateAvailability(world, side, recorded);
        return new MatchResult(fixture.Id, world.Week, home.Id, away.Id, sides[0].Goals, sides[1].Goals, sides[0].Shots, sides[1].Shots, 0, 0,
            moments.OrderBy(m => m.Minute).ToImmutableArray())
        {
            Winner = winner,
            ExtraTime = extraTime,
            Shootout = shootout,
            HomeLineup = homeLineup,
            AwayLineup = awayLineup,
            Events = recorded,
            PlayerOfMatch = best
        };
    }

    private static void UpdateAvailability(World world, Side side, ImmutableArray<MatchEvent> events)
    {
        var players = side.Club.Players;
        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var own = events.Where(e => e.ClubId == side.Club.Id && e.Player == player.Id).ToArray();
            var served = side.Suspended.Contains(player.Id) ? 1 : 0;
            if (served == 0 && own.Length == 0) continue;
            var injury = own.FirstOrDefault(e => e.Kind == MatchEventKind.Injury);
            players[i] = player with
            {
                SuspendedMatches = player.SuspendedMatches - served + own.Where(e => e.Kind != MatchEventKind.Injury).Sum(e => e.Duration),
                SeasonYellows = player.SeasonYellows + own.Count(e => e.Kind == MatchEventKind.Yellow),
                InjuredUntilWeek = injury is null ? player.InjuredUntilWeek : world.Week + injury.Duration + 1
            };
        }
    }

    // The manager's post-match word to the owner. Derived from the saved result, so it is never stored.
    public static string ManagerLine(MatchResult match, ClubId club, bool cup, IReadOnlyDictionary<PersonId, string> names)
    {
        string Name(PersonId id) => names.TryGetValue(id, out var name) ? name : "One of our players";
        static string Plural(int count, string unit) => $"{count} {unit}{(count == 1 ? "" : unit == "match" ? "es" : "s")}";
        var home = match.Home == club;
        var margin = (home ? match.HomeGoals - match.AwayGoals : match.AwayGoals - match.HomeGoals);
        var won = match.Winner == club || margin > 0;
        var lost = margin < 0 || match.Winner != club && match.Winner != default;
        var lines = new List<string>
        {
            won && match.Shootout is not null ? "We needed penalties, but we are through." :
            won && cup ? "We are through to the next round, and the players earned it." :
            won && margin >= 3 ? "A convincing performance. The players did everything we asked." :
            won ? "Three points and a performance we can build on." :
            lost && cup ? "Our cup run is over. We regroup and focus on the league." :
            lost && margin <= -3 ? "That was not good enough, and I take responsibility for it." :
            lost ? "Disappointing. We will look hard at the goals we conceded." :
            (home ? match.HomeGoals : match.AwayGoals) == 0 ? "A point, but we did not create enough." : "A point. We had spells where we should have done more."
        };
        var lineup = home ? match.HomeLineup : match.AwayLineup;
        if (match.PlayerOfMatch is { } star && lineup.Any(a => a.Player == star)) lines.Add($"{Name(star)} was outstanding.");
        foreach (var e in match.Events.Where(e => e.ClubId == club && e.Duration > 0))
            lines.Add(e.Kind switch
            {
                MatchEventKind.Injury => $"{Name(e.Player)} is injured and out for about {Plural(e.Duration, "week")}.",
                MatchEventKind.Yellow => $"{Name(e.Player)} has reached {YellowsPerBan * e.Duration} bookings and is suspended for the next {Plural(e.Duration, "match")}.",
                _ => $"{Name(e.Player)} was sent off and is suspended for the next {Plural(e.Duration, "match")}."
            });
        return string.Join(" ", lines);
    }
}
