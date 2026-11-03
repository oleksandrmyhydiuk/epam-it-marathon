using RestaurantAqaFramework.Config;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Core;
using RestaurantAqaFramework.Screenplay.Tasks;

namespace RestaurantAqaFramework.Tests;

/// <summary>
/// Base class for all End-to-End (UI + API) test suites.
/// Manages Screenplay Actors, dynamic test data lifecycle, artifact capture, and guaranteed environment cleanup.
/// </summary>
public abstract class TestBase : IAsyncLifetime
{
    private readonly List<Actor> _actors = [];
    private readonly List<(Actor Owner, string ReservationId)> _reservationsToCleanUp = [];

    protected Actor Owner { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Owner = CreateApiActor("Owner");
        await Owner.AttemptsToAsync(
            new AuthenticateCustomerViaApiTask(
                TestSettings.Current.OwnerEmail,
                TestSettings.Current.OwnerPassword,
                firstName: "OwnerAndrii",
                lastName: "Marathon"));
    }

    protected Actor CreateApiActor(string name)
    {
        var actor = Actor.Named(name).WhoCan(CallRestApi.At());
        _actors.Add(actor);
        return actor;
    }

    protected async Task<Actor> EnableWebBrowsingAsync(Actor actor)
    {
        if (!actor.HasAbility<BrowseTheWeb>())
        {
            var web = await BrowseTheWeb.OpenNewBrowserSessionAsync();
            actor.WhoCan(web);
        }
        return actor;
    }

    protected bool IsFrontendConfigured()
    {
        var url = TestSettings.Current.FrontendBaseUrl;
        return !string.IsNullOrWhiteSpace(url)
               && !url.Contains("dxxxxxxxxxxxx.cloudfront.net", StringComparison.OrdinalIgnoreCase);
    }

    protected async Task<(string ReservationId, string GroupReservationId, string InviteToken)> CreateTrackedGroupReservationAsync(
        Actor? ownerActor = null,
        int guestsCount = 4,
        bool createAssociatedOrder = true)
    {
        var targetOwner = ownerActor ?? Owner;
        await targetOwner.AttemptsToAsync(new CreateGroupReservationFixtureTask(guestsCount, createAssociatedOrder));

        var reservationId = targetOwner.Recall<string>(MemoryKeys.ReservationId);
        var groupReservationId = targetOwner.Recall<string>(MemoryKeys.GroupReservationId);
        var inviteToken = targetOwner.Recall<string>(MemoryKeys.InviteToken);

        _reservationsToCleanUp.Add((targetOwner, reservationId));
        return (reservationId, groupReservationId, inviteToken);
    }

    protected async Task<Actor> CreateAuthenticatedRoleActorAsync(string roleName)
    {
        var actor = CreateApiActor(roleName);

        switch (roleName)
        {
            case "OtherCustomer":
                await actor.AttemptsToAsync(
                    new AuthenticateCustomerViaApiTask(
                        TestSettings.Current.OtherCustomerEmail,
                        TestSettings.Current.OtherCustomerPassword,
                        firstName: "OtherClient",
                        lastName: "User"));
                break;

            case "Waiter":
                if (!string.IsNullOrWhiteSpace(TestSettings.Current.WaiterEmail) &&
                    !TestSettings.Current.WaiterEmail.Equals("waiter@example.com", StringComparison.OrdinalIgnoreCase))
                {
                    await actor.AttemptsToAsync(
                        new AuthenticateCustomerViaApiTask(
                            TestSettings.Current.WaiterEmail,
                            TestSettings.Current.WaiterPassword));
                }
                else
                {
                    // Fallback to a non-owner authenticated account when dedicated waiter credentials are not set in .env
                    await actor.AttemptsToAsync(
                        new AuthenticateCustomerViaApiTask(firstName: "WaiterRole", lastName: "User"));
                }
                break;

            case "GuestSession":
                actor.Using<CallRestApi>().WithGuestSessionToken(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
                break;

            case "Unauthenticated":
                // Remains without Bearer or Guest tokens
                break;
        }

        return actor;
    }

    public virtual async Task DisposeAsync()
    {
        foreach (var (owner, reservationId) in _reservationsToCleanUp)
        {
            await owner.AttemptsToAsync(new CleanupReservationTask(reservationId));
        }

        foreach (var actor in _actors)
        {
            await actor.DisposeAsync();
        }
    }
}

