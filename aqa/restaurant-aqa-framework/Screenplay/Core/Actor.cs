using System.Collections.Concurrent;

namespace RestaurantAqaFramework.Screenplay.Core;

public interface IAbility : IAsyncDisposable
{
}

public interface ITask
{
    Task PerformAsAsync(Actor actor);
}

public interface IQuestion<TAnswer>
{
    Task<TAnswer> AnsweredByAsync(Actor actor);
}

/// <summary>
/// Screenplay Pattern Actor representing a user role (Owner, Guest, Waiter, OtherCustomer, Anonymous).
/// Holds abilities (BrowseTheWeb, CallRestApi) and scenario memory.
/// </summary>
public sealed class Actor(string name) : IAsyncDisposable
{
    private readonly Dictionary<Type, IAbility> _abilities = new();
    private readonly ConcurrentDictionary<string, object?> _memory = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; } = name;

    public static Actor Named(string name) => new(name);

    public Actor WhoCan(params IAbility[] abilities)
    {
        foreach (var ability in abilities)
        {
            _abilities[ability.GetType()] = ability;
        }
        return this;
    }

    public TAbility Using<TAbility>() where TAbility : class, IAbility
    {
        if (_abilities.TryGetValue(typeof(TAbility), out var exact))
            return (TAbility)exact;

        var assignable = _abilities.Values.OfType<TAbility>().FirstOrDefault();
        return assignable ?? throw new InvalidOperationException(
            $"Actor '{Name}' does not have the ability '{typeof(TAbility).Name}'.");
    }

    public bool HasAbility<TAbility>() where TAbility : class, IAbility =>
        _abilities.ContainsKey(typeof(TAbility)) || _abilities.Values.OfType<TAbility>().Any();

    public async Task AttemptsToAsync(params ITask[] tasks)
    {
        foreach (var task in tasks)
        {
            await task.PerformAsAsync(this);
        }
    }

    public Task<TAnswer> AsksForAsync<TAnswer>(IQuestion<TAnswer> question) =>
        question.AnsweredByAsync(this);

    public void Remember<T>(string key, T value) => _memory[key] = value;

    public T Recall<T>(string key)
    {
        if (_memory.TryGetValue(key, out var val) && val is T typed)
            return typed;

        throw new KeyNotFoundException($"Actor '{Name}' does not recall key '{key}' of type '{typeof(T).Name}'.");
    }

    public T? RecallOrDefault<T>(string key, T? defaultValue = default) =>
        _memory.TryGetValue(key, out var val) && val is T typed ? typed : defaultValue;

    public async ValueTask DisposeAsync()
    {
        foreach (var ability in _abilities.Values)
        {
            await ability.DisposeAsync();
        }
        _abilities.Clear();
    }
}

