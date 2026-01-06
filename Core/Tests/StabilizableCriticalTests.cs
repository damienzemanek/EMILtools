using EMILtools.Core;
using NUnit.Framework;
using System;
using System.Diagnostics;
using static EMILtools.Core.AutoBoxer;

public class StablizableCriticalTests
{
    class TestUser : IBoxUser
    {
        // Three forms of wrappers. Stabilizable is the best of both worlds
        // ----------------------------------------------------------------------------------------------------
        // 1: Struct -> Stack performance, Value-type semantics, Value-type passing (boxing)
        // 2: Stabilizable -> Stack performance, Value-type semantics, Reference-type passing
        // 3: Reference -> Heap performance, Reference-type semantics, Refernce Type passing
        public ValueType valueType = new ValueType(5);
        public NestedValueType nestedValueType = new NestedValueType(5);

        public OptionalRef<ValueType> nestedOR_vt = new OptionalRef<ValueType>(new ValueType(5));
        [AutoBox] public OptionalRef<ValueType> nestedOR_ref = new OptionalRef<ValueType>(new ValueType(5));

        public OptionalRef<int> OR_valuetype = new(5);
        [AutoBox] public OptionalRef<int> OR_reference = new(5);

        public ReferenceType reference = new(5);
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
        testUser.OR_reference.Box();
        testUser.nestedOR_ref.Box();

        // ------------------------------ ValueType ----------------------------------
        int valueTypeSum = 0;
        long valueTypeTime = 0;

        // ------------------------------- ValueType (Nested) ------------------------
        int nestedVTsum = 0;
        long nestedVTtime = 0;

        // ------------------------------ OptionalRef - Value (Nested) --------------
        int OR_NestedValueSum = 0;
        long OR_NestedValueTime = 0;

        // ------------------------------ OptionalRef - stack (Nested) --------------
        int OR_NestedstackSum = 0;
        long OR_NestedstackTime = 0;

        // ------------------------------ OptionalRef - heap  (Nested)--------------
        int OR_NestedheapSum = 0;
        long OR_NestedheapTime = 0;

        // ------------------------------ OptionalRef - Value ---------------------
        int OR_ValueSum = 0;
        long OR_ValueTime = 0;

        // ------------------------------ OptionalRef - stack  --------------------
        int OR_StackSum = 0;
        long OR_StackTime = 0;

        // ------------------------------ OptionalRef - heapReadOnly  --------------------
        int OR_HeapReadOnlySum = 0;
        long OR_HeapReadOnlyTime = 0;
        
        // ------------------------------ OptionalRef - FastHeap via stack  --------------------
        int OR_FastHeapSum = 0;
        long OR_FastHeapTime = 0;

        // ---------------------------------REFERENCE ---------------------------------
        int refSum = 0;
        long refTime = 0;

        // ==================================== EXECUTE =====================================

        // ------------------------------ ValueType ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            valueTypeSum += testUser.valueType.someValue;
        }

        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;

        // ------------------------------- ValueType (Nested) ------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            nestedVTsum += testUser.nestedValueType.inner.someValue;
        }

        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - Value (Nested) --------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_NestedValueSum += testUser.nestedOR_vt.Value.someValue;
        }

        sw.Stop();
        OR_NestedValueTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - stack (Nested) --------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_NestedstackSum += testUser.nestedOR_vt.stack.someValue;
        }

        sw.Stop();
        OR_NestedstackTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - heap  (Nested)--------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_NestedheapSum += testUser.nestedOR_ref.heap.someValue;
        }

        sw.Stop();
        OR_NestedheapTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - Value ---------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_ValueSum += testUser.OR_valuetype.Value;
        }

        sw.Stop();
        OR_ValueTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - stack  --------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_StackSum += testUser.OR_valuetype.stack;
        }

        sw.Stop();
        OR_StackTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - heapReadOnly  --------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            OR_HeapReadOnlySum += testUser.OR_reference.heapReadOnly;
        }

        sw.Stop();
        OR_HeapReadOnlyTime = sw.ElapsedMilliseconds;
        
        // ------------------------------ OptionalRef - FastHeap  --------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // To edit the heap and auto-refresh it, use FastHeap
            // this means that you can directly read from the stack after edits
            OR_FastHeapSum += testUser.OR_reference.stack;
        }

        sw.Stop();
        OR_FastHeapTime = sw.ElapsedMilliseconds;

        // ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            refSum += testUser.reference.someValue;
        }

        sw.Stop();
        refTime = sw.ElapsedMilliseconds;


        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations:N0} iterations)\n" +
            $"Label                                               Time (ms)        Sum\n" +
            $"=====================================================================\n" +
            $"Value-Type *                                                  {valueTypeTime,6} *   {valueTypeSum}\n" +
            $"Nested Value-Type *                                     {nestedVTtime,6} *   {nestedVTsum}\n" +
            $"OptionalRef (Nested) - Value                         {OR_NestedValueTime,6}    {OR_NestedValueSum}\n" +
            $"OptionalRef (Nested) - Stack *                      {OR_NestedstackTime,6} *   {OR_NestedstackSum}\n" +
            $"OptionalRef (Nested) - Heap                         {OR_NestedheapTime,6}    {OR_NestedheapSum}\n" +
            $"OptionalRef - Value                                        {OR_ValueTime,6}    {OR_ValueSum}\n" +
            $"OptionalRef - Stack *                                     {OR_StackTime,6} *   {OR_StackSum}\n" +
            $"OptionalRef - Heap (heapReadOnly)             {OR_HeapReadOnlyTime,6}    {OR_HeapReadOnlySum}\n" +
            $"OptionalRef - After FastHeap (via stack) *    {OR_FastHeapTime,6} *   {OR_FastHeapSum}\n" +
            $"Reference Type                                            {refTime,6}    {refSum}\n"
        );
        
        // ================================ RANGE DEBUG ==================================

        long vt_vs_orStack = Math.Abs(valueTypeTime - OR_StackTime);
        long nvt_vs_nestedOrStack = Math.Abs(nestedVTtime - OR_NestedstackTime);
        long fastheap_vs_vt = Math.Abs(valueTypeTime - OR_FastHeapTime);

        UnityEngine.Debug.Log(
            $"Range Deltas (ms)\n" +
            $"---------------------------------------------\n" +
            $"Value-Type vs OptionalRef-Stack        : {vt_vs_orStack} ms\n" +
            $"Nested Value-Type vs Nested OR-Stack   : {nvt_vs_nestedOrStack} ms\n" +
            $"After FastHeap via stack vs Value-Type   : {fastheap_vs_vt} ms"
        );
        //===============================================================================

        GC.KeepAlive(
            valueTypeSum + nestedVTsum +
            OR_NestedValueSum + OR_NestedstackSum + OR_NestedheapSum +
            OR_ValueSum + OR_StackSum + OR_HeapReadOnlySum + OR_FastHeapSum +
            refSum
        );

        // ================================ ASSERTIONS ==================================
        int expectedSum = valueTypeSum;

        Assert.AreEqual(expectedSum, nestedVTsum);
        Assert.AreEqual(expectedSum, OR_NestedValueSum);
        Assert.AreEqual(expectedSum, OR_NestedstackSum);
        Assert.AreEqual(expectedSum, OR_NestedheapSum);
        Assert.AreEqual(expectedSum, OR_ValueSum);
        Assert.AreEqual(expectedSum, OR_StackSum);
        Assert.AreEqual(expectedSum, OR_HeapReadOnlySum);
        Assert.AreEqual(expectedSum, OR_FastHeapSum);
        Assert.AreEqual(expectedSum, refSum);

        long baseline = valueTypeTime;
        const long tolerance = 10;

        Assert.IsTrue(Math.Abs(OR_NestedstackTime - baseline) <= tolerance,
            $"OR Nested Stack {OR_NestedstackTime}ms not within ±{tolerance}ms of ValueType {baseline}ms");

        Assert.IsTrue(Math.Abs(OR_StackTime - baseline) <= tolerance,
            $"OR Stack {OR_StackTime}ms not within ±{tolerance}ms of ValueType {baseline}ms");
        
        Assert.IsTrue(Math.Abs(OR_FastHeapTime - baseline) <= tolerance,
            $"OR Stack {OR_FastHeapTime}ms not within ±{tolerance}ms of ValueType {baseline}ms");
        
    }

    [Test]
    public void HotLoop_SingleValue_WRITE()
    {
        const int iterations = 50_000_000;

        // ==================================== SETUP =====================================
        var testUser = new TestUser();
        var sw = new System.Diagnostics.Stopwatch();
        testUser.OR_reference.Box();
        testUser.nestedOR_ref.Box();

        // ------------------------------ ValueType ----------------------------------
        long valueTypeTime = 0;

        // ------------------------------- ValueType (Nested) ------------------------
        long nestedVTtime = 0;

        // ------------------------------ OptionalRef - Value (Nested) --------------
        long OR_NestedValueTime = 0;

        // ------------------------------ OptionalRef - stack (Nested) --------------
        long OR_NestedstackTime = 0;

        // ------------------------------ OptionalRef - heap  (Nested)--------------
        long OR_NestedheapTime = 0;

        // ------------------------------ OptionalRef - Value ---------------------
        long OR_ValueTime = 0;

        // ------------------------------ OptionalRef - stack  --------------------
        long OR_StackTime = 0;

        // ------------------------------ OptionalRef - heap  )--------------------
        long OR_HeapTime = 0;

        // ---------------------------------REFERENCE ---------------------------------
        long refTime = 0;

        // ==================================== EXECUTE =====================================

        // ------------------------------ ValueType ----------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.valueType.someValue += 1;
        }

        sw.Stop();
        valueTypeTime = sw.ElapsedMilliseconds;

        // ------------------------------- ValueType (Nested) ------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.nestedValueType.inner.someValue += 1;
        }

        sw.Stop();
        nestedVTtime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - Value (Nested) --------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Not Recommended

            var temp = testUser.nestedOR_vt.Value;
            temp.someValue += 1;
            testUser.nestedOR_vt.stack = temp;
        }

        sw.Stop();
        OR_NestedValueTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - stack (Nested) --------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            // Not Recommended
            var temp = testUser.nestedOR_ref.stack;
            temp.someValue += 1;
            testUser.nestedOR_vt.stack = temp;
        }

        sw.Stop();
        OR_NestedstackTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - heap  (Nested)--------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.nestedOR_ref.heap.someValue += 1;
        }

        sw.Stop();
        OR_NestedheapTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - Value ---------------------

        // has no setter for refernce semantics

        // ------------------------------ OptionalRef - stack  --------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.OR_valuetype.stack += 1;
        }

        sw.Stop();
        OR_StackTime = sw.ElapsedMilliseconds;

        // ------------------------------ OptionalRef - heap  )--------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.OR_reference.heap += 1;
        }

        sw.Stop();
        OR_HeapTime = sw.ElapsedMilliseconds;

        // ---------------------------------REFERENCE ---------------------------------
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            testUser.reference.someValue += 1;
        }

        sw.Stop();
        refTime = sw.ElapsedMilliseconds;


        UnityEngine.Debug.Log(
            $"Hot-loop Single Value Read ({iterations:N0} iterations)\n" +
            $"Label                                               Time (ms)        Sum\n" +
            $"=====================================================================\n" +
            $"Value-Type                                                   {valueTypeTime,6}    \n" +
            $"Nested Value-Type                                      {nestedVTtime,6}    \n" +
            $"OptionalRef (Nested) - Value                       {OR_NestedValueTime,6}    \n" +
            $"OptionalRef (Nested) - Stack                      {OR_NestedstackTime,6}    \n" +
            $"OptionalRef (Nested) - Heap *                     {OR_NestedheapTime,6} *   \n" +
            $"OptionalRef - Value                                         n/a    \n" +
            $"OptionalRef - Stack                                      {OR_StackTime,6}    \n" +
            $"OptionalRef - Heap *                                    {OR_HeapTime,6} *   \n" +
            $"Reference Type *                                          {refTime,6} *   \n"
        );
        
        UnityEngine.Debug.Log(
            $"Range Deltas (ms)\n" +
            $"---------------------------------------------------------\n" +
            $"OptionalRef (Nested) - Heap vs Reference Type: {Math.Abs(OR_NestedheapTime - refTime)}ms\n" +
            $"OptionalRef - Heap vs Reference Type:          {Math.Abs(OR_HeapTime - refTime)}ms\n"
        );
        
        // ================================ TIME ASSERTIONS ==================================
        // Tolerance for starred rows (OptionalRef Nested Heap, OptionalRef Heap, Reference Type)
        int tolerance = 10;

        Assert.IsTrue(Math.Abs(OR_NestedheapTime - OR_HeapTime) <= tolerance,
            $"OptionalRef Nested-Heap time {OR_NestedheapTime}ms differs from OptionalRef-Heap time {OR_HeapTime}ms by more than {tolerance}ms");

        Assert.IsTrue(Math.Abs(OR_HeapTime - refTime) <= tolerance,
            $"OptionalRef-Heap time {OR_HeapTime}ms differs from Reference Type time {refTime}ms by more than {tolerance}ms");

        Assert.IsTrue(Math.Abs(OR_NestedheapTime - refTime) <= tolerance,
            $"OptionalRef Nested-Heap time {OR_NestedheapTime}ms differs from Reference Type time {refTime}ms by more than {tolerance}ms");
    }
}
