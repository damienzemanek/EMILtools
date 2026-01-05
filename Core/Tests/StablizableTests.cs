using EMILtools.Core;
using NUnit.Framework;
using System;

public class StablizableTests
{
    class StablizableUser : IStablizableUser
    {
        [Stabilize] public Stablizable<float> speed = new();
        public Stablizable<int> jumpHeight = new(); // Not attributed
    }

    // -------------------------
    // Non-stable behavior tests
    // -------------------------

    [Test]
    public void NonStable_ValuesMoveFreely()
    {
        var user = new StablizableUser();

        user.speed.Value = 10f;
        Assert.AreEqual(10f, user.speed.Value);

        var copy = user.speed;
        copy.Value = 20f;

        // Non-stable copies do not affect original
        Assert.AreEqual(10f, user.speed.Value);
        Assert.AreEqual(20f, copy.Value);
    }

    [Test]
    public void NonStable_CopyBeforeStabilize_RemainsIndependent()
    {
        var user = new StablizableUser();

        var preCopy = user.speed; // copy before stabilization
        user.speed.Value = 5f;

        preCopy.Value = 50f; // should be allowed
        Assert.AreEqual(50f, preCopy.Value);
        Assert.AreEqual(5f, user.speed.Value);
    }

    [Test]
    public void NonStable_CopyAfterStabilize_BecomesReference()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed(); // stabilize only attributed field

        var postCopy = user.speed; // copy after stabilization
        postCopy.Value = 7f;

        // Reference semantics
        Assert.AreEqual(7f, user.speed.Value);
        Assert.AreEqual(7f, postCopy.Value);
    }

    [Test]
    public void NonAttributedField_NotStabilized()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed(); // only speed is stabilized

        var copy = user.jumpHeight;
        copy.Value = 15; // still value semantics
        Assert.AreEqual(15, copy.Value);

        var original = user.jumpHeight;
        Assert.AreEqual(0, original.Value); // original untouched
    }

    // -------------------------
    // Stable behavior tests
    // -------------------------

    [Test]
    public void Stable_CopiesShareReference()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed();

        user.speed.Value = 10f;

        var copy = user.speed;
        copy.Value = 50f;

        Assert.AreEqual(50f, user.speed.Value);
    }

    [Test]
    public void Stable_ImplicitConversion()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed();

        user.speed.Value = 42f;
        float x = user.speed; // implicit conversion
        Assert.AreEqual(42f, x);
    }

    [Test]
    public void Stable_ReassignKeepsValue()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed();

        user.speed.Value = 30f;

        // re-stabilize manually
        user.speed.Stabilize(user);
        Assert.AreEqual(30f, user.speed.Value);
    }

    [Test]
    public void Stable_VariableCanMoveFreely_PostCopy()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed();

        var moved = user.speed;
        var another = moved;

        another.Value = 100f;

        Assert.AreEqual(100f, user.speed.Value);
    }

    [Test]
    public void Stable_MultipleStablizables_IndependentReferences()
    {
        var user = new StablizableUser();
        user.StabilizeAll(); // stabilize all fields

        user.speed.Value = 10f;
        user.jumpHeight.Value = 5;

        var speedCopy = user.speed;
        var jumpCopy = user.jumpHeight;

        speedCopy.Value = 20f;
        jumpCopy.Value = 15;

        Assert.AreEqual(20f, user.speed.Value);
        Assert.AreEqual(15, user.jumpHeight.Value);
    }

    // -------------------------
    // Edge cases
    // -------------------------

    [Test]
    public void StableCheck_IsStableReturnsCorrectly()
    {
        var user = new StablizableUser();
        Assert.IsFalse(user.speed.isStable, "Non-stable before stabilization");

        user.StabilizeAttributed();
        Assert.IsTrue(user.speed.isStable, "Stable after stabilization");
    }

    [Test]
    public void PreStabilizationCopy_RemainsValue()
    {
        var user = new StablizableUser();

        var preCopy = user.speed;
        user.StabilizeAttributed();

        preCopy.Value = 33; // still allowed
        Assert.AreEqual(33, preCopy.Value);

        var postCopy = user.speed;
        postCopy.Value = 77;

        Assert.AreEqual(77, user.speed.Value);
    }

    [Test]
    public void ManualStructStabilization_AddsUserToHashSet()
    {
        var user = new StablizableUser();

        // manually stabilize single field
        user.jumpHeight.Stabilize(user);

        Assert.IsTrue(Stabilizer.stabilizedUsers.Contains(user));
        Assert.IsTrue(user.jumpHeight.isStable);
    }

    [Test]
    public void ManualStabilizationFromStruct_DoesNotDuplicateHashSetEntry()
    {
        var user = new StablizableUser();
        user.StabilizeAttributed(); // stabilizes speed

        int countBefore = Stabilizer.stabilizedUsers.Count;
        user.speed.Stabilize(user, fromStabilizer: true); // simulate stabilizer calling struct
        int countAfter = Stabilizer.stabilizedUsers.Count;

        Assert.AreEqual(countBefore, countAfter); // no duplicate entry
    }

    [Test]
    public void MultipleCopies_PreAndPostStabilize_WorkAsExpected()
    {
        var user = new StablizableUser();

        var preCopy = user.speed;   // before stabilization
        user.StabilizeAttributed();
        var postCopy = user.speed;  // after stabilization

        preCopy.Value = 33; // non-stable, allowed
        postCopy.Value = 77; // stable reference

        Assert.AreEqual(77, user.speed.Value);
        Assert.AreEqual(33, preCopy.Value);
    }

    [Test]
    public void MultiplePreCopies_IndependentFromEachOther()
    {
        var user = new StablizableUser();

        var copy1 = user.speed;
        var copy2 = user.speed;

        copy1.Value = 5;
        copy2.Value = 10;

        Assert.AreEqual(5, copy1.Value);
        Assert.AreEqual(10, copy2.Value);
        Assert.AreEqual(0, user.speed.Value);
    }

    [Test]
    public void StabilizeAll_IncludesNonAttributedFields()
    {
        var user = new StablizableUser();
        user.StabilizeAll();

        Assert.IsTrue(user.speed.isStable);
        Assert.IsTrue(user.jumpHeight.isStable);
    }
    
    
}
