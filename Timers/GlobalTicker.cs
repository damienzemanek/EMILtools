using UnityEngine;

namespace EMILtools.Timers
{
    public class GlobalTicker : MonoBehaviour
    {
        void Update() => TimerUtility.TickAllUpdates(Time.deltaTime);
        void FixedUpdate() => TimerUtility.TickAllFixed(Time.fixedDeltaTime);
    }
}
