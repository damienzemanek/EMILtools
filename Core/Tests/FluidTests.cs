// using System;
// using EMILtools.Core;
// using NUnit.Framework;
// using System.Diagnostics;
// using UnityEngine;
// using Debug = UnityEngine.Debug;
//
// public class FluidTests
// {
//     class TestUser : IBoxUser
//     {
//         [AutoBox] public Fluid<float> speed = new();
//         public Fluid<int> jumpHeight = new(); // Not attributed
//     }
//
//     // -------------------------
//     // 1️⃣ Non-stable behavior tests
//     // -------------------------
//
//     [Test]
//     public void NonStable_Copies_Seperate()
//     {
//         var user = new TestUser();
//         user.speed.stack = 10f;
//         Assert.AreEqual(10f, user.speed.Value);
//
//         var copy = user.speed;
//         copy.stack = 20f;
//
//         Assert.AreEqual(10f, user.speed.Value);
//         Assert.AreEqual(20f, copy.Value);
//     }
//     
//     [Test]
//     public void NonStable_CopyAfterStabilize_BecomesReference()
//     {
//         var user = new TestUser();
//
//         var postCopy = user.speed;
//         postCopy.heap += 7f;
//         Debug.Log($" orig ref {user.speed.sharedheap} copy ref {postCopy.sharedheap}" +
//                   $" are same? {ReferenceEquals(postCopy.heapReadOnly, user.speed.heapReadOnly)}");
//         Debug.Log($"before refresh: orig {user.speed.Value} copy {postCopy.Value} refval {user.speed.sharedheap.unbox}");
//         
//         Debug.Log($"after refresh: orig {user.speed.Value} copy {postCopy.Value} refval {user.speed.sharedheap.unbox}");
//
//         Assert.AreNotEqual(null, user.speed.sharedheap, "orig reference is null");
//         Assert.AreNotEqual(null, postCopy.sharedheap, "postCopy reference is null");
//
//         Assert.IsTrue(ReferenceEquals(postCopy.heapReadOnly, user.speed.heapReadOnly), 
//             "Post copy and original should share the same heap reference");
//
//         Assert.AreEqual(7f, postCopy.Value);
//         Assert.AreEqual(7f, user.speed.Value);
//     }
//     
//     [Test]
//     public void NonAttributedField_NotStabilized()
//     {
//         var user = new TestUser();
//         user.BoxAutos();
//
//         var jumpcopy = user.jumpHeight;
//         jumpcopy.stack = 15;
//         Assert.AreEqual(15, jumpcopy.Value);
//
//         var original = user.jumpHeight;
//         Assert.AreEqual(0, original.Value);
//     }
//
//     // -------------------------
//     // 2️⃣ Stable behavior tests
//     // -------------------------
//
//      [Test]
//      public void Stable_CopiesShareReference()
//      {
//          var user = new TestUser();
//          user.BoxAutos();
//
//          user.speed.FastHeap(user.speed.heap = 10f);
//          user.speed.heap = 10f;
//
//          var copy = user.speed;
//          copy.heap = 50f;
//
//          Assert.AreEqual(50f, user.speed.Value);
//          Assert.AreEqual(50f, copy.Value);
//      }
//
//      [Test]
//      public void PreStabilizationCopy_RemainsIndependent()
//      {
//          var user = new TestUser();
//
//          var preCopy = user.speed;
//          user.BoxAutos();
//          var postCopy = user.speed;
//
//          preCopy.stack = 33f;
//          postCopy.heap = 77f;
//
//          Assert.AreEqual(77f, postCopy.Value);
//          Assert.AreEqual(77f, user.speed.Value);
//          Assert.AreEqual(33f, preCopy.Value);
//      }
//
//      [Test]
//      public void StabilizeAll_IncludesNonAttributedFields()
//      {
//          var user = new TestUser();
//          user.BoxAll();
//
//          user.speed.heap = 10f;
//          user.jumpHeight.heap = 5;
//
//          Assert.IsTrue(user.speed.isRef);
//          Assert.IsTrue(user.jumpHeight.isRef);
//          Assert.AreEqual(10f, user.speed.Value);
//          Assert.AreEqual(5, user.jumpHeight.Value);
//      }
//      
//      
//      [Test]
//      public void MultipleCopies_PreAndPostStabilize_WorkAsExpected()
//      {
//          var user = new TestUser();
//
//          var preCopy = user.speed;
//          user.BoxAutos();
//          var postCopy = user.speed;
//
//          preCopy.stack = 33f;
//          postCopy.heap = 77f;
//
//          Assert.AreEqual(77f, user.speed.Value);
//          Assert.AreEqual(33f, preCopy.Value);
//      }
//
//      [Test]
//      public void MultiplePreCopies_IndependentFromEachOther()
//      {
//          var user = new TestUser();
//
//          var copy1 = user.speed;
//          var copy2 = user.speed;
//
//          copy1.stack = 5f;
//          copy2.stack = 10f;
//
//          Assert.AreEqual(5f, copy1.Value);
//          Assert.AreEqual(10f, copy2.Value);
//          Assert.AreEqual(0f, user.speed.Value);
//      }
//
//      [Test]
//      public void SelectiveSharing_PreAndPostStabilizationBehavior()
//      {
//          var user = new TestUser();
//          var preCopy = user.speed;
//          user.BoxAutos();
//
//          var postCopy1 = user.speed;
//          var postCopy2 = user.speed;
//
//          postCopy1.heap = 5f;
//          Assert.AreEqual(5f, postCopy2.Value);
//
//          preCopy.stack = 10f;
//          Assert.AreEqual(10f, preCopy.Value);
//          Assert.AreEqual(5f, postCopy1.Value);
//      }
//
//      // -------------------------
//      // 3️⃣ Passing structs to methods
//      // -------------------------
//
//      [Test]
//      public void PassingPreStabilizationStructByValue_DoesNotAffectOriginal()
//      {
//          var user = new TestUser();
//          user.speed.stack = 10f;
//
//          float IncrementHealth(Fluid<float> h, int amount)
//          {
//              h.stack = amount;
//              return h.Value;
//          }
//
//          float result = IncrementHealth(user.speed, 5);
//          Assert.AreEqual(5f, result);
//          Assert.AreEqual(10f, user.speed.Value);
//      }
//
//      [Test]
//      public void PassingPostStabilizationStructByValue_UpdatesSharedReference()
//      {
//          var user = new TestUser();
//          user.speed.stack = 10f;
//          user.BoxAutos();
//
//          float IncrementHealth(Fluid<float> h, int amount)
//          {
//              h.heap = amount;
//              return h.Value;
//          }
//
//          float result = IncrementHealth(user.speed, 5);
//          Assert.AreEqual(5f, result);
//          Assert.AreEqual(5f, user.speed.Value);
//      }
//
//      [Test]
//      public void PreStabilizationCopiesRemainIndependent_AfterStabilization()
//      {
//          var user = new TestUser();
//          var pre1 = user.speed;
//          var pre2 = user.speed;
//          user.BoxAutos();
//
//          pre1.stack = 5f;
//          pre2.stack = 7f;
//
//          var post = user.speed;
//          post.heap = 42f;
//
//          Assert.AreEqual(42f, user.speed.Value);
//          Assert.AreEqual(5f, pre1.Value);
//          Assert.AreEqual(7f, pre2.Value);
//      }
//
//      // -------------------------
//      // 4️⃣ Dynamic editing
//      // -------------------------
//
//      [Test]
//      public void DynamicEditing_PreStabilization_IsIndependent()
//      {
//          var user = new TestUser();
//
//          var preCopy = user.speed;
//          preCopy.stack = 5f;
//
//          var original = user.speed;
//          original.stack = 15f;
//
//          Assert.AreEqual(5f, preCopy.Value);
//          Assert.AreEqual(15f, original.Value);
//      }
//
//      [Test]
//      public void DynamicEditing_PostStabilization_SharedAcrossSystems()
//      {
//          var user = new TestUser();
//          user.BoxAutos();
//
//          var systemA = user.speed;
//          var systemB = user.speed;
//
//          systemA.heap = 3f;
//          Assert.AreEqual(3f, systemB.Value);
//
//          systemB.heap = 7f;
//          Assert.AreEqual(7f, systemA.Value);
//      }
//
//      [Test]
//      public void MultipleSystems_ModifySharedVariableSafely()
//      {
//          var user = new TestUser();
//          user.BoxAutos();
//
//          var sys1 = user.speed;
//          var sys2 = user.speed;
//          var sys3 = user.speed;
//
//          sys1.heap = 10f;
//          sys2.heap = 20f;
//          sys3.heap = 30f;
//
//          Assert.AreEqual(30f, user.speed.Value);
//          Assert.AreEqual(30f, sys1.Value);
//          Assert.AreEqual(30f, sys2.Value);
//      }
//
//      // -------------------------
//      // 5️⃣ Pre-stabilization copies throw on heap access
//      // -------------------------
//
//      [Test]
//      public void PreStabilizationSystemCopies_ThrowAfterStabilization_UsingHeap()
//      {
//          var user = new TestUser();
//          var copy1 = user.speed; 
//          var copy2 = user.speed; 
//          user.BoxAutos();
//
//          Assert.Throws<InvalidOperationException>(() => copy1.heap = 5f);
//          Assert.Throws<InvalidOperationException>(() => copy2.heap = 7f);
//      }
//
//      // -------------------------
//      // 6️⃣ Performance / hot loop simulation
//      // -------------------------
//
//      [Test]
//      public void ValueAccess_Fast_WhenNotStabilized()
//      {
//          var st = new Fluid<int>();
//          st.stack = 0;
//
//          int sum = 0;
//          for (int i = 0; i < 100_000; i++)
//              sum += st.Value;
//
//          Assert.AreEqual(0, st.Value);
//      }
//
//      [Test]
//      public void StackLocalAccess_IsFasterThanReferenceAfterManyReads()
//      {
//          var localStruct = new Fluid<int>();
//          localStruct.stack = 123;
//
//          int sum1 = 0;
//          var sw = Stopwatch.StartNew();
//          for (int i = 0; i < 1_000_000; i++)
//              sum1 += localStruct.Value;
//          sw.Stop();
//          long stackTime = sw.ElapsedMilliseconds;
//
//          var user = new TestUser();
//          user.speed.stack = 123;
//          user.BoxAutos();
//
//          float sum2 = 0;
//          sw.Restart();
//          for (int i = 0; i < 1_000_000; i++)
//              sum2 += user.speed.Value;
//          sw.Stop();
//          long refTime = sw.ElapsedMilliseconds;
//
//          Debug.Log($"Stack-local: {stackTime}ms, Reference: {refTime}ms");
//          Assert.IsTrue(stackTime <= refTime);
//      }
//
//      [Test]
//      public void RepeatedPasses_ReferenceDereference_BreakEvenAfterFewReads()
//      {
//          var user = new TestUser();
//          user.speed.stack = 0;
//          user.BoxAutos();
//
//          float sum = 0;
//          for (int i = 0; i < 10; i++)
//              sum += user.speed.Value;
//
//          Assert.AreEqual(0, sum);
//      }
//  }
