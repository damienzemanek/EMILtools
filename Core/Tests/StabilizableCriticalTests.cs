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
        public NestedValueType nestedValueTypeHealth = new NestedValueType(5);
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
    
    public struct NestedValueType
    {
        public ValueType inner;
        public NestedValueType(int initial) => inner = new(initial);
    }

    
    [Test]
    public void HotLoop_SingleValue_READ()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = new System.Diagnostics.Stopwatch();
        testUser.stabilizedHealth.Stabilize(testUser);

        // --------------------------------- VALUETYPE ----------------------------------
        int valueTypeSum = 0;
        long valueTypeTime = 0;
        
        // -------------------------------Nested VALUETYPE ----------------------------------
        int nestedVTsum = 0;
        long nestedVTtime = 0;


        // ------------------ STABILIZABLE - NOT STABILIZED - Get ----------------------
        int nonStabilizedSumGet = 0;
        long nonStabilizedTimeGet = 0;
        
        // ------------------ STABILIZABLE - NOT STABILIZED - GetStack ----------------------
        int nonStabilizedSumGetStack = 0;
        long nonStabilizedTimeGetStack = 0;

        // ------------------------ STABILIZABLE - STABILIZED - Get ---------------------------
        int stabilizedSumGet = 0;
        long stabilizedTimeGet = 0;
        
        // ------------------ STABILIZABLE - STABILIZED - GetStable ----------------------
        int stabilizedSumGetStable = 0;
        long stabilizedTimeGetSable = 0;

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
        
        // ------------------------------ Nested VALUETYPE --------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            nestedVTsum += testUser.nestedValueTypeHealth.inner.someValue;
        }
        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - NOT STABILIZED - Get ----------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            nonStabilizedSumGet += testUser.nonStabilizedHealth.Get.someValue;
        }
        sw.Stop();
        nonStabilizedTimeGet = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - NOT STABILIZED - GetStack  ----------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            nonStabilizedSumGetStack += testUser.nonStabilizedHealth.GetStack.someValue;
        }
        sw.Stop();
        nonStabilizedTimeGetStack = sw.ElapsedMilliseconds;

        // ------------------------ STABILIZABLE - STABILIZED - Get ---------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            stabilizedSumGet += testUser.stabilizedHealth.Get.someValue;
        }
        sw.Stop();
        stabilizedTimeGet = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - STABILIZED - GetStable ---------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            stabilizedSumGetStable += testUser.stabilizedHealth.GetStable.someValue;
        }
        sw.Stop();
        stabilizedTimeGetSable = sw.ElapsedMilliseconds;

        // ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            referenceSum += testUser.referencehealth.someValue;
        }
        sw.Stop();
        referenceTime = sw.ElapsedMilliseconds;
        
        
        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations} iterations):\n" +
            $"Label   Time      Sum" +
            $"===============================================================================================================\n" +
            $"VT: Value-Type:................................... {valueTypeTime}ms \n" +
            $"VT: Nested Value-Type:...................... {nestedVTtime}ms  \n" +
            $"VT: Non-Stabilized - Get:.................... {nonStabilizedTimeGet}ms \n" +
            $"VT: Non-Stabilized - GetStack:........... {nonStabilizedTimeGetStack}ms  \n" +
            $"RT: Stabilized - Get :............................ {stabilizedTimeGet}ms \n" +
            $"RT: Stabilized - GetStable:.................. {stabilizedTimeGetSable}ms \n" +
            $"RT: Ref<T> class:................................. {referenceTime}ms "
        );
        
        
        // ================================ ASSERTIONS ==================================
        Assert.IsTrue((valueTypeSum == nestedVTsum) &&
                      (nestedVTsum == nonStabilizedSumGet) &&
                      (nonStabilizedSumGet == nonStabilizedSumGetStack) &&
                      (nonStabilizedSumGetStack == stabilizedSumGet) &&
                      (stabilizedSumGet == stabilizedSumGetStable) &&
                      (stabilizedSumGetStable == referenceSum) &&
                      (referenceSum == nestedVTsum));
        
        //  ValueType and NestedValueType within 40ms
        Assert.IsTrue(Math.Abs(valueTypeTime - nestedVTtime) <= 40,
            $"ValueTypeTime {valueTypeTime}ms is not within ±40ms of NestedValueTypeTime {nestedVTtime}ms");

        // Non-Stabilized Get and Stabilized Get are within 300ms of Ref<T>
        Assert.IsTrue(Math.Abs(nonStabilizedTimeGet - referenceTime) <= 300,
            $"NonStabilized Get time {nonStabilizedTimeGet}ms is not within ±300ms of RefClassTime {referenceTime}ms");

        Assert.IsTrue(Math.Abs(stabilizedTimeGet - referenceTime) <= 300,
            $"Stabilized Get time {stabilizedTimeGet}ms is not within ±300ms of RefClassTime {referenceTime}ms");

        // Non-Stabilized GetStack is less than or within 5ms of ValueType
        Assert.IsTrue(nonStabilizedTimeGetStack <= valueTypeTime + 5,
            $"NonStabilized GetStack time {nonStabilizedTimeGetStack}ms should be <= ValueTypeTime {valueTypeTime} + 5ms");

        // Stabilized GetStable is less than or within 5ms of Ref<T>
        Assert.IsTrue(stabilizedTimeGetSable <= referenceTime + 5,
            $"Stabilized GetStable time {stabilizedTimeGetSable}ms should be <= RefClassTime {referenceTime} + 5ms");
    }

    
    [Test]
    public void HotLoop_SingleValue_WRITE_StackVsPseudoBoxVsRefClass()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = Stopwatch.StartNew(); sw.Stop();
        testUser.stabilizedHealth.Stabilize(testUser);

        
        // --------------------------------- VALUETYPE ----------------------------------
        long valueTypeTime = 0;

        // ------------------------- Nested VALUETYPE  ----------------------------------
        long nestedVTtime = 0;
        
        // ------------------------ STABILIZABLE - NOT STABALIZED - Get  -------------------
        long nonStabilizedleGetTime = 0;

        // ------------------------ STABILIZABLE - NOT STABALIZED - GetStack  ---------------
        long nonStabilizedleGetStackTime = 0;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        long stabilizedleTime = 0;

        //  ---------------------------------REFERENCE ---------------------------------
        long referenceTime = 0;
        
        
        // ==================================== EXECUTE =====================================

        
        // --------------------------------- VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Direct field assignment
            testUser.valueTypeHealth.someValue += 1;
        }
        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;
        
        // ----------------------------- Nested VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Compiler still does read-modify-write (Slow)
            testUser.nestedValueTypeHealth.inner.someValue += 1;
        }
        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - NOT STABALIZED - Get  ----------------------
        // NOT RECOMENDED
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Required: Read-modify-write
            // This is how you would normally write to a value type outside its context
            var newWrapper = new ValueType(testUser.nonStabilizedHealth.Get.someValue);
            newWrapper.someValue += 1;
            testUser.nonStabilizedHealth.stack = newWrapper;
        }
        sw.Stop();
        nonStabilizedleGetTime = sw.ElapsedMilliseconds;
        
                
        // ------------------------ STABILIZABLE - NOT STABALIZED - GetStack  ----------------------
        // NOT RECOMMENDED
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Required: Read-modify-write
            // This is how you would normally write to a value type outside its context
            var newWrapper = new ValueType(testUser.nonStabilizedHealth.GetStack.someValue);
            newWrapper.someValue += 1;
            testUser.nonStabilizedHealth.stack = newWrapper;
        }
        sw.Stop();
        nonStabilizedleGetStackTime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Reference Type semantics when stabilized during writing
            // With the heap reference, it's as fast as value type and reference type
            // No read-modify-write
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
            $"Label   Time      Sum" +
            $"===============================================================================================================\n" +
            $"VT: Value-Type:................................... {valueTypeTime}ms \n" +
            $"VT: Nested Value-Type:...................... {nestedVTtime}ms  \n" +
            $"VT: Non-Stabilized - Get:.................... {nonStabilizedleGetTime}ms \n" +
            $"VT: Non-Stabilized - GetStack:.......... {nonStabilizedleGetStackTime}ms  \n" +
            $"RT: Stabilized :..................................... {stabilizedleTime}ms \n" +
            $"RT: Ref<T> class:................................. {referenceTime}ms "
        );




        // ==================================== ASSERTIONS =====================================

        // Assert that value-type, nested value type, stabilized, and Ref<T> class are all within 15ms of each other
        Assert.LessOrEqual(Math.Abs(valueTypeTime - nestedVTtime), 15, "Value-Type vs Nested Value-Type timing difference too high");
        Assert.LessOrEqual(Math.Abs(valueTypeTime - stabilizedleTime), 15, "Value-Type vs Stabilized timing difference too high");
        Assert.LessOrEqual(Math.Abs(valueTypeTime - referenceTime), 15, "Value-Type vs Ref<T> timing difference too high");
        Assert.LessOrEqual(Math.Abs(nestedVTtime - stabilizedleTime), 15, "Nested Value-Type vs Stabilized timing difference too high");
        Assert.LessOrEqual(Math.Abs(nestedVTtime - referenceTime), 15, "Nested Value-Type vs Ref<T> timing difference too high");
        Assert.LessOrEqual(Math.Abs(stabilizedleTime - referenceTime), 15, "Stabilized vs Ref<T> timing difference too high");

        // Assert that Non-Stabilized GetStack is faster than Non-Stabilized Get
        Assert.Less(nonStabilizedleGetStackTime, nonStabilizedleGetTime, "Non-Stabilized GetStack should be faster than Non-Stabilized Get");

        // Assert that Stabilized is faster than both Non-Stabilized Get and Non-Stabilized GetStack
        Assert.Less(stabilizedleTime, nonStabilizedleGetTime, "Stabilized should be faster than Non-Stabilized Get");
        Assert.Less(stabilizedleTime, nonStabilizedleGetStackTime, "Stabilized should be faster than Non-Stabilized GetStack");
        // ======================================================================================
    }
    
    
    public void PassInValueType(ValueType vt) { }
    public void PassInNestedValueType(NestedValueType vt) { }
    public void PassInNonStable(Stablizable<ValueType> ns) { }
    public void PassInStableSpecificParam(IRefBox s) { }
    public void PassInStableImplicitConversion(ValueType s) { }
    public void PassInReference(ReferenceType rt) { }
    
    
    [Test]
    public void HotLoop_SingleValue_PASSINGinMETHOD()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = Stopwatch.StartNew(); sw.Stop();
        testUser.stabilizedHealth.Stabilize(testUser);
        
        // --------------------------------- VALUETYPE ----------------------------------
        long valueTypeTime = 0;
        // ---------------------------- Nested VALUETYPE ----------------------------------
        long nestedVTtime = 0;
        
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        long nonStabilizedleTime = 0;

        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        long refBoxStabilizedTime = 0;
        long implicitconversionStabilizedTime = 0;

        //  ---------------------------------REFERENCE ---------------------------------
        long referenceTime = 0;
        
        
        // ==================================== EXECUTE =====================================

        
        // --------------------------------- VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassInValueType(testUser.valueTypeHealth);
        }
        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;
        // --------------------------------- VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassInNestedValueType(testUser.nestedValueTypeHealth);
        }
        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        // NOT RECCOMENDED
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassInNonStable(testUser.nonStabilizedHealth);
        }
        sw.Stop();
        nonStabilizedleTime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        //                        RefBox<ValueType> Method Param 
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Advantage: Psuedo-boxes
            PassInStableSpecificParam(testUser.stabilizedHealth.param);
        }
        sw.Stop();
        refBoxStabilizedTime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        //          ValueType Method Param (Implicit conversion - Slowest)

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Not reccomended
            PassInStableImplicitConversion(testUser.stabilizedHealth.param);
        }
        sw.Stop();
        implicitconversionStabilizedTime = sw.ElapsedMilliseconds;
        
        //  ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            PassInReference(testUser.referencehealth);
        }
        sw.Stop();
        referenceTime = sw.ElapsedMilliseconds;


        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations} iterations):\n" +
            $"{"Label"} {"Time",6} {"Sum",12}\n" +
            $"===================================\n" +
            $"{"VT: Value-Type:"}........................................................ {valueTypeTime}ms\n" +
            $"{"VT: Nested Value-Type:"}........................................ {nestedVTtime}ms\n" +
            $"{"VT: Non-Stabilized:"}................................................. {nonStabilizedleTime}ms\n" +
            $"{"VT:Stabilized Implicit Conversin param:"}..............{implicitconversionStabilizedTime}ms\n" +
            $"{"RT: Stabilized (RefBox<ValueType> param:"}........ {refBoxStabilizedTime}ms\n" +
            $"{"RT: Ref<T> class:"}.............................................. {referenceTime}ms"
        );
        


        // ==================================== ASSERTIONS =====================================
// Value-Type vs Nested Value-Type within 10ms
        Assert.LessOrEqual(Math.Abs(valueTypeTime - nestedVTtime), 10, "Value-Type and Nested Value-Type timing difference too high");

// Ref<T> class vs Stabilized (RefBox<ValueType>) may vary due to JIT
        Assert.LessOrEqual(Math.Abs(referenceTime - refBoxStabilizedTime), Math.Max(referenceTime, refBoxStabilizedTime) + 5, 
            "Ref<T> class and Stabilized (RefBox<ValueType>) timing difference unexpectedly high");

// Stabilized Implicit Conversion vs Value-Type within 10ms
        Assert.LessOrEqual(Math.Abs(implicitconversionStabilizedTime - valueTypeTime), 10, "Stabilized Implicit Conversion and Value-Type timing difference too high");

// Non-Stabilized should remain the slowest
        Assert.Greater(nonStabilizedleTime, valueTypeTime, "Non-Stabilized should be slower than Value-Type");
        Assert.Greater(nonStabilizedleTime, nestedVTtime, "Non-Stabilized should be slower than Nested Value-Type");
        Assert.Greater(nonStabilizedleTime, refBoxStabilizedTime, "Non-Stabilized should be slower than Stabilized RefBox");
        Assert.Greater(nonStabilizedleTime, implicitconversionStabilizedTime, "Non-Stabilized should be slower than Stabilized Implicit Conversion");
        Assert.Greater(nonStabilizedleTime, referenceTime, "Non-Stabilized should be slower than Ref<T> class");

// Stabilized RefBox and Ref<T> class are generally faster, but may occasionally be slightly slower than Nested Value-Type (esp when nesteds are SMALL)
        Assert.LessOrEqual(refBoxStabilizedTime, nonStabilizedleTime, "Stabilized RefBox should be faster than Non-Stabilized");
        Assert.LessOrEqual(referenceTime, nonStabilizedleTime, "Ref<T> class should be faster than Non-Stabilized");
    }

    // Limitation: Pass by value
    public void PassAndUseInValueType(ValueType vt)
    {
        // compiler copy-read-write
        vt.someValue += 1;
    }
    // Limitation: Pass by value
    public void PassAndUseInNestedValueType(NestedValueType vt)
    {
        // compiler copy-read-write
        vt.inner.someValue += 1;
    }
    // Not reccomended
    public void PassAndUseInNonStable(Stablizable<ValueType> ns)
    {
        // copy-read-write
        var temp = ns.GetStack;
        temp.someValue += 1;
        ns.stack = temp;
    }

    // Advantage: Pass by Reference
    public void PassAndUseInStableRefBox(IRefBox s)
    {
        ((RefBox<ValueType>)s).unbox.someValue += 1;
    }
    // Advantage: Pass by Reference
    public void PassAndUseInStableSpecificRefBox(RefBox<ValueType> s)
    {
        s.unbox.someValue += 1;
    }
    
    public void PassAndUseInStableImplicitConversion(ValueType s)
    {
        s.someValue += 1;
    }

    public void PassAndUseInReference(ReferenceType rt)
    {
        rt.someValue += 1;
    }
    
    
    [Test]
    public void HotLoop_SingleValue_PASSandUSEinMETHOD()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = Stopwatch.StartNew(); sw.Stop();
        testUser.stabilizedHealth.Stabilize(testUser);
        
        // --------------------------------- VALUETYPE ----------------------------------
        long valueTypeTime = 0;
        // --------------------------------- VALUETYPE ----------------------------------
        long nestedVTtime = 0;
        
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        long nonStabilizedleTime = 0;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        long refBoxIRefBoxStableTime = 0;
        long refBoxSpecificStableTime = 0;
        long implicitConverstionStableTime = 0;

        //  ---------------------------------REFERENCE ---------------------------------
        long referenceTime = 0;
        
        
        // ==================================== EXECUTE =====================================

        
        // --------------------------------- VALUETYPE ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassAndUseInValueType(testUser.valueTypeHealth);
        }
        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;
        // ------------------------------ Nested VALUETYPE --------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassAndUseInNestedValueType(testUser.nestedValueTypeHealth);
        }
        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - NOT STABALIZED  ----------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Limtation: Pass by value
            PassAndUseInNonStable(testUser.nonStabilizedHealth);
        }
        sw.Stop();
        nonStabilizedleTime = sw.ElapsedMilliseconds;
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        //                         IRefBox Method Param 
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Advantage: Psuedo-boxes
            PassAndUseInStableRefBox(testUser.stabilizedHealth.param);
        }
        sw.Stop();
        refBoxIRefBoxStableTime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        //                        RefBox<ValueType> Method Param 
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Advantage: Psuedo-boxes
            PassAndUseInStableSpecificRefBox(testUser.stabilizedHealth.param);
        }
        sw.Stop();
        refBoxSpecificStableTime = sw.ElapsedMilliseconds;
        
        // ------------------------ STABILIZABLE - STABILIZED ---------------------------
        //   ValueType Method Param (Implicit conversion ~ About as fast as Value-type Boxing)

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Not Reccomended, although works
            // Use GetStable for already stable Valuetype passes. That will avoid 
            PassAndUseInStableImplicitConversion(testUser.stabilizedHealth.param);
        }
        sw.Stop();
        implicitConverstionStableTime = sw.ElapsedMilliseconds;
        
        //  ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            PassAndUseInReference(testUser.referencehealth);
        }
        sw.Stop();
        referenceTime = sw.ElapsedMilliseconds;


        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations} iterations):\n" +
            $"{"Label"} {"Time",6} {"Sum",12}\n" +
            $"===================================\n" +
            $"{"VT: Stabilized Implicit Conversin param:"}.......... {implicitConverstionStableTime}ms\n" +
            $"{"VT: Value-Type:"}................................................ {valueTypeTime}ms\n" +
            $"{"VT: Nested Value-Type:"}................................... {nestedVTtime}ms\n" +
            $"{"VT: Non-Stabilized:"}.......................................... {nonStabilizedleTime}ms\n" +
            $"{"RT: Stabilized IRefBox param:"}......................... {refBoxIRefBoxStableTime}ms\n" +
            $"{"RT: Stabilized Specific RefBox param:"}........... {refBoxSpecificStableTime}ms\n" +
            $"{"RT: Ref<T> class:"}............................................ {referenceTime}ms"
        );


        // ==================================== ASSERTIONS =====================================

        // All value-types should be within 20ms of each other
        Assert.LessOrEqual(Math.Abs(valueTypeTime - nestedVTtime), 20, "Value-Type and Nested Value-Type timing difference too high");
        Assert.LessOrEqual(Math.Abs(valueTypeTime - implicitConverstionStableTime), 20, "Value-Type and Stabilized Implicit Conversion timing difference too high");
        Assert.LessOrEqual(Math.Abs(nestedVTtime - implicitConverstionStableTime), 20, "Nested Value-Type and Stabilized Implicit Conversion timing difference too high");

        // Non-Stabilized should be much slower than all value-types
        Assert.Greater(nonStabilizedleTime, valueTypeTime * 5, "Non-Stabilized should be significantly slower than Value-Type");
        Assert.Greater(nonStabilizedleTime, nestedVTtime * 5, "Non-Stabilized should be significantly slower than Nested Value-Type");
        Assert.Greater(nonStabilizedleTime, implicitConverstionStableTime * 5, "Non-Stabilized should be significantly slower than Stabilized Implicit Conversion");

        // All reference-type passes should be within 20ms of each other
        long maxRefTime = Math.Max(Math.Max(refBoxIRefBoxStableTime, refBoxSpecificStableTime), referenceTime);
        long minRefTime = Math.Min(Math.Min(refBoxIRefBoxStableTime, refBoxSpecificStableTime), referenceTime);
        Assert.LessOrEqual(maxRefTime - minRefTime, 20, "All reference-type passes should be within 20ms of each other");

        // Reference-types should generally be slower than value-types (except non-stabilized) 
        Assert.Greater(refBoxIRefBoxStableTime, valueTypeTime, "Stabilized IRefBox param should be slower than Value-Type");
        Assert.Greater(refBoxSpecificStableTime, valueTypeTime, "Stabilized Specific RefBox param should be slower than Value-Type");
        Assert.Greater(referenceTime, valueTypeTime, "Ref<T> class should be slower than Value-Type");

        Assert.Less(nonStabilizedleTime, refBoxIRefBoxStableTime * 3, "Non-Stabilized should still be slower than most RefBox passes (sanity check)");
        // ========================================================================================

    }
}
