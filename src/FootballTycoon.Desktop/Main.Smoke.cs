using Godot;
using FootballTycoon.Core;

namespace FootballTycoon.Desktop;

public partial class Main
{
    // Opt-in engine probe. Exercises real UI callbacks and saves only its own new career.
    private async Task SmokeTest()
    {
        try
        {
            await Settled();
            var trainingProbe = OS.GetCmdlineUserArgs().Contains("--training-smoke-test");
            var freeAgentProbe = OS.GetCmdlineUserArgs().Contains("--free-agent-smoke-test");
            var freeAgentChecked = false;
            // Verify the engine applied the weight axes; StringName keys silently used Archivo's 600 default.
            var textServer = TextServerManager.GetPrimaryInterface();
            var weightTag = textServer.NameToTag("wght");
            foreach (var (font, weight) in new[] { (Theme.DefaultFont, 400), (Theme.GetFont("font", "KeyNumber"), 750) })
            {
                var coordinates = textServer.FontGetVariationCoordinates(font.GetRids()[0]);
                if (!coordinates.TryGetValue(weightTag, out var applied) || applied.AsInt32() != weight)
                    throw new InvalidOperationException($"Expected font weight {weight}; the engine did not apply it.");
            }
            var output = OS.GetEnvironment("FT_SMOKE_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = ProjectSettings.GlobalizePath("user://smoke");
            System.IO.Directory.CreateDirectory(output);
            async Task Capture(string name)
            {
                await Settled();
                if (shell.Size.X > GetViewportRect().Size.X + 1 || shell.Size.Y > GetViewportRect().Size.Y)
                    throw new InvalidOperationException($"Shell overflow in {name}: {shell.Size}.");
                if (Descendants(shell).OfType<Label>().Any(label => label.GetThemeFont("font") == monoFont && label.GetLineCount() > 1))
                    throw new InvalidOperationException($"A numeric table cell wrapped in {name}.");
                if (DisplayServer.GetName() != "headless")
                {
                    var result = GetViewport().GetTexture().GetImage().SavePng(System.IO.Path.Combine(output, $"{name}-{textScale}.png"));
                    if (result != Error.Ok) throw new InvalidOperationException("Screenshot failed: " + result);
                }
            }
            async Task Press(string title)
            {
                var button = controls.FirstOrDefault(b => GodotObject.IsInstanceValid(b) && b.IsInsideTree() && !b.IsQueuedForDeletion() && b.Text == title && !b.Disabled)
                    ?? throw new InvalidOperationException("No enabled control: " + title);
                button.EmitSignal(Godot.Button.SignalName.Pressed); await Settled();
            }
            async Task CaptureSection(string heading, string name)
            {
                await Settled();
                if (content.GetParent() is ScrollContainer list)
                {
                    var label = Descendants(content).OfType<Label>().First(l => l.Text == heading);
                    list.ScrollVertical += (int)(label.GlobalPosition.Y - list.GlobalPosition.Y);
                    await Capture(name);
                    list.ScrollVertical = 0;
                }
            }
            foreach (var scale in new[] { 100, 125, 150 })
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Capture("Acquisition");
            }
            textScale = 100; Theme.DefaultFontSize = 14; Render();
            await Press("Review acquisition terms →");
            var before = view.Revision; ContinueCareer(); await Settled();
            if (view.Revision != before) throw new InvalidOperationException("Continue escaped proposal hold.");
            await Press("Review final terms"); await Capture("Acquisition-terms");
            if (view.Status != CareerStatus.Acquisition) throw new InvalidOperationException("Purchase committed before confirmation.");
            await Press("Confirm and commit");
            if (view.Status != CareerStatus.Active || view.PersonalReserve != Balance.Load().OwnerCapital - Balance.Load().PurchasePrice)
                throw new InvalidOperationException("Acquisition callback did not complete.");
            before = view.Revision; ContinueCareer(); await Settled();
            if (view.Revision != before || view.Week != 0) throw new InvalidOperationException("Required opening decision was skipped.");
            foreach (var scale in new[] { 100, 125, 150 })
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Capture("Opening-comparison");
                Preview(Allocation.Hospitality); await Settled(); await Capture("Hospitality-proposal");
                await Press("Review final terms"); await Capture("Hospitality-terms");
                await Press("Keep editing");
                await Press("Back to comparison");
                Preview(Allocation.Training); await Settled(); await Capture("Training-proposal");
                await Press("Review final terms"); await Capture("Training-terms");
                await Press("Keep editing"); await Press("Back to comparison");
            }
            textScale = 100; Theme.DefaultFontSize = 14;
            Preview(Allocation.Recruitment); await Settled(); await Capture("Recruitment-proposal");
            await Press("Back to comparison"); Preview(Allocation.PreserveReserve); await Settled(); await Capture("Retain-cash-proposal");
            await Press("Back to comparison"); Preview(trainingProbe ? Allocation.Training : Allocation.Hospitality); await Settled();
            await Press("Review final terms"); await Press("Confirm and commit");
            if (!view.AllocationChosen || view.ClubCash != Balance.Load().OpeningClubCash - (trainingProbe ? Facilities.TrainingCost(0) : Balance.Load().HospitalityCost))
                throw new InvalidOperationException("Construction was not committed exactly once.");
            ContinueCareer(); await Settled();
            if (view.Week != 4) throw new InvalidOperationException("Continue did not stop at the first monthly review.");
            // The league opens in mid-August (week 7); the second monthly stop has a played match to inspect.
            ContinueCareer(); await Settled();
            if (view.Week != 8) throw new InvalidOperationException("Continue did not stop at the second monthly review.");
            static IEnumerable<Node> Descendants(Node node)
            {
                foreach (var child in node.GetChildren())
                {
                    yield return child;
                    foreach (var descendant in Descendants(child)) yield return descendant;
                }
            }
            var chart = Descendants(shell).OfType<SeasonChart>().Single();
            chart.GrabFocus();
            chart._GuiInput(new InputEventKey { Pressed = true, Keycode = Key.Left });
            await Settled();
            if (!Descendants(shell).OfType<Label>().Any(label => label.IsVisibleInTree() && label.Text.StartsWith($"{Calendar.FullDay(7)} · reserve target")))
                throw new InvalidOperationException("Keyboard chart inspection did not expose exact values.");
            await Capture("Keyboard-chart");
            chart._GuiInput(new InputEventKey { Pressed = true, Keycode = Key.Enter }); await Settled();
            if (inboxSelection != "match" || chosenMatch?.Week != 7) throw new InvalidOperationException("Keyboard chart match selection failed.");
            SelectInbox("brief");
            foreach (var scale in new[] { 100, 125, 150 })
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100;
                foreach (var name in new[] { "Owner Desk", "Club", "Business", "Football", "People", "History" })
                {
                    Navigate(name); await Capture(name.Replace(' ', '-'));
                }
                Navigate("Owner Desk"); await Press("Collapse chart"); await Capture("Collapsed"); await Press("Expand chart");
            }
            textScale = 100; Theme.DefaultFontSize = 14; Navigate("Owner Desk");
            chosenReview = view.Reviews.Last(); SelectInbox(ReviewKey(chosenReview)); await Capture("Monthly-review");
            await Press("File away");
            if (!filedReviews.Contains(ReviewKey(chosenReview))) throw new InvalidOperationException("Review was not filed.");
            await Press("Show filed reviews"); SelectInbox(ReviewKey(chosenReview)); await Press("Restore to inbox");
            if (filedReviews.Count != 0) throw new InvalidOperationException("Review was not restored.");
            chosenHistory = view.History.Last(); SelectInbox("commitment"); await Capture("Commitment");
            chosenMatch = view.Results.Last(); SelectInbox("match"); await Capture("Match-report");
            var reportText = content.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToArray();
            if (!reportText.Contains("LINE-UPS AND RATINGS") || !reportText.Contains("CALLUM PRICE · MANAGER") || !reportText.Any(t => t.StartsWith("Player of the match")))
                throw new InvalidOperationException("Match report is missing line-ups, player of the match or the manager's line.");
            foreach (var scale in new[] { 100, 125, 150 })
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Capture("Match-report");
                if (content.GetParent() is ScrollContainer reportScroll)
                {
                    reportScroll.ScrollVertical = (int)reportScroll.GetVScrollBar().MaxValue; await Capture("Match-report-lower"); reportScroll.ScrollVertical = 0;
                }
            }
            textScale = 100; Theme.DefaultFontSize = 14;
            Navigate("Business"); Preview(Allocation.InjectCapital, 25000000); await Settled();
            await Press("Review final terms"); await Capture("Owner-funding-terms"); await Press("Keep editing"); await Press("Back to comparison");
            ShowSettings(); await Settled(); before = view.Revision; ContinueCareer(); await Settled();
            if (view.Revision != before) throw new InvalidOperationException("Continue escaped settings.");
            await Capture("Settings"); Navigate("Owner Desk");
            chosenReview = view.Reviews.Last(); SelectInbox(ReviewKey(chosenReview)); await Press("File away");
            Save(); await Settled(); ShowSaves(); await Settled(); await Capture("Recovery");
            await Press("Load this checkpoint");
            if (inboxSelection != "resume" || auxiliary != "" || view.Week != 8 || filedReviews.Count != 0 || chosenReview is not null) throw new InvalidOperationException("Checkpoint did not restore context.");
            await Capture("Resumed-career");
            if (OS.GetCmdlineUserArgs().Contains("--market-smoke-test"))
            {
                while (!view.MarketAvailable && view.Week < Seasons.Weeks)
                {
                    await session.AdvanceAsync(AdvanceTarget.Month); view = await session.QueryAsync();
                }
                Navigate("Owner Desk"); await Capture("Midseason-brief");
                await Press("Review midseason recruitment →");
                if (!view.MarketAvailable || view.RecruitmentOptions.Length != 2) throw new InvalidOperationException("Midseason shortlist missing.");
                foreach (var scale in new[] { 100, 150 })
                {
                    textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Capture("Midseason-options");
                    foreach (var allocation in new[] { Allocation.MidseasonRecruitment, Allocation.MidseasonValue, Allocation.MidseasonWait })
                    {
                        Preview(allocation); await Settled(); await Capture("Midseason-" + allocation);
                        await Press("Back to comparison");
                    }
                }
                textScale = 100; Theme.DefaultFontSize = 14; Render();
                await Press("Review lower-cost forward");
                if (selected is not null && selected.BlockingReasons.Any(r => r.Contains("reserve exception", StringComparison.OrdinalIgnoreCase)))
                {
                    await Capture("Midseason-reserve-warning");
                    await Press("Review explicit reserve exception");
                }
                if (selected is not { Recruitment: not null } || !selected.BlockingReasons.IsEmpty)
                    throw new InvalidOperationException("Expected affordable midseason terms: " + string.Join("; ", selected?.BlockingReasons ?? []) + $"; target {selected?.Recruitment?.Player.Name ?? "missing"}");
                var cashBefore = view.ClubCash;
                await Press("Review final terms"); await Capture("Midseason-final-terms"); await Press("Confirm and commit");
                if (view.ClubCash != cashBefore || view.MarketAvailable) throw new InvalidOperationException("Mandate spent cash early or stayed open.");
                Save(); await Settled(); ShowSaves(); await Settled(); await Press("Load this checkpoint");
                if (view.MarketAvailable) throw new InvalidOperationException("Reload reopened the midseason decision.");
                await session.AdvanceAsync(AdvanceTarget.Week); view = await session.QueryAsync();
                Navigate("History"); await Capture("Midseason-outcome");
                if (!view.Reviews.Any(r => r.Week == view.Week && r.ForecastId is not null && (r.Title == "Forward signed" || r.Title == "Recruitment closed without a signing")))
                    throw new InvalidOperationException("Negotiation outcome missing.");
                GD.Print("MARKET SMOKE PASS: shortlist, three options at two scales, confirmed ceiling, pending-save reload and negotiation outcome.");
            }
            if (OS.GetCmdlineUserArgs().Contains("--season-smoke-test"))
            {
                var academyChecked = false;
                for (var season = 1; season <= Seasons.PlayableSeasons; season++)
                {
                    while (view.Week < season * Seasons.Weeks)
                    {
                        var previousWeek = view.Week;
                        await session.AdvanceAsync(AdvanceTarget.Month); view = await session.QueryAsync();
                        if (view.Week == previousWeek) throw new InvalidOperationException("Season smoke stopped before year-end.");
                    }
                    if (string.IsNullOrWhiteSpace(view.CupStatus)) throw new InvalidOperationException("Cup status missing.");
                    Navigate("Football"); await Capture($"Season-{season}-football");
                    Navigate("Owner Desk"); await Capture($"Season-{season}-close");
                    if (season == Seasons.PlayableSeasons) break;
                    ContinueCareer(); await Settled();
                    if (selected?.Renewal?.NextSeason != season + 1) throw new InvalidOperationException("Renewal terms missing.");
                    var nextDivision = selected.Renewal.NextDivision;
                    await Capture($"Season-{season + 1}-renewals");
                    textScale = 150; Theme.DefaultFontSize = 21; Render();
                    await Capture($"Season-{season + 1}-renewals");
                    textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
                    var intake = selected.Renewal.AcademyIntake;
                    if (!intake.IsEmpty)
                    {
                        await Press("Collapse chart");
                        foreach (var scale in new[] { 100, 150 })
                        {
                            textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render();
                            await CaptureSection("ACADEMY · THIS YEAR'S SENIOR INTAKE", "Academy-proposal");
                        }
                        textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
                        await Press("Expand chart");
                        await Press("Decline academy intake");
                        if (selected is not { Command.DeclineAcademy: true, Renewal.AcademyIntake.IsEmpty: true })
                            throw new InvalidOperationException("Declining academy intake did not remove the contracts.");
                    }
                    var expiring = selected.Renewal.Contracts;
                    if (!expiring.IsEmpty)
                    {
                        // Reverse one recommendation, return to the director's plan, then commit with one override.
                        var pick = expiring.FirstOrDefault(c => c.Recommended == ContractAction.Release) ?? expiring.First(c => c.Role == Role.Forward);
                        await Press(ContractButtonText(pick));
                        var flipped = selected?.Renewal?.Contracts.Single(c => c.PlayerId == pick.PlayerId);
                        if (flipped is null || flipped.Chosen == pick.Chosen || selected!.Command.ContractOverrides.Length != 1)
                            throw new InvalidOperationException("Contract override was not requoted.");
                        if (GetViewport().GuiGetFocusOwner() is not Godot.Button focused || focused.Text != ContractButtonText(flipped))
                            throw new InvalidOperationException("Keyboard focus did not return to the changed contract.");
                        await Capture($"Season-{season + 1}-renewal-override");
                        if (content.GetParent() is ScrollContainer renewalScroll)
                        {
                            renewalScroll.ScrollVertical = (int)renewalScroll.GetVScrollBar().MaxValue; await Capture($"Season-{season + 1}-renewal-override-lower"); renewalScroll.ScrollVertical = 0;
                        }
                        textScale = 150; Theme.DefaultFontSize = 21; Render(); await Capture($"Season-{season + 1}-renewal-override");
                        // With the chart collapsed, page through the contract list at both scales.
                        await Press("Collapse chart");
                        foreach (var scale in new[] { 150, 100 })
                        {
                            textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Settled();
                            if (content.GetParent() is not ScrollContainer list) continue;
                            var page = 0;
                            for (var offset = 0; offset <= (int)list.GetVScrollBar().MaxValue; offset += (int)(list.Size.Y * .9f))
                            {
                                list.ScrollVertical = offset; await Capture($"Season-{season + 1}-contracts-{++page}");
                            }
                            list.ScrollVertical = 0;
                        }
                        await Press("Expand chart");
                        await Press("Accept all recommendations");
                        if (!intake.IsEmpty && !selected!.Command.DeclineAcademy)
                            throw new InvalidOperationException("Resetting contract recommendations undid the declined academy intake.");
                        if (selected is not { Renewal: not null } || !selected.Command.ContractOverrides.IsEmpty || selected.Renewal.Contracts.Any(c => c.Chosen != c.Recommended))
                            throw new InvalidOperationException("Accepting all recommendations did not clear the overrides.");
                        await Press(ContractButtonText(pick));
                        if (!selected!.BlockingReasons.IsEmpty) throw new InvalidOperationException("Override blocked: " + string.Join("; ", selected.BlockingReasons));
                    }
                    if (!intake.IsEmpty)
                    {
                        await Press("Reconsider academy intake");
                        if (selected!.Command.DeclineAcademy || !selected.Renewal!.AcademyIntake.SequenceEqual(intake))
                            throw new InvalidOperationException("Reconsidering academy intake rerolled the cohort.");
                        academyChecked = true;
                    }
                    var confirmedIntake = selected!.Renewal!.AcademyIntake;
                    await Press("Review final terms");
                    if (!confirmedIntake.IsEmpty) await CaptureSection("ACADEMY · THIS YEAR'S SENIOR INTAKE", "Academy-final-terms");
                    if (!expiring.IsEmpty) await Capture($"Season-{season + 1}-renewal-final-terms");
                    await Press("Confirm and commit");
                    if (!view.AcademyGraduates.Where(g => g.Week == view.Week).Select(g => g.Player).SequenceEqual(confirmedIntake))
                        throw new InvalidOperationException("Committed academy intake differs from confirmed terms.");
                    if (!expiring.IsEmpty && view.History.Last(h => h.Renewal is not null).Renewal!.Contracts.Count(c => c.Chosen != c.Recommended) != 1)
                        throw new InvalidOperationException("Renewal decision was not recorded against the recommendation.");
                    if (view.Season != season + 1 || view.Division != nextDivision || view.AllocationChosen || !view.Results.IsEmpty)
                        throw new InvalidOperationException("Rollover did not reset the season view.");
                    await Capture($"Season-{season + 1}-capital-plan");
                    Preview(Allocation.PreserveReserve); await Settled();
                    await Press("Review final terms"); await Press("Confirm and commit");
                    if (freeAgentProbe && !freeAgentChecked && view.FreeAgentRecommendation is not null)
                    {
                        Navigate("People"); await Press("Collapse chart");
                        await CaptureSection("Director's free-agent recommendation", "Free-agent-recommendation");
                        await Press("Review free-agent approach");
                        if (selected!.BlockingReasons.Any(r => r.Contains("reserve exception", StringComparison.OrdinalIgnoreCase)))
                            await Press("Review explicit reserve exception");
                        if (selected is not { Recruitment.IsFreeAgent: true } || !selected.BlockingReasons.IsEmpty)
                            throw new InvalidOperationException("Free-agent terms unavailable: " + string.Join("; ", selected?.BlockingReasons ?? []));
                        var terms = selected.Recruitment;
                        foreach (var scale in new[] { 100, 150 })
                        {
                            textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Capture("Free-agent-proposal");
                            await CaptureSection("DIRECTOR’S TARGET", "Free-agent-contract");
                        }
                        textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
                        var cashBefore = view.ClubCash;
                        await Press("Review final terms"); await Capture("Free-agent-final-terms"); await Press("Confirm and commit");
                        if (view.ClubCash != cashBefore || !view.FreeAgentApproachPending)
                            throw new InvalidOperationException("Free-agent approval spent cash or lost its pending mandate.");
                        Save(); await Settled(); ShowSaves(); await Settled(); await Press("Load this checkpoint");
                        if (!view.FreeAgentApproachPending) throw new InvalidOperationException("Reload lost the free-agent mandate.");
                        Navigate("People"); await CaptureSection("Director's free-agent recommendation", "Free-agent-pending");
                        await session.AdvanceAsync(AdvanceTarget.Week); view = await session.QueryAsync();
                        if (view.FreeAgentApproachPending || !view.Reviews.Any(r => r.Week == view.Week && (r.Title == "Free agent signed" || r.Title == "Free-agent approach closed")))
                            throw new InvalidOperationException("Free-agent negotiation did not report its outcome.");
                        if (view.Squad.SingleOrDefault(p => p.Id == terms.Player.Id) is { } signed
                            && (signed.WeeklyWage != terms.WeeklyWage || signed.ContractEndWeek != terms.ContractEndWeek))
                            throw new InvalidOperationException("Signed free-agent terms differ from the approved mandate.");
                        Navigate("History"); await Capture("Free-agent-outcome");
                        await Press("Expand chart");
                        freeAgentChecked = true;
                        GD.Print("FREE AGENT SMOKE PASS: recommendation, two text scales, full terms, no payment on approval, pending-save reload and negotiation outcome.");
                    }
                }
                if (freeAgentProbe && !freeAgentChecked) throw new InvalidOperationException("No free-agent approach was exercised.");
                Navigate("History"); await Capture("Three-season-history");
                if (view.Status != CareerStatus.PrototypeComplete || view.SeasonSummaries.Length != 3)
                    throw new InvalidOperationException("Three-season milestone did not finish.");
                if (view.SeasonSummaries.Any(s => s.Development.IsEmpty)
                    || view.SeasonSummaries.Last().Development.Any(p => (view.Squad.SingleOrDefault(s => s.Id == p.PlayerId)?.Age
                        ?? view.RetiredFormerPlayers.SingleOrDefault(r => r.PlayerId == p.PlayerId)?.Age) != p.AgeBefore + 1))
                    throw new InvalidOperationException("Season development did not reach the final report and squad.");
                Navigate("People"); await Capture("Three-season-development");
                if (OS.GetCmdlineUserArgs().Contains("--rival-smoke-test"))
                {
                    if (view.RivalApproaches.IsEmpty || view.RivalApproaches.Any(a => a.Outcome == ApproachOutcome.Pending))
                        throw new InvalidOperationException("Rival recruitment history missing or unresolved at the endpoint.");
                    await Press("Collapse chart");
                    foreach (var scale in new[] { 100, 150 })
                    {
                        textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render();
                        await CaptureSection("Recent rival recruitment", "Rival-recruitment");
                    }
                    textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
                    await Press("Expand chart");
                    GD.Print("RIVAL SMOKE PASS: retained approaches and resolved outcomes visible at both text scales.");
                }
                if (OS.GetCmdlineUserArgs().Contains("--academy-smoke-test"))
                {
                    if (!academyChecked || view.AcademyGraduates.IsEmpty) throw new InvalidOperationException("The academy choice path was not exercised.");
                    foreach (var scale in new[] { 100, 150 })
                    {
                        textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render();
                        await CaptureSection("Academy graduates", "Academy-graduates");
                    }
                    textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
                    GD.Print("ACADEMY SMOKE PASS: decline, contract reset preserves choice, reconsider, final terms and retained graduate history.");
                }
                if (view.Departures.Any(d => d.ClubId != view.ClubId || view.PlayerNames.GetValueOrDefault(d.PlayerId) != d.Name))
                    throw new InvalidOperationException("Departure records lost their club or historical name.");
                if (view.Departures.Length > 0 && content.GetParent() is ScrollContainer peopleList)
                {
                    peopleList.ScrollVertical = (int)peopleList.GetVScrollBar().MaxValue;
                    await Capture("Player-departures");
                }
                Navigate("History"); await Settled();
                await Press("Collapse chart");
                foreach (var scale in new[] { 100, 150 })
                {
                    textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Settled();
                    await Press("Show season 3 player reviews");
                    if (content.GetParent() is ScrollContainer developmentList)
                    {
                        developmentList.ScrollVertical = 500 * scale / 100;
                        await Capture("Annual-development-reviews");
                        developmentList.ScrollVertical = 0;
                    }
                    await Press("Hide season 3 player reviews");
                }
                GD.Print("DEVELOPMENT SMOKE PASS: three annual reports and aged final squad.");
                if (trainingProbe)
                {
                    if (view.TrainingLevel != 1 || view.HospitalityLevel != 0 || !view.SeasonSummaries.First().Development.Any(p => p.TrainingBonus > 0)
                        || !view.Reviews.Any(r => r.Title == "Training center opens"))
                        throw new InvalidOperationException("Training delivery or development evidence is missing.");
                    Navigate("Club"); await Capture("Training-facilities");
                    GD.Print("TRAINING SMOKE PASS: proposal, confirmed cost, saved project, delivery, upkeep and seasonal development evidence.");
                }
                GD.Print("SEASON SMOKE PASS: renewal UI, contract recommendations with an override and accept-all, annual plans, three season reports and final endpoint.");
            }
            if (OS.GetCmdlineUserArgs().Contains("--retirement-smoke-test"))
            {
                // Explicit controlled age fixture; normal seed careers do not reach the cutoff in three years.
                var fixture = WorldFactory.Create(2026);
                void CommitFixture(Allocation allocation)
                {
                    var proposal = Proposals.Preview(fixture, new OwnerCommand(allocation) { DeclineAcademy = allocation == Allocation.StartNextSeason }, fixture.Revision);
                    Proposals.Commit(fixture, Guid.NewGuid().ToString("N"), fixture.Revision, proposal);
                }
                CommitFixture(Allocation.Acquire); CommitFixture(Allocation.PreserveReserve);
                var keepers = fixture.OwnedClub.Players.Where(p => p.Role == Role.Goalkeeper).ToArray();
                var forward = fixture.OwnedClub.Players.First(p => p.Role == Role.Forward);
                foreach (var player in keepers.Append(forward))
                {
                    var end = player.Role == Role.Goalkeeper ? 52 : 156;
                    fixture.OwnedClub.Players[fixture.OwnedClub.Players.IndexOf(player)] = player with { Age = player.Role == Role.Goalkeeper ? 41 : 37, ContractEndWeek = end };
                    fixture.Obligations = fixture.Obligations.Select(o => o.Description == $"Player contract {player.ContractId.Value}" ? o with { EndWeek = end } : o).ToList();
                }
                async Task LoadFixture()
                {
                    var vault = new FootballTycoon.Infrastructure.LocalSaveVault(saveDirectory);
                    var checkpoint = vault.Write(WorldCodec.Encode(fixture), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "retirement-probe");
                    await session.LoadAsync(checkpoint.SnapshotId); view = await session.QueryAsync();
                    selected = null; confirming = false; chartExpanded = false;
                    Navigate("People"); await Settled();
                }
                while (fixture.Week < 51) Simulation.AdvanceWeek(fixture);
                await LoadFixture();
                if (view.PendingRetirements.Length != 2) throw new InvalidOperationException("Expected two announced goalkeeper retirements.");
                foreach (var scale in new[] { 100, 150 })
                {
                    textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render();
                    await CaptureSection("Retirement plans", "Retirement-announcements");
                }
                await session.AdvanceAsync(AdvanceTarget.Week); view = await session.QueryAsync();
                Preview(Allocation.StartNextSeason, declineAcademy: true); await Settled();
                if (selected is not { Renewal.Retired: 2 } || !selected.BlockingReasons.IsEmpty
                    || !selected.Renewal.Shortages.Any(s => s.StartsWith("Goalkeeper: 0")))
                    throw new InvalidOperationException("Fixed retirement terms must disclose the shortage and permit renewal.");
                foreach (var scale in new[] { 100, 150 })
                {
                    textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render();
                    await CaptureSection("JONAS REED · SPORTING DIRECTOR · EXPIRING CONTRACTS", "Retirement-contracts");
                }
                await Press("Review final terms"); await CaptureSection("JONAS REED · SPORTING DIRECTOR · EXPIRING CONTRACTS", "Retirement-final-terms");
                await Press("Confirm and commit");
                if (view.RetiredFormerPlayers.Length != 2 || view.Squad.Any(p => p.Role == Role.Goalkeeper))
                    throw new InvalidOperationException("Renewal did not apply the mandatory departures.");
                Preview(Allocation.PreserveReserve); await Settled();
                await Press("Review final terms"); await Press("Confirm and commit");
                var saved = await session.SaveAsync("retirement-result"); await session.LoadAsync(saved.SnapshotId);
                await session.AdvanceAsync(AdvanceTarget.Week); view = await session.QueryAsync();
                if (view.Week != 53 || view.RetiredFormerPlayers.Length != 2)
                    throw new InvalidOperationException("Saved retirements or shortage advance failed.");
                fixture = WorldCodec.Decode(await session.ExportCheckpointAsync());
                while (fixture.Week < 155)
                {
                    if (fixture.Status == CareerStatus.SeasonReview)
                    { CommitFixture(Allocation.StartNextSeason); CommitFixture(Allocation.PreserveReserve); }
                    var previousWeek = fixture.Week;
                    Simulation.AdvanceWeek(fixture);
                    if (fixture.Week == previousWeek) throw new InvalidOperationException("Retirement fixture stopped before its final season: " + fixture.Status);
                }
                await LoadFixture(); await session.AdvanceAsync(AdvanceTarget.Week); view = await session.QueryAsync();
                if (view.Status != CareerStatus.PrototypeComplete || !view.RetiredFormerPlayers.Any(p => p.PlayerId == forward.Id && p.Age == 40)
                    || !view.SeasonSummaries.Last().Development.Any(p => p.PlayerId == forward.Id))
                    throw new InvalidOperationException("Final-season retirement lost the named development history.");
                textScale = 100; Theme.DefaultFontSize = 14; Navigate("People");
                await CaptureSection("Player departures", "Retirement-archive");
                GD.Print("RETIREMENT SMOKE PASS: controlled announcements, fixed renewal, shortage advance, saved retirements and final-season history.");
            }
            GD.Print($"SMOKE PASS: acquisition, all plan previews, final confirmations, required-decision guards, next review, six workspaces at three text scales, chart collapse, review filing/restoration, funding terms, settings, save/load and resume; {DisplayServer.GetName()}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError("SMOKE FAILED: " + error); GetTree().Quit(1); }
    }
    private async Task Settled()
    {
        var frames = 0;
        while (busy)
        {
            if (++frames > 3600) throw new TimeoutException("Session operation did not finish.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        for (var frame = 0; frame < 8; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
