namespace FootballTycoon.Core;

public sealed record AcquisitionCounteroffer(long Amount, bool Accepted);

public static class Acquisition
{
    public static long Price(World world) => world.AcquisitionOffer is { Accepted: true } offer
        ? offer.Amount : Balance.Load().PurchasePrice;

    public static string Response(AcquisitionCounteroffer offer) => offer.Accepted
        ? $"Seller accepted {Money.Format(offer.Amount)}. Review and confirm the purchase to take control; no money has moved."
        : $"Seller rejected {Money.Format(offer.Amount)}. The original asking price remains available; no money has moved.";

    internal static void Submit(World world, long amount)
    {
        // A dedicated stream keeps bargaining independent of football and every other market.
        // Only commit draws: inspecting or retrying a proposal cannot reroll the seller's response.
        var minimum = Money.Scale(Balance.Load().PurchasePrice,
            (85 + RandomStreams.Next(world, "acquisition/seller", 14)) / 100m);
        world.AcquisitionOffer = new(amount, amount >= minimum);
    }

    internal static void Validate(World world)
    {
        var offers = world.History.Where(h => h.Command.Allocation == Allocation.Counteroffer).ToArray();
        if (world.AcquisitionOffer is not { } offer)
        {
            if (offers.Length != 0) throw new InvalidDataException("Missing acquisition counteroffer response.");
            return;
        }
        if (world.SchemaVersion < 22 || offer.Amount <= 0 || offer.Amount >= Balance.Load().PurchasePrice
            || offer.Amount > world.OpeningOwnerCash || offers.Length != 1 || offers[0].Week != 0
            || offers[0].Command.Amount != offer.Amount || world.History.IndexOf(offers[0]) != 0)
            throw new InvalidDataException("Invalid acquisition counteroffer.");
        var replay = new World { Seed = world.Seed };
        Submit(replay, offer.Amount);
        if (offer != replay.AcquisitionOffer || !world.RandomStates.TryGetValue("acquisition/seller", out var state)
            || state != replay.RandomStates["acquisition/seller"])
            throw new InvalidDataException("Invalid saved seller response.");
        var purchases = world.History.Where(h => h.Command.Allocation == Allocation.Acquire).ToArray();
        var payments = world.Journal.Where(j => j.Kind == CashKind.Purchase).ToArray();
        if (world.Status == CareerStatus.Acquisition)
        {
            if (world.Week != 0 || purchases.Length != 0 || payments.Length != 0 || world.OwnerInvested != 0)
                throw new InvalidDataException("An unconfirmed acquisition cannot transfer ownership or money.");
        }
        else if (purchases.Length != 1 || payments.Length != 2 || payments.Any(j => j.Week != 0)
            || payments.Count(j => j.Account == "owner" && j.Amount == -Price(world)) != 1
            || payments.Count(j => j.Account == "seller" && j.Amount == Price(world)) != 1)
            throw new InvalidDataException("Acquisition payment does not match the agreed price.");
    }
}
