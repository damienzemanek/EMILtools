using System;
using EMILtools.Core;
using NUnit.Framework;
using System.Diagnostics;
using UnityEngine;
using static EMILtools.Core.Stabilizer;
using Debug = UnityEngine.Debug;

public class StablizableTests
{
    class TestUser : IStablizableUser
    {
        [Stabilize] public Stablizable<float> speed = new();
        public Stablizable<int> jumpHeight = new(); // Not attributed
    }

    // -------------------------
    // 1️⃣ Non-stable behavior tests
    // -------------------------

    [Test]
    public void NonStable_Copies_Seperate()
    {
        var user = new TestUser();
        user.speed.stack = 10f;
        Assert.AreEqual(10f, user.speed.Get);

        var copy = user.speed;
        copy.stack = 20f;

        Assert.AreEqual(10f, user.speed.Get);
        Assert.AreEqual(20f, copy.Get);
    }
    
    [Test]
    public void NonStable_CopyAfterStabilize_BecomesReference()
    {
        var user = new TestUser();
        user.StabilizeAttributed();

        var postCopy = user.speed;
        postCopy.heap += 7f;
        Debug.Log($" orig ref {user.speed.sharedheap} copy ref {postCopy.sharedheap}" +
                  $" are same? {ReferenceEquals(postCopy.inspectHeap, user.speed.inspectHeap)}");
        Debug.Log($"before refresh: orig {user.speed.Get} copy {postCopy.Get} refval {user.speed.sharedheap.unbox}");
        
        Debug.Log($"after refresh: orig {user.speed.Get} copy {postCopy.Get} refval {user.speed.sharedheap.unbox}");

        Assert.AreNotEqual(null, user.speed.sharedheap, "orig reference is null");
        Assert.AreNotEqual(null, postCopy.sharedheap, "postCopy reference is null");

        Assert.IsTrue(ReferenceEquals(postCopy.inspectHeap, user.speed.inspectHeap), 
            "Post copy and original should share the same heap reference");

        Assert.AreEqual(7f, postCopy.Get);
        Assert.AreEqual(7f, user.speed.Get);
    }
    
    [Test]
    public void NonAttributedField_NotStabilized()
    {
        var user = new TestUser();
        user.StabilizeAttributed();

        var jumpcopy = user.jumpHeight;
        jumpcopy.stack = 15;
        Assert.AreEqual(15, jumpcopy.Get);

        var original = user.jumpHeight;
        Assert.AreEqual(0, original.Get);
    }

    // -------------------------
    // 2️⃣ Stable behavior tests
    // -------------------------

     [Test]
     public void Stable_CopiesShareReference()
     {
         var user = new TestUser();
         user.StabilizeAttributed();

         user.speed.heap = 10f;

         var copy = user.speed;
         copy.heap = 50f;

         Assert.AreEqual(50f, user.speed.Get);
         Assert.AreEqual(50f, copy.Get);
     }

     [Test]
     public void PreStabilizationCopy_RemainsIndependent()
     {
         var user = new TestUser();

         var preCopy = user.speed;
         user.StabilizeAttributed();
         var postCopy = user.speed;

         preCopy.stack = 33f;
         postCopy.heap = 77f;

         Assert.AreEqual(77f, postCopy.Get);
         Assert.AreEqual(77f, user.speed.Get);
         Assert.AreEqual(33f, preCopy.Get);
     }

     [Test]
     public void StabilizeAll_IncludesNonAttributedFields()
     {
         var user = new TestUser();
         user.StabilizeAll();

         user.speed.heap = 10f;
         user.jumpHeight.heap = 5;

         Assert.IsTrue(user.speed.isStable);
         Assert.IsTrue(user.jumpHeight.isStable);
         Assert.AreEqual(10f, user.speed.Get);
         Assert.AreEqual(5, user.jumpHeight.Get);
     }

     [Test]
     public void ManualStructStabilization_AddsUserToHashSet()
     {
         var user = new TestUser();
         user.jumpHeight.Stabilize(user);

         Assert.IsTrue(stabilizedUsers.Contains(user));
         Assert.IsTrue(user.jumpHeight.isStable);
     }

     [Test]
     public void ManualStabilizationFromStruct_DoesNotDuplicateHashSetEntry()
     {
         var user = new TestUser();
         user.StabilizeAttributed();

         int countBefore = stabilizedUsers.Count;
         user.speed.Stabilize(user, fromStabilizer: true);
         int countAfter = stabilizedUsers.Count;

         Assert.AreEqual(countBefore, countAfter);
     }

     [Test]
     public void MultipleCopies_PreAndPostStabilize_WorkAsExpected()
     {
         var user = new TestUser();

         var preCopy = user.speed;
         user.StabilizeAttributed();
         var postCopy = user.speed;

         preCopy.stack = 33f;
         postCopy.heap = 77f;

         Assert.AreEqual(77f, user.speed.Get);
         Assert.AreEqual(33f, preCopy.Get);
     }

     [Test]
     public void MultiplePreCopies_IndependentFromEachOther()
     {
         var user = new TestUser();

         var copy1 = user.speed;
         var copy2 = user.speed;

         copy1.stack = 5f;
         copy2.stack = 10f;

         Assert.AreEqual(5f, copy1.Get);
         Assert.AreEqual(10f, copy2.Get);
         Assert.AreEqual(0f, user.speed.Get);
     }

     [Test]
     public void SelectiveSharing_PreAndPostStabilizationBehavior()
     {
         var user = new TestUser();
         var preCopy = user.speed;
         user.StabilizeAttributed();

         var postCopy1 = user.speed;
         var postCopy2 = user.speed;

         postCopy1.heap = 5f;
         Assert.AreEqual(5f, postCopy2.Get);

         preCopy.stack = 10f;
         Assert.AreEqual(10f, preCopy.Get);
         Assert.AreEqual(5f, postCopy1.Get);
     }

     // -------------------------
     // 3️⃣ Passing structs to methods
     // -------------------------

     [Test]
     public void PassingPreStabilizationStructByValue_DoesNotAffectOriginal()
     {
         var user = new TestUser();
         user.speed.stack = 10f;

         float IncrementHealth(Stablizable<float> h, int amount)
         {
             h.stack = amount;
             return h.Get;
         }

         float result = IncrementHealth(user.speed, 5);
         Assert.AreEqual(5f, result);
         Assert.AreEqual(10f, user.speed.Get);
     }

     [Test]
     public void PassingPostStabilizationStructByValue_UpdatesSharedReference()
     {
         var user = new TestUser();
         user.speed.stack = 10f;
         user.StabilizeAttributed();

         float IncrementHealth(Stablizable<float> h, int amount)
         {
             h.heap = amount;
             return h.Get;
         }

         float result = IncrementHealth(user.speed, 5);
         Assert.AreEqual(5f, result);
         Assert.AreEqual(5f, user.speed.Get);
     }

     [Test]
     public void PreStabilizationCopiesRemainIndependent_AfterStabilization()
     {
         var user = new TestUser();
         var pre1 = user.speed;
         var pre2 = user.speed;
         user.StabilizeAttributed();

         pre1.stack = 5f;
         pre2.stack = 7f;

         var post = user.speed;
         post.heap = 42f;

         Assert.AreEqual(42f, user.speed.Get);
         Assert.AreEqual(5f, pre1.Get);
         Assert.AreEqual(7f, pre2.Get);
     }

     // -------------------------
     // 4️⃣ Dynamic editing
     // -------------------------

     [Test]
     public void DynamicEditing_PreStabilization_IsIndependent()
     {
         var user = new TestUser();

         var preCopy = user.speed;
         preCopy.stack = 5f;

         var original = user.speed;
         original.stack = 15f;

         Assert.AreEqual(5f, preCopy.Get);
         Assert.AreEqual(15f, original.Get);
     }

     [Test]
     public void DynamicEditing_PostStabilization_SharedAcrossSystems()
     {
         var user = new TestUser();
         user.StabilizeAttributed();

         var systemA = user.speed;
         var systemB = user.speed;

         systemA.heap = 3f;
         Assert.AreEqual(3f, systemB.Get);

         systemB.heap = 7f;
         Assert.AreEqual(7f, systemA.Get);
     }

     [Test]
     public void MultipleSystems_ModifySharedVariableSafely()
     {
         var user = new TestUser();
         user.StabilizeAttributed();

         var sys1 = user.speed;
         var sys2 = user.speed;
         var sys3 = user.speed;

         sys1.heap = 10f;
         sys2.heap = 20f;
         sys3.heap = 30f;

         Assert.AreEqual(30f, user.speed.Get);
         Assert.AreEqual(30f, sys1.Get);
         Assert.AreEqual(30f, sys2.Get);
     }

     // -------------------------
     // 5️⃣ Pre-stabilization copies throw on heap access
     // -------------------------

     [Test]
     public void PreStabilizationSystemCopies_ThrowAfterStabilization_UsingHeap()
     {
         var user = new TestUser();
         var copy1 = user.speed; 
         var copy2 = user.speed; 
         user.StabilizeAttributed();

         Assert.Throws<InvalidOperationException>(() => copy1.heap = 5f);
         Assert.Throws<InvalidOperationException>(() => copy2.heap = 7f);
     }

     // -------------------------
     // 6️⃣ Performance / hot loop simulation
     // -------------------------

     [Test]
     public void ValueAccess_Fast_WhenNotStabilized()
     {
         var st = new Stablizable<int>();
         st.stack = 0;

         int sum = 0;
         for (int i = 0; i < 100_000; i++)
             sum += st.Get;

         Assert.AreEqual(0, st.Get);
     }

     [Test]
     public void StackLocalAccess_IsFasterThanReferenceAfterManyReads()
     {
         var localStruct = new Stablizable<int>();
         localStruct.stack = 123;

         int sum1 = 0;
         var sw = Stopwatch.StartNew();
         for (int i = 0; i < 1_000_000; i++)
             sum1 += localStruct.Get;
         sw.Stop();
         long stackTime = sw.ElapsedMilliseconds;

         var user = new TestUser();
         user.speed.stack = 123;
         user.StabilizeAttributed();

         float sum2 = 0;
         sw.Restart();
         for (int i = 0; i < 1_000_000; i++)
             sum2 += user.speed.Get;
         sw.Stop();
         long refTime = sw.ElapsedMilliseconds;

         Debug.Log($"Stack-local: {stackTime}ms, Reference: {refTime}ms");
         Assert.IsTrue(stackTime <= refTime);
     }

     [Test]
     public void RepeatedPasses_ReferenceDereference_BreakEvenAfterFewReads()
     {
         var user = new TestUser();
         user.speed.stack = 0;
         user.StabilizeAttributed();

         float sum = 0;
         for (int i = 0; i < 10; i++)
             sum += user.speed.Get;

         Assert.AreEqual(0, sum);
     }
 }
