### EMILtools: 

Architectural system additions for Unity 6.
1. "Signals" Modifier System
2. Timers System
3. ReactiveIntercept
4. Guards System

### Installation

Copy the `EMILtools-Private` folder into your Unity project's `Assets` directory.

### "Signals" Modifier System

A framework for modifying entity stats (Health, Speed, etc.).
- Uses reflection discovery and caching only in Awake() when an IStatUser is initialized.
- struct based Modifiers to be memory-efficient that use the Decorator pattern to create special Modifiers like Timed Modifiers.

#### Key Features
- **Type-Safe Routing**: "Tags" (empty structs like `Speed` or `Health`) are used to identify stats. These can be re-used on different IStatUsers
- **SoC of Math and Tags**: Tags are completely seperate from modifiers, meaning you can tag your stats, once and don't have to deal with moving a container around to reference your stat.
- **Zero-Boxing Heterogeneous Storage of Modifiers**: Using a JIT "Double Elision" resolve method, you can have a list of different modifier types (Adders, Multipliers, etc.) without ever hitting the heap.
- **Decorator Support**: You can wrap any modifier in timers, loggers, or custom logic seamlessly without touching the core math.

#### Usage Example

Setup your Stat Tags 
```csharp
public struct Speed : IStatTag { }
public struct Healthy : IStatTag { }
```

IStatUser Implementation
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

### Reactive Intercepts (RI)

A variable wrapper that Intercepts changes made to a variable, and allows for Reactions when the variable is changed.

#### Key Features
- **Reactions**: 2 PersistentActions are Lazy Initialized in each ReactiveIntercept both are Invoked when the variable is changed.
- **Intercepts**: Every ReactiveIntercepts holds Lazy Initializes a Func<T,T> that "Intercepts" the setter and mutates its changes. For example you can add Clamps really easy this way

#### Usage Example
```csharp
public class Player : MonoBehaviour 
{
    public ReactiveIntercept<bool> isGrounded;
    public ReactiveIntercept<float> maxSpeed;

    void Awake()
    {
        isGrounded.SimpleReactions.Add(LandAnimation);                     // is a wrapped Action
        isGrounded.Reactions.Add(CheckIfStillInAir);                       // is a wrapped Action<T>
        maxSpeed.Intercepts.Add(x => Mathf.Clamp(x, min: 0, max: 50));     // is a wrapppd List<Func<T,T>>
    }

    void LandAnimation() { ... }
    void CheckIfStillInAir(bool val) { ... }

}
```

### Guarder System

Lightweight composable early exit framework for managing logic flow.
Declaritively block execution and optional branch to other functionality


#### Key Features
- **Declarative Early Exit**: Replace nestied if trees with readable guard chains
- **Inspector Debugging**: See exactly what condition is blocking execution
- **Branching Support**: Block and respond seemlessly
- **Zero hidden allocations after Init**: (Immutable Variants)
- **Separation of Concerns (SoC)**: Separates (1) Condition Evaluation, (2) Execution Flow, (3) Branching Logic

#### Usage Example

**Guard Condition Types**
*1. Guard*:
Simple Condition Wrapper
- Wrapps a Func<bool>
```csharp
new Guard("IsDead", () => health <= 0);
```
*2. Action Guard*:
Condition + branching logic
- Optionally invoke an action when blocking
```csharp
new ActionGuard(
    () => stamina <= 0,
    () => Debug.Log("Too Exhausted"),
    "No Stamina",
    "Show Exhausted Message"
);
```
*3. Lazy Guard*:
Reactive Guard
- Subscribes to an already established PersistentAction
- Only re-evalutes when the observed value changes
- Inteded to subscribe using ReactiveIntercept PersistentActions
```csharp
new LazyGuard(
    "IsGrounded",
    isGrounded.Reactions,
    () => !isGrounded.Value
);
```
*4. Lazy Action Guard*:
Conditon + branching logic
- Subscribe to an already established PersistentAction
- Only re-evaluates when the observed value changes
- Inteded to subscribe using ReactiveIntercept PersistentActions
```csharp
new LazyActionGuard(
    isGrounded.Reactions,
    () => stamina <= 0,
    () => Debug.Log("Too Exhausted"),
    "No Stamina",
    "Show Exhausted Message"
);
```

**Guarder Types**
Guarders group multiple guards and evaluate them linearly
- Inspector Friendly, optionally name/tag conditions and reactions

====================== LAYER 1 - PURE BOOLEAN EVALUATION ======================

*1. GuarderMutable*:
Runtime editable guard list
- Backed by List<T>
- Add guards dynamically
```csharp
var guards = new GuarderMutable(
    ("Dead", () => health <= 0),
    ("Stunned", () => isStunned)
);

if (guards) return;
```

*2. Guarder Immutable*:
Fixed guard array
- Alloc free after construction
- Ideal for initialization only setups
```csharp
var guards = new GuarderImmutable(
    ("Dead", () => health <= 0),
    ("Stunned", () => isStunned)
);

if (guards) return;
```

====================== LAYER 2 - LAZY OBSERVATION BOOLEAN EVALUATION ======================

*1. LazyGuarderMutable*:
Lazy version of GuarderMutable
- Pass in a PersistentAction to subscribe to the re-evaluate when the value changes
```csharp
var guards = new LazyGuarderMutable(
    ("Dead", healthChanged, () => health <= 0),
    ("Stunned", stunChanged, () => isStunned)
);

if (guards) return;
```

*2. Guarder Immutable*:
Lazy version of GuarderImmutable
- Pass in a PersistentAction to subscribe to the re-evaluate when the value changes
```csharp
var guards = new LazyGuarderImmutable(
    ("Dead", healthChanged, () => health <= 0),
    ("Stunned", stunChanged, () => isStunned)
);

if (guards) return;
```

====================== LAYER 2 - BRANCHING & LAZY EVALUATION BOOLEAN EVALUATION ======================


*1. Action Guarder Mutable*:
Branching Guard chain (runtime editable)
- Backed by List<IGuardReaction>
- Supports both ActionGuard and LazyActionGuard
```csharp
var guards = new ActionGuarderMutable(
    new ActionGuard(
        () => health <= 0,
        () => Debug.Log("Dead"),
        "Is Dead",
        "Notify Dead"
    ),
    new ActionGuard(
        () => isStunned,
        () => Debug.Log("Stunned"),
        "Is Stunned",
        "Notify Stunned"
    )
);

if (guards) return;
```

*2. Action Guarder Immutable*:
Branching guard chain (fixed after construction)
- Backed by IGuardReaction[]
- Alloc-free after initialization
```csharp
var guards = new ActionGuarderImmutable(
    new ActionGuard(
        () => health <= 0,
        () => Debug.Log("Dead"),
        "Is Dead",
        "Notify Dead"
    ),
    new ActionGuard(
        () => isStunned,
        () => Debug.Log("Stunned"),
        "Is Stunned",
        "Notify Stunned"
    )
);

if (guards) return;
```

