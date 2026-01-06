using EMILtools.Core;
using NUnit.Framework;
using System;
using System.Diagnostics;
using static EMILtools.Core.Stabilizer;

public class StablizableCriticalTests
{
    class TestUser : IStablizableUser
    {
        // Three forms of wrappers. Stabilizable is the best of both worlds
        // ----------------------------------------------------------------------------------------------------
        // 1: Struct -> Stack performance, Value-type semantics, Value-type passing (boxing)
        // 2: Stabilizable -> Stack performance, Value-type semantics, Reference-type passing
        // 3: Reference -> Heap performance, Reference-type semantics, Refernce Type passing
        public ValueType valueTypeHealth = new ValueType(5);
        public Stablizable<ValueType> nonStabilizedHealth = new Stablizable<ValueType>(new ValueType(5));
        [Stabilize] public Stablizable<ValueType> stabilizedHealth = new Stablizable<ValueType>(new ValueType(5));

        public ReferenceType referencehealth = new(5);
    }
    
    
    // Reference type passing, with value-type read/writing
    
    // -------------------------
    // 1️⃣ Hot-loop read comparison
    // -------------------------
    

    public class ReferenceType
    {
        public int someValue;
        public ReferenceType(int initial) => someValue = initial;
    }
    
    public struct ValueType
    {
        public int someValue;
        public ValueType(int initial) => someValue = initial;
    }

    
[Test]
public void HotLoop_SingleValue_Read_StackVsPseudoBoxVsRefClass()
{
    const int iterations = 50_000_000;

    // ==================================== SETUP =====================================
    var testUser = new TestUser();
    var sw = new System.Diagnostics.Stopwatch();

    // --------------------------------- VALUETYPE ----------------------------------
    int valueTypeSum = 0;
    long valueTypeTime = 0;

    // ------------------------ STABILIZABLE - NOT STABILIZED  ----------------------
    int nonStabilizedSum = 0;
    long nonStabilizedTime = 0;

    // ------------------------ STABILIZABLE - STABILIZED ---------------------------
    int stabilizedSum = 0;
    long stabilizedTime = 0;
    testUser.stabilizedHealth.Stabilize(testUser);

    // ---------------------------------REFERENCE ---------------------------------
    int referenceSum = 0;
    long referenceTime = 0;

    // ==================================== EXECUTE =====================================

    // --------------------------------- VALUETYPE ----------------------------------
    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        valueTypeSum += testUser.valueTypeHealth.someValue;
    }
    sw.Stop();
    valueTypeTime = sw.ElapsedMilliseconds;

    // ------------------------ STABILIZABLE - NOT STABILIZED  ----------------------
    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        nonStabilizedSum += testUser.nonStabilizedHealth.Get.someValue;
    }
    sw.Stop();
    nonStabilizedTime = sw.ElapsedMilliseconds;

    // ------------------------ STABILIZABLE - STABILIZED ---------------------------
    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        stabilizedSum += testUser.stabilizedHealth.Get.someValue;
    }
    sw.Stop();
    stabilizedTime = sw.ElapsedMilliseconds;

    // ---------------------------------REFERENCE ---------------------------------
    sw.Restart();
    for (int i = 0; i < iterations; i++)
    {
        referenceSum += testUser.referencehealth.someValue;
    }
    sw.Stop();
    referenceTime = sw.ElapsedMilliseconds;

    // ================================ DEBUG OUTPUT ==================================

    UnityEngine.Debug.Log(
        $"Hot-loop Single Value Read ({iterations} iterations):\n" +
        $"Label   Time      Sum         #NotStableAvoid  #PullCalled  #HeapAccessed  #StabilizeCalled  #IsStableQueried\n" +
        $"===============================================================================================================\n" +
        $"Value-Type:.......... {valueTypeTime}ms {valueTypeSum,10} {"N/A",15} {"N/A",12} {"N/A",13} {"N/A",15} {"N/A",17}\n" +
        $"Non-Stabilized:.... {nonStabilizedTime}ms {nonStabilizedSum,10} \n" +
        $"Stabilized:............. {stabilizedTime}ms {stabilizedSum,10}\n" +
        $"Ref<T> class:....... {referenceTime}ms {referenceSum,10} {"N/A",15} {"N/A",12} {"N/A",13} {"N/A",15} {"N/A",17}"
    );
    
}

    
    [Test]
    public void HotLoop_SingleValue_WRITE_StackVsPseudoBoxVsRefClass()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = Stopwatch.StartNew(); sw.Stop();
        
        // --------------------------------- VALUETYPE ----------------------------------
        int valueTypeSum = 0;
        long valueTypeTime = 0;
        
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        int nonStabilizedleSum = 0;
        long nonStabilizedleTime = 0;

        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        int stabilizedSum = 0;
        long stabilizedleTime = 0;
        testUser.stabilizedHealth.Stabilize(testUser);

        //  ---------------------------------REFERENCE ---------------------------------
        int referenceSum = 0;
        long referenceTime = 0;
        
        
        // ==================================== EXECUTE =====================================

        
        // --------------------------------- VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.valueTypeHealth.someValue += 1;
        }
        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Required: value-type semantics during writing
            var newWrapper = new ValueType(testUser.nonStabilizedHealth.Get.someValue);
            newWrapper.someValue += 1;
            testUser.nonStabilizedHealth.stack = newWrapper;
        }
        sw.Stop();
        nonStabilizedleTime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Referece Type semantics when stabilized during writing
            testUser.stabilizedHealth.heap.someValue += 1;
        }
        sw.Stop();
        stabilizedleTime = sw.ElapsedMilliseconds;
        //  ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.referencehealth.someValue += 1;
        }
        sw.Stop();
        referenceTime = sw.ElapsedMilliseconds;


        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations} iterations):\n" +
            $"{"Label"} {"Time",6} {"Sum",12}\n" +
            $"===================================\n" +
            $"{"Value-Type:"}.......... {valueTypeTime}ms.......sum: {valueTypeSum,10}\n" +
            $"{"Non-Stabilized:"}.... {nonStabilizedleTime}ms.......sum: {nonStabilizedleSum,10}\n" +
            $"{"Stabilized:"}............. {stabilizedleTime}ms.......sum: {stabilizedSum,10}\n" +
            $"{"Ref<T> class:"}....... {referenceTime}ms.......sum: {referenceSum,10}"
        );





    // ==================================== ASSERTIONS =====================================
    // Compare sums (should be exact)
        Assert.AreEqual(valueTypeSum, nonStabilizedleSum);
        Assert.AreEqual(nonStabilizedleSum, stabilizedSum);
        Assert.AreEqual(stabilizedSum, referenceSum);

    // Compare times with ±2ms tolerance
        Assert.IsTrue(Math.Abs(valueTypeTime - nonStabilizedleTime) <= 2, 
            $"ValueTypeTime {valueTypeTime}ms is not within ±2ms of NonStabilizedTime {nonStabilizedleTime}ms");

        Assert.IsTrue(Math.Abs(nonStabilizedleTime - stabilizedleTime) <= 2, 
            $"NonStabilizedTime {nonStabilizedleTime}ms is not within ±2ms of StabilizedTime {stabilizedleTime}ms");

        Assert.IsTrue(Math.Abs(stabilizedleTime - referenceTime) <= 2, 
            $"StabilizedTime {stabilizedleTime}ms is not within ±2ms of ReferenceTime {referenceTime}ms");
        // ======================================================================================
    }
    
    // /// <summary>
    // /// Box and return the box
    // /// </summary>
    // /// <param name="stabilizable"></param>
    // /// <returns></returns>
    // public int UseStackLocal(int val)
    // {
    //     val += 1;
    //     return val;
    // }
    //
    // /// <summary>
    // /// Box and return the box
    // /// </summary>
    // /// <param name="stabilizable"></param>
    // /// <returns></returns>
    // public int UseStabilized(int val)
    // {
    //     val += 1;
    //     return val;
    // }
    //
    //
    //
    // /// <summary>
    // /// Pointer de-ref, no return
    // /// </summary>
    // /// <param name="reference"></param>
    // public void UseRef(RefClass reference)
    // {
    //     reference.val += 1;
    // }
    //
    // /// <summary>
    // /// Passing:
    // /// Stack local operations when passing are slow due to boxing, using implicit conversion, this is fastest
    // /// Stabilized operations are fast due to intended box
    // /// </summary>
    // [Test]
    // public void HotLoop_SingleValue_PASSING_StackVsPseudoBoxVsRefClass()
    // {
    //     const int iterations = 5_000_000;
    //
    //     // Stack-local struct
    //     var stackStruct = new Stablizable<int>();
    //     stackStruct.Value = 42;
    //     int sum1 = 0;
    //     
    //     //TIMER
    //     var sw = Stopwatch.StartNew();
    //     for (int i = 0; i < iterations; i++)
    //     {
    //         sum1 += UseStackLocal(stackStruct);
    //     }
    //     sw.Stop();
    //     
    //     
    //     long stackTime = sw.ElapsedMilliseconds;
    //
    //     // Stabilized Stablizable (pseudo-box) - capture into local to avoid repeated field-copy
    //     var user = new TestUser();
    //     user.health.Value = 42;
    //     user.StabilizeAttributed();
    //     var stabLocal = user.health; // hoist once
    //     int sum2 = 0;
    //
    //     //TIMER
    //     sw.Restart();
    //     for (int i = 0; i < iterations; i++)
    //     {
    //         sum2 = UseValue(stabLocal.Value);
    //     }
    //     sw.Stop();
    //     
    //     
    //     long stabTime = sw.ElapsedMilliseconds;
    //
    //     // Classic Ref<T>
    //     var refObj = new RefClass(42);
    //     int sum3 = 0;
    //     
    //     //TIMER
    //     sw.Restart();
    //     for (int i = 0; i < iterations; i++)
    //         UseRef(refObj);
    //     sw.Stop();
    //     
    //     
    //     
    //     long refTime = sw.ElapsedMilliseconds;
    //     
    //     // Sanity checks
    //     Assert.AreEqual(sum1, sum2);
    //     Assert.AreEqual(sum2, sum3);
    //
    //     UnityEngine.Debug.Log(
    //         $"Hot-loop Single Value Read ({iterations} iterations):\n" +
    //         $"Stack-local: {stackTime:F3}ms\n" +      // 3 decimal places
    //         $"Stabilized: {stabTime:F3}ms\n" +
    //         $"Ref<T> class: {refTime:F3}ms"
    //     );
    //     
    //
    //     // Realistic performance assertions
    //     Assert.IsTrue(stackTime > stabTime, "When passing stack-local, it boxes, stabailized doesnt");
    //     Assert.IsTrue(stabTime <= refTime * 1.1, "Stabilized reads competitive with standard Ref<T> class");
    // }
    //
    // [Test]
    // public void MassUpdate_MultiCopies_StackVsPseudoBoxVsRefClass()
    // {
    //     const int count = 500_000;
    //     const int copies = 10;
    //
    //     // Stack-local struct copies
    //     var stackStruct = new Stablizable<int>();
    //     stackStruct.Value = 0;
    //     var stackCopies = new Stablizable<int>[copies];
    //     for (int i = 0; i < copies; i++)
    //         stackCopies[i] = stackStruct;
    //
    //     var sw = Stopwatch.StartNew();
    //     for (int i = 0; i < count; i++)
    //         for (int j = 0; j < copies; j++)
    //             stackCopies[j].Value += 1; // independent increments
    //     sw.Stop();
    //     long stackTime = sw.ElapsedMilliseconds;
    //
    //     // Stabilized pseudo-box (all share reference) - hoist the stabilized struct once, fill copies from it
    //     var user = new TestUser();
    //     user.health.Value = 0;
    //     user.StabilizeAttributed();
    //     var stabLocal = user.health; // hoisted stabilized struct
    //     var stabCopies = new Stablizable<int>[copies];
    //     for (int i = 0; i < copies; i++)
    //         stabCopies[i] = stabLocal; // all share the same underlying Ref<T>
    //
    //     sw.Restart();
    //     for (int i = 0; i < count; i++)
    //         for (int j = 0; j < copies; j++)
    //             stabCopies[j].Value += 1; // reference update on local-backed copies
    //     sw.Stop();
    //     long stabTime = sw.ElapsedMilliseconds;
    //
    //     // RefClass<T>
    //     var refObj = new RefClass<int>(0);
    //     var refCopies = new RefClass<int>[copies];
    //     for (int i = 0; i < copies; i++)
    //         refCopies[i] = refObj; // all share reference
    //
    //     sw.Restart();
    //     for (int i = 0; i < count; i++)
    //         for (int j = 0; j < copies; j++)
    //             refCopies[j].val += 1;
    //     sw.Stop();
    //     long refTime = sw.ElapsedMilliseconds;
    //
    //     UnityEngine.Debug.Log(
    //         $"Mass Update ({count} updates x {copies} copies):\n" +
    //         $"Stack-local: {stackTime}ms\n" +
    //         $"Pseudo-box: {stabTime}ms\n" +
    //         $"Ref<T> class: {refTime}ms"
    //     );
    //
    //     // Validate results
    //     int expectedStackSum = count; // independent struct copies
    //     for (int i = 0; i < copies; i++)
    //         Assert.AreEqual(expectedStackSum, stackCopies[i].Value);
    //
    //     int expectedRefSum = count * copies; // shared reference
    //     Assert.AreEqual(expectedRefSum, stabCopies[0].Value);
    //     Assert.AreEqual(expectedRefSum, refCopies[0].val);
    //
    //     // Realistic performance assertions
    //     Assert.IsTrue(stabTime < stackTime, "Pseudo-box updates faster than multiple independent stack-local copies");
    //     Assert.IsTrue(stabTime <= refTime * 1.2, "Pseudo-box comparable or slightly faster than standard Ref<T>");
    // }
}
