// E08 / E00-A1: read-only, dependency-free analysis of the registered 2 x 2 matrix.
// Usage: node scripts/diagnostics/analyze-owner-mandates.mjs RUN_DIRECTORY OUTPUT_DIRECTORY
// Checks: node scripts/diagnostics/analyze-owner-mandates.mjs --self-test
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const seeds = [2026, 2027, 2028, 2029, 2030];
const eligibleSeeds = [2026, 2030];
const arms = ['RC', 'DC', 'RV', 'DV'];
const id = x => typeof x === 'object' && x !== null ? x.Value : x;
const read = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const integer = n => { assert(Number.isSafeInteger(n), `Unsafe/missing integer: ${n}`); return n; };
const sum = xs => xs.reduce((a, b) => integer(a + integer(b)), 0);
const delta = (a, b) => a === null || b === null ? null : integer(integer(b) - integer(a));
const wages = x => x === 'Wages' || x === 2;
const signed = x => x === 'Signed' || x === 1;
const fixtureKey = f => JSON.stringify([f.Season, f.Competition, f.FixtureId, f.Home, f.Away]);
const tenPercent = (baseline, alternative) => BigInt(integer(alternative)) * 10n <= BigInt(integer(baseline)) * 9n;
const developmentEligible = p => p !== null && p !== undefined && p.AgeBefore <= 23 && p.AbilityBefore < 100;
const moneyFields = ['OpeningCash', 'ClosingCash', 'MinimumCash', 'ReserveTarget', 'ActualWagesPaid', 'ClosingAnnualWages', 'RemainingWageCommitment'];
const contrastFields = ['Points', 'Position', 'ClosingCash', 'MinimumCash', 'ActualWagesPaid', 'ClosingAnnualWages', 'RemainingWageCommitment', 'ClosingArrears', 'ArrearsWeeks', 'ShortageWeeks', 'YoungCohortStarts', 'MatchCount', 'MinimumReserveHeadroom', 'RealizedCohortAbilityGain'];

function journalRows(world) {
  return world.Journal.map(x => Array.isArray(x)
    ? { Sequence: x[0], Week: x[1], Account: x[2], Amount: x[3], Kind: x[4], Reference: x[5] }
    : x);
}

function squad(players) {
  return {
    Count: players.length,
    AgeSum: sum(players.map(p => p.Age)), AbilitySum: sum(players.map(p => p.Ability)),
    Roles: [...new Set(players.map(p => p.Role))].map(role => ({ Role: role, Count: players.filter(p => p.Role === role).length })),
    ContractEndWeeks: [...new Set(players.map(p => p.ContractEndWeek))].sort((a, b) => a - b)
      .map(week => ({ Week: week, Count: players.filter(p => p.ContractEndWeek === week).length }))
  };
}

export function analyzeArm(raw, world) {
  moneyFields.forEach(f => integer(raw[f]));
  const owner = id(world.OwnedClubId);
  const journal = journalRows(world);
  const ownedPayments = journal.filter(x => x.Account === `club/${owner}` && wages(x.Kind) && x.Week > 52 && x.Week <= world.Week);
  const actualWages = -sum(ownedPayments.map(x => x.Amount));
  assert.equal(actualWages, raw.ActualWagesPaid, 'Actual wages must match original journal');
  const remaining = sum(world.Obligations.filter(x => id(x.ClubId) === owner && wages(x.Kind))
    .map(x => integer(Math.max(0, x.EndWeek - Math.max(world.Week + 1, x.StartWeek) + 1) * Math.max(0, -x.WeeklyAmount))));
  assert.equal(remaining, raw.RemainingWageCommitment, 'Remaining wages must match original obligations');
  const closingArrears = sum(world.Arrears.filter(x => id(x.ClubId) === owner).map(x => x.Amount));
  if (raw.ClosingArrears !== undefined) assert.equal(closingArrears, raw.ClosingArrears);
  assert.equal(Math.min(raw.OpeningCash, ...raw.Weeks.map(x => integer(x.Cash))), raw.MinimumCash);
  assert.equal(raw.Weeks.filter(x => x.Arrears > 0).length, raw.ArrearsWeeks);
  assert.equal(raw.Weeks.filter(x => x.Shortages.length > 0).length, raw.ShortageWeeks);
  assert.equal(raw.Fixtures.length, raw.MatchCount);
  assert.equal(sum(raw.Fixtures.map(x => x.CohortStarts)), raw.YoungCohortStarts);
  assert.equal(sum(raw.YoungCohort.map(x => x.Starts)), raw.YoungCohortStarts);
  assert.equal(new Set(raw.Fixtures.map(fixtureKey)).size, raw.Fixtures.length, 'Duplicate fixture key');
  const swaps = raw.Fixtures.flatMap(x => x.Swaps ?? []);
  assert.equal(swaps.length, raw.DevelopmentSwaps);
  for (const f of raw.Fixtures) {
    assert(f.Week > 52 && f.Week <= 104);
    assert((f.Swaps ?? []).length <= 2);
    for (const s of f.Swaps ?? []) {
      assert(s.AbilitySacrifice >= 0 && s.AbilitySacrifice <= 5);
      assert.equal(s.Displaced.Ability - s.Selected.Ability, s.AbilitySacrifice);
      assert(s.Displaced.Age > 23 && s.Selected.Age <= 23);
      assert.equal(s.Displaced.Role, s.Selected.Role);
      assert(s.Selected.Role !== 'Goalkeeper' && s.Selected.Role !== 0);
    }
  }
  const cohort = new Set(raw.YoungCohort.map(x => x.PersonId));
  const approaches = raw.Approaches.map(a => {
    const outcome = a.Outcome;
    const obligationId = id(outcome?.WageObligationId);
    const obligation = outcome && signed(outcome.Outcome)
      ? world.Obligations.find(o => id(o.Id) === obligationId) : null;
    if (outcome && signed(outcome.Outcome)) assert(obligation, 'Signed approach must resolve to original obligation');
    const payments = obligation ? ownedPayments.filter(x => x.Reference.startsWith(`obligation/${obligationId}/`) || x.Reference.startsWith(`arrear/${obligationId}/`)) : [];
    return { ForecastId: a.ForecastId, Recommendation: a.Recommendation, Outcome: outcome, OriginalObligation: obligation,
      SignedOriginalCommitment: obligation ? integer(-obligation.WeeklyAmount * (obligation.EndWeek - obligation.StartWeek + 1)) : 0,
      RemainingCommitment: obligation ? integer(-obligation.WeeklyAmount * Math.max(0, obligation.EndWeek - Math.max(world.Week + 1, obligation.StartWeek) + 1)) : 0,
      ActualWagesPaid: -sum(payments.map(x => x.Amount)), PaymentCount: payments.length,
      PaymentSequences: payments.map(x => x.Sequence) };
  });
  return { ...raw, ClosingArrears: closingArrears,
    MinimumReserveHeadroom: Math.min(delta(raw.ReserveTarget, raw.OpeningCash), ...raw.Weeks.map(x => delta(x.ReserveTarget, x.Cash))),
    RealizedCohortAbilityGain: raw.YoungCohort.every(x => x.Development) ? sum(raw.YoungCohort.map(x => x.Development.AbilityAfter - x.Development.AbilityBefore)) : null,
    CohortStartsPerFixture: { Numerator: raw.YoungCohortStarts, Denominator: raw.MatchCount },
    SwapAbilitySacrifice: sum(swaps.map(x => x.AbilitySacrifice)),
    PositiveCostSwaps: swaps.filter(x => x.AbilitySacrifice > 0).length,
    SwapsSelectingFrozenCohort: swaps.filter(x => cohort.has(id(x.Selected.PersonId))).length,
    OpeningSquadContext: squad(raw.OpeningSquad), ClosingSquadContext: squad(raw.ClosingSquad),
    Approaches: approaches };
}

function pairedFixtures(a, b) {
  const am = new Map(a.Fixtures.map(x => [fixtureKey(x), x]));
  const bm = new Map(b.Fixtures.map(x => [fixtureKey(x), x]));
  const shared = [...am.keys()].filter(k => bm.has(k));
  const onlyA = [...am.keys()].filter(k => !bm.has(k)).map(k => am.get(k));
  const onlyB = [...bm.keys()].filter(k => !am.has(k)).map(k => bm.get(k));
  const sharedDelta = sum(shared.map(k => delta(am.get(k).CohortStarts, bm.get(k).CohortStarts)));
  const nonsharedDelta = delta(sum(onlyA.map(x => x.CohortStarts)), sum(onlyB.map(x => x.CohortStarts)));
  assert.equal(sharedDelta + nonsharedDelta, b.YoungCohortStarts - a.YoungCohortStarts);
  return { SharedCount: shared.length, SharedCohortStartsDelta: sharedDelta, NonsharedCohortStartsDelta: nonsharedDelta,
    OnlyBaseline: onlyA, OnlyTreatment: onlyB,
    Shared: shared.map(k => ({ Key: JSON.parse(k), BaselineStarts: am.get(k).CohortStarts, TreatmentStarts: bm.get(k).CohortStarts })) };
}

export function managerContrast(a, b) {
  assert.equal(a.Seed, b.Seed);
  assert.deepEqual(a.YoungCohort.map(x => x.PersonId).sort((x, y) => x - y), b.YoungCohort.map(x => x.PersonId).sort((x, y) => x - y));
  const players = a.YoungCohort.map(p => {
    const q = b.YoungCohort.find(x => x.PersonId === p.PersonId);
    const ap = p.Development, bp = q.Development;
    const ae = ap ? Math.min(25, ap.Appearances) : null, be = bp ? Math.min(25, bp.Appearances) : null;
    return { PersonId: p.PersonId, Name: p.Name, BaselineStarts: p.Starts, TreatmentStarts: q.Starts,
      BaselineDevelopment: ap, TreatmentDevelopment: bp,
      BaselineEligibleBelowCeiling: developmentEligible(ap), TreatmentEligibleBelowCeiling: developmentEligible(bp),
      BaselineCappedAppearances: ae, TreatmentCappedAppearances: be,
      BaselineTwentyThreshold: ap ? ap.Appearances >= 20 : null, TreatmentTwentyThreshold: bp ? bp.Appearances >= 20 : null,
      ExposureIncreased: ap && bp ? developmentEligible(bp) && (be > ae || bp.Appearances >= 20 && ap.Appearances < 20) : null,
      SaturatedExtraStarts: ap && bp ? Math.max(0, bp.Appearances - ap.Appearances) - Math.max(0, be - ae) : null,
      RealizedGainDelta: ap && bp ? (bp.AbilityAfter - bp.AbilityBefore) - (ap.AbilityAfter - ap.AbilityBefore) : null };
  });
  const complete = a.Complete && b.Complete && players.every(p => p.ExposureIncreased !== null);
  return { Seed: a.Seed, Baseline: a.Arm, Treatment: b.Arm,
    Delta: Object.fromEntries(contrastFields.map(k => [k, delta(a[k], b[k])])), Fixtures: pairedFixtures(a, b), Players: players,
    RelevantExposurePlayers: players.filter(x => x.ExposureIncreased).map(x => x.PersonId),
    Gate: !complete ? 'INCONCLUSIVE' : b.YoungCohortStarts - a.YoungCohortStarts >= 6 && players.some(p => p.ExposureIncreased) ? 'PASS' : 'REVISE' };
}

export function directorContrast(a, b) {
  const ap = a.InitialPreview, bp = b.InitialPreview;
  const ac = a.InitialRecommendation?.Selected, bc = b.InitialRecommendation?.Selected;
  const available = !!(ap?.Recruitment && bp?.Recruitment && ac && bc);
  let quoteGate = false;
  if (available) {
    for (const [p, c] of [[ap, ac], [bp, bc]]) {
      assert.equal(id(p.Recruitment.Player.Id), id(c.PersonId));
      assert.equal(p.Recruitment.WeeklyWage, c.WeeklyWage);
      assert.equal(p.TotalCommitment, c.TotalCommitment);
      assert.equal(p.Recruitment.ContractEndWeek, c.ContractEndWeek);
    }
    quoteGate = ap.BlockingReasons.length === 0 && bp.BlockingReasons.length === 0 && id(ac.PersonId) !== id(bc.PersonId)
      && bc.WeeklyWage <= ac.WeeklyWage && bc.TotalCommitment <= ac.TotalCommitment && ac.Ability - bc.Ability <= 5
      && (tenPercent(ac.WeeklyWage, bc.WeeklyWage) || tenPercent(ac.TotalCommitment, bc.TotalCommitment));
  }
  return { Seed: a.Seed, Baseline: a.Arm, Treatment: b.Arm,
    Delta: Object.fromEntries(contrastFields.map(k => [k, delta(a[k], b[k])])), Fixtures: pairedFixtures(a, b),
    BaselineQuote: ac, TreatmentQuote: bc, BaselineBlocked: ap?.BlockingReasons ?? null, TreatmentBlocked: bp?.BlockingReasons ?? null,
    WeeklySaving: available ? delta(bc.WeeklyWage, ac.WeeklyWage) : null,
    TotalSaving: available ? delta(bc.TotalCommitment, ac.TotalCommitment) : null,
    AbilitySacrifice: available ? ac.Ability - bc.Ability : null,
    WeeklyTenPercent: available ? tenPercent(ac.WeeklyWage, bc.WeeklyWage) : null,
    TotalTenPercent: available ? tenPercent(ac.TotalCommitment, bc.TotalCommitment) : null,
    QuoteGate: available ? quoteGate ? 'PASS' : 'REVISE' : 'INCONCLUSIVE',
    ChangedCandidateSigned: b.Approaches.some(x => x.Recommendation && id(x.Recommendation.Baseline.PersonId) !== id(x.Recommendation.Selected.PersonId) && signed(x.Outcome?.Outcome)) };
}

const dominanceDirections = { Points: 1, ClosingCash: 1, MinimumCash: 1, YoungCohortStarts: 1, RemainingWageCommitment: -1, ArrearsWeeks: -1, ShortageWeeks: -1 };
export function dominance(all, better, worse) {
  const comparisons = eligibleSeeds.map(seed => {
    const a = all.find(x => x.Seed === seed && x.Arm === better), b = all.find(x => x.Seed === seed && x.Arm === worse);
    if (!a?.Complete || !b?.Complete) return { Seed: seed, Complete: false };
    const deltas = Object.fromEntries(Object.keys(dominanceDirections).map(k => [k, delta(b[k], a[k])]));
    return { Seed: seed, Complete: true, Delta: deltas,
      NoWorse: Object.entries(dominanceDirections).every(([k, sign]) => deltas[k] * sign >= 0),
      StrictlyBetter: Object.entries(dominanceDirections).some(([k, sign]) => deltas[k] * sign > 0) };
  });
  return { Better: better, Worse: worse, Seeds: comparisons,
    Status: comparisons.some(x => !x.Complete) ? 'INCONCLUSIVE' : comparisons.every(x => x.NoWorse && x.StrictlyBetter) ? 'WARNING' : 'NO WARNING' };
}

const table = (headers, rows) => [headers, headers.map(() => '---'), ...rows].map(r => `| ${r.map(x => String(x).replaceAll('|', '/')).join(' | ')} |`).join('\n');

function markdown(result) {
  const lines = ['# E08 generated observations', '', 'All money below is integer pence. Deltas are treatment minus baseline. These are exploratory deterministic observations, not population estimates.', '',
    table(['Seed', 'Arm', 'Complete', 'Points/position', 'Cup', 'Closing cash', 'Minimum cash', 'Minimum reserve headroom', 'Wages paid', 'Annual wages', 'Remaining wages', 'Arrears pence/weeks', 'Shortage weeks', 'Starts/fixtures', 'Swaps/cost', 'Ability gain'], result.Arms.map(a => [a.Seed, a.Arm, a.Complete, `${a.Points}/${a.Position}`, a.CupResult, a.ClosingCash, a.MinimumCash, a.MinimumReserveHeadroom, a.ActualWagesPaid, a.ClosingAnnualWages, a.RemainingWageCommitment, `${a.ClosingArrears}/${a.ArrearsWeeks}`, a.ShortageWeeks, `${a.YoungCohortStarts}/${a.MatchCount}`, `${a.DevelopmentSwaps}/${a.SwapAbilitySacrifice}`, a.RealizedCohortAbilityGain])), '',
    '## Manager contrasts', '', table(['Seed', 'Contrast', 'Starts delta', 'Shared fixtures/delta', 'Nonshared delta', 'Relevant exposure IDs', 'Realized gain delta', 'Gate'], result.ManagerContrasts.map(c => [c.Seed, `${c.Treatment}-${c.Baseline}`, c.Delta.YoungCohortStarts, `${c.Fixtures.SharedCount}/${c.Fixtures.SharedCohortStartsDelta}`, c.Fixtures.NonsharedCohortStartsDelta, c.RelevantExposurePlayers.join(', ') || 'none', c.Delta.RealizedCohortAbilityGain, c.Gate])), '',
    '## Director contrasts', '', table(['Seed', 'Contrast', 'Initial weekly saving', 'Initial total saving', 'Ability sacrifice', 'Quote gate', 'Changed candidate signs', 'Closing cash delta', 'Wages paid delta', 'Remaining wages delta'], result.DirectorContrasts.map(c => [c.Seed, `${c.Treatment}-${c.Baseline}`, c.WeeklySaving, c.TotalSaving, c.AbilitySacrifice, c.QuoteGate, c.ChangedCandidateSigned, c.Delta.ClosingCash, c.Delta.ActualWagesPaid, c.Delta.RemainingWageCommitment])), '',
    '## All six arm pairs, both directions', '', table(['Candidate dominant', 'Comparator', '2026 no worse/strict', '2030 no worse/strict', 'Status'], result.Dominance.map(c => [c.Better, c.Worse, ...c.Seeds.map(s => s.Complete ? `${s.NoWorse}/${s.StrictlyBetter}` : 'incomplete'), c.Status])), '',
    '## Frozen cohort exposure by manager contrast', ''];
  for (const c of result.ManagerContrasts) {
    lines.push(`### ${c.Seed}: ${c.Treatment}-${c.Baseline}`, '', table(['ID/name', 'Starts B→T', 'Annual inputs B→T', 'Cap25 B→T', '20 threshold B→T', 'Age B/T', 'Ability before B/T', 'Eligible below100 B/T', 'Relevant exposure', 'Saturated extra', 'Gain delta'], c.Players.map(p => [
      `${p.PersonId} ${p.Name}`, `${p.BaselineStarts}→${p.TreatmentStarts}`, `${p.BaselineDevelopment?.Appearances}→${p.TreatmentDevelopment?.Appearances}`, `${p.BaselineCappedAppearances}→${p.TreatmentCappedAppearances}`, `${p.BaselineTwentyThreshold}→${p.TreatmentTwentyThreshold}`, `${p.BaselineDevelopment?.AgeBefore}/${p.TreatmentDevelopment?.AgeBefore}`, `${p.BaselineDevelopment?.AbilityBefore}/${p.TreatmentDevelopment?.AbilityBefore}`, `${p.BaselineEligibleBelowCeiling}/${p.TreatmentEligibleBelowCeiling}`, p.ExposureIncreased, p.SaturatedExtraStarts, p.RealizedGainDelta])), '');
  }
  lines.push('## Executed approaches', '', table(['Seed/arm', 'Forecast', 'Player', 'Outcome', 'Obligation', 'Start/end', 'Signed total', 'Remaining', 'Paid / entries'], result.Arms.flatMap(a => a.Approaches.map(x => [`${a.Seed}/${a.Arm}`, x.ForecastId, id(x.Recommendation?.Selected.PersonId), x.Outcome?.Outcome ?? 'Pending', id(x.OriginalObligation?.Id) ?? 'none', x.OriginalObligation ? `${x.OriginalObligation.StartWeek}/${x.OriginalObligation.EndWeek}` : 'none', x.SignedOriginalCommitment, x.RemainingCommitment, `${x.ActualWagesPaid}/${x.PaymentCount}`]))), '');
  lines.push('## Later young recruits (excluded from frozen cohort)', '', table(['Seed/arm', 'Recorded recruits'], result.Arms.map(a => [`${a.Seed}/${a.Arm}`, JSON.stringify(a.LaterYoungRecruits)])), '', 'See analysis.json for exact fixture sets, original evidence, squad context, every monetary delta, payment sequence references, and source hashes.', '');
  return lines.join('\n');
}

function selfTest() {
  assert(tenPercent(10000, 9000)); assert(!tenPercent(10000, 9001));
  assert(tenPercent(Number.MAX_SAFE_INTEGER, 8000000000000000));
  assert.throws(() => tenPercent(Number.MAX_SAFE_INTEGER + 1, 0));
  const fixtures = n => Array.from({ length: n }, (_, i) => ({ Season: 2, FixtureId: i, Competition: 'Cup', Home: 1, Away: i + 2, CohortStarts: 2 }));
  const pf = pairedFixtures({ Fixtures: fixtures(2), YoungCohortStarts: 4 }, { Fixtures: fixtures(3), YoungCohortStarts: 6 });
  assert.equal(pf.SharedCohortStartsDelta, 0); assert.equal(pf.NonsharedCohortStartsDelta, 2);
  const rows = eligibleSeeds.flatMap(Seed => ['RC', 'DC'].map(Arm => ({ Seed, Arm, Complete: true, ...Object.fromEntries(Object.keys(dominanceDirections).map(k => [k, 0])) })));
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'NO WARNING');
  rows.find(x => x.Seed === 2026 && x.Arm === 'DC').Points = 1;
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'NO WARNING', 'Strict improvement required at each seed');
  rows.find(x => x.Seed === 2030 && x.Arm === 'DC').Points = 1;
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'WARNING');
  rows.find(x => x.Seed === 2030 && x.Arm === 'DC').ClosingArrears = 999;
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'WARNING', 'Closing arrears pence is contextual, not an extra registered dominance measure');
  rows.find(x => x.Seed === 2030 && x.Arm === 'DC').ShortageWeeks = 1;
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'NO WARNING');
  rows[0].Complete = false;
  assert.equal(dominance(rows, 'DC', 'RC').Status, 'INCONCLUSIVE');
  const makeArm = (Arm, starts, ability = 90) => ({ Seed: 2026, Arm, Complete: true,
    ...Object.fromEntries(contrastFields.map(k => [k, 0])), YoungCohortStarts: starts,
    YoungCohort: [{ PersonId: 1, Name: 'Boundary fixture', Starts: starts,
      Development: { Appearances: starts, AgeBefore: 23, AbilityBefore: ability, AbilityAfter: ability } }],
    Fixtures: [{ Season: 2, FixtureId: 1, Competition: 'League', Home: 1, Away: 2, CohortStarts: starts }] });
  assert.equal(managerContrast(makeArm('RC', 26), makeArm('DC', 33)).Gate, 'REVISE', 'Seven saturated starts do not establish development exposure');
  assert.equal(managerContrast(makeArm('RC', 19), makeArm('DC', 25)).Gate, 'PASS', 'Six starts and useful capped exposure need no realized gain');
  assert.equal(managerContrast(makeArm('RC', 19, 100), makeArm('DC', 25, 100)).Gate, 'REVISE', 'Ability ceiling excludes exposure gate');
  assert.equal(managerContrast({ ...makeArm('RC', 19), Complete: false, Points: null }, makeArm('DC', 25)).Gate, 'INCONCLUSIVE');
  console.log('Analysis boundary checks passed: exact 10%, unsafe integers, fixture decomposition, all-seed strict dominance, countervailing cost, incomplete data.');
}

function main() {
  if (process.argv[2] === '--self-test') return selfTest();
  const [input, output] = process.argv.slice(2);
  assert(input && output, 'Usage: analyze-owner-mandates.mjs RUN_DIRECTORY OUTPUT_DIRECTORY');
  assert(!fs.existsSync(output), 'Output directory must be new; preserve earlier evidence');
  const manifest = read(path.join(input, 'manifest.json'));
  assert.deepEqual(manifest.Seeds.map(x => x.Seed), seeds, 'Do not omit or reorder registered seeds');
  assert.deepEqual(manifest.Seeds.filter(x => x.Eligible).map(x => x.Seed), eligibleSeeds, 'Eligibility frozen by E01B');
  const all = eligibleSeeds.flatMap(seed => arms.map(arm => {
    const dir = path.join(input, String(seed), arm);
    const raw = read(path.join(dir, 'summary.json'));
    assert.equal(raw.Seed, seed); assert.equal(raw.Arm, arm);
    return analyzeArm(raw, read(path.join(dir, 'final-world.json')));
  }));
  const managers = eligibleSeeds.flatMap(seed => [['RC', 'DC'], ['RV', 'DV']].map(([a, b]) => managerContrast(all.find(x => x.Seed === seed && x.Arm === a), all.find(x => x.Seed === seed && x.Arm === b))));
  const directors = eligibleSeeds.flatMap(seed => [['RC', 'RV'], ['DC', 'DV']].map(([a, b]) => directorContrast(all.find(x => x.Seed === seed && x.Arm === a), all.find(x => x.Seed === seed && x.Arm === b))));
  const comparisons = arms.flatMap((a, i) => arms.slice(i + 1).flatMap(b => [dominance(all, a, b), dominance(all, b, a)]));
  const completeMatrix = all.every(x => x.Complete && x.ReplayExact && x.CodecExact && Object.values(x.Guards).every(v => v === true));
  const result = { Protocol: 'E00-A1; fixed E01B checkpoints; season 2 weeks 53–104', Seeds: seeds, EligibleSeeds: eligibleSeeds,
    SourceDirectory: path.resolve(input), Manifest: manifest, Arms: all, ManagerContrasts: managers, DirectorContrasts: directors, Dominance: comparisons,
    Gates: { CompleteValidatedMatrix: completeMatrix,
      DevelopmentExposure: !completeMatrix ? 'INCONCLUSIVE' : managers.some(x => x.Gate === 'PASS') ? 'PASS' : managers.some(x => x.Gate === 'INCONCLUSIVE') ? 'INCONCLUSIVE' : 'REVISE',
      InitialValueQuote: !completeMatrix ? 'INCONCLUSIVE' : directors.some(x => x.QuoteGate === 'PASS') ? 'PASS' : directors.some(x => x.QuoteGate === 'INCONCLUSIVE') ? 'INCONCLUSIVE' : 'REVISE',
      ExecutedFinancialComparison: completeMatrix && directors.some(x => x.ChangedCandidateSigned) ? 'OBSERVED; contextual interpretation required' : 'INCONCLUSIVE',
      DominanceWarnings: comparisons.filter(x => x.Status === 'WARNING').map(x => `${x.Better}>${x.Worse}`),
      TechnicalSafety: 'Requires runner guards plus separately recorded application, fault-publication, solution and Desktop checks',
      Human: 'Awaiting player evidence' } };
  fs.mkdirSync(output, { recursive: true });
  fs.writeFileSync(path.join(output, 'analysis.json'), JSON.stringify(result, null, 2) + '\n');
  fs.writeFileSync(path.join(output, 'tables.md'), markdown(result));
  console.log(JSON.stringify(result.Gates, null, 2));
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) main();
