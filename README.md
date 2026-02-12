### EMILtools: 

Architectural system additions for Unity 6.

### Installation

Copy the `EMILtools-Private` folder into your Unity project's `Assets` directory.

### "Signals" Modifier System

A framework for modifying entity stats (Health, Speed, etc.).
- Uses reflection discovery and caching only in Awake() when an IStatUser is initialized.
- struct based Modifiers to be memory-efficient that use the Decorator pattern to create special Modifiers like Timed Modifiers.

#### Key Features
- **Type-Safe Routing**: "Tags" (empty structs like `Speed` or `Health`) are used to identify stats. 
- **SoC of Math and Tags**: Tags are completely seperate from modifiers, meaning you can tag your stats, once and don't have to deal with moving a container around to reference your stat.
- **Zero-Boxing Heterogeneous Storage of Modifiers**: Using a JIT "Double Elision" resolve method, you can have a list of different modifier types (Adders, Multipliers, etc.) without ever hitting the heap.
- **Decorator Support**: You can wrap any modifier in timers, loggers, or custom logic seamlessly without touching the core math.

#### Usage Example

```csharp
public class Enemy : MonoBehaviour, IStatUser 
{
    // The only variable that is implemented via IStatUser
    public Dictionary<Type, IStat> Stats { get; set; }

    public Stat<float, Speed> speed = new(10f);

    void Awake() => this.CacheStats();


    public void Freeze() 
    {
        var halfspeed = new Mathmod(x => x * 0.5f);  // Multiply speed by 0.5 for 3 seconds

        this.Modify<Speed>(halfspeed).WithTimer(3f);
    }
}
```

### Timers System

A centralized ticking engine designed to handle thousands of concurrent timers with minimal GC pressure. It’s the backbone for anything that needs to happen over time.

#### Key Features
- **Global Ticker**: A persistent, hidden `MonoBehaviour` that handles all `Update` and `FixedUpdate` cycles in one place.
- **Leak-Safe**: Uses `ConditionalWeakTable` to prevent memory leaks. If your ITimerUser gets destroyed, the timer system won't keep it alive.
- **Fast Removal**: Using a $O(1)$ removal logic (swap-and-pop) so cleaning up expired timers is practically free.
- **Easy Unsubscribing**: You dont have to call Timer.Remove() on any of your previously added timers. ShutdownTimers() removes all of them for you in one swoop.

#### Usage Example

```csharp
public class Player : MonoBehaviour, ITimerUser 
{
    private CountdownTimer speedBoost = new(5f);

    void Awake() 
    {
        this.InitializeTimers((speedBoost, isFixed: false));

        speedBoost.OnTimerStart.Add(() => Debug.Log("Boost Started));
        speedBoost.OnTimerStop.Add(() => Debug.Log("Boost Over"));
    }

    void Start() => sprintTimer.Start();

    // Unsubscribes ALL subscriptions
    void OnDestroy() => this.ShutdownTimers();
}
```

