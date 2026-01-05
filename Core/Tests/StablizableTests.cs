using EMILtools.Core;
using NUnit.Framework;
using System;

public class StablizableTests
{
    class StablizableUser : IStablizableUser
    {
        public Stablizable<float> speed = new();
        public Stablizable<int> jumpHeight = new();
    }

    // -------------------------
    // Non-stable behavior tests
    // -------------------------

    [Test]
    public void NonStable_ValuesCanMoveFreely()
    {
        var user = new StablizableUser();

        // Non-stable: never called Stabilize()
        user.speed.Value = 10f;
        Assert.AreEqual(10f, user.speed.Value);

        var copy = user.speed;
        copy.Value = 20f;

        // Non-stable copies do NOT affect original (value semantics)
        Assert.AreEqual(10f, user.speed.Value);
        Assert.AreEqual(20f, copy.Value);
    }

    [Test]
    public void NonStable_CopyBeforeStabilize_RemainsUsable()
    {
        var user = new StablizableUser();

        var copy = user.speed; // copy made before stabilization
        user.speed.Value = 5f;

        // Access and modification still allowed
        copy.Value = 50f;
        Assert.AreEqual(50f, copy.Value);
    }

    [Test]
    public void NonStable_CopyAfterStabilize_BehavesCorrectly()
    {
        var user = new StablizableUser();
        user.Stabilize(); // stabilizes owner

        var copy = user.jumpHeight; // copy after stabilization
        copy.Value = 7; // allowed because copy points to the stabilized reference
        Assert.AreEqual(7, copy.Value);
    }

    // -------------------------
    // Stable behavior tests
    // -------------------------

    [Test]
    public void Stable_CopiesShareReference()
    {
        var user = new StablizableUser();
        user.Stabilize();

        user.speed.Value = 10f;

        var copy = user.speed; // copy made after stabilization
        copy.Value = 50f;

        // Both point to same reference
        Assert.AreEqual(50f, user.speed.Value);
    }

    [Test]
    public void Stable_ImplicitConversion_Works()
    {
        var user = new StablizableUser();
        user.Stabilize();

        user.speed.Value = 42f;
        float x = user.speed; // implicit conversion
        Assert.AreEqual(42f, x);
    }

    [Test]
    public void Stable_MultipleStablizables_WorkIndependently()
    {
        var user = new StablizableUser();
        user.Stabilize();

        user.speed.Value = 10f;
        user.jumpHeight.Value = 5;

        // speed copy affects original
        var speedCopy = user.speed;
        speedCopy.Value = 20f;
        Assert.AreEqual(20f, user.speed.Value);

        // jumpHeight copy affects original too
        var jumpCopy = user.jumpHeight;
        jumpCopy.Value = 15;
        Assert.AreEqual(15, user.jumpHeight.Value);
    }

    [Test]
    public void Stable_ReassignKeepsValue()
    {
        var user = new StablizableUser();
        user.Stabilize();

        user.speed.Value = 30f;

        // Re-stabilize does not reset value
        user.speed.Stabilize(user);
        Assert.AreEqual(30f, user.speed.Value);
    }

    [Test]
    public void Stable_VariableCanMoveFreely()
    {
        var user = new StablizableUser();
        user.Stabilize();

        var moved = user.speed;
        var another = moved;

        another.Value = 100f;

        Assert.AreEqual(100f, user.speed.Value);
    }

    // -------------------------
    // Edge cases
    // -------------------------

    [Test]
    public void Stable_AccessBeforeStabilization_ThrowsIfNeeded()
    {
        var c = new Stablizable<int>();

        // Non-stable access is allowed (not stabilized, no owner)
        c.Value = 5;
        Assert.AreEqual(5, c.Value);
    }

    [Test]
    public void CopyBeforeStabilization_RemainsNonStable()
    {
        var user = new StablizableUser();
        var copy = user.speed; // pre-stabilization copy
        user.Stabilize(); // owner stabilized

        // copy remains non-stable (myUser in copy is null)
        copy.Value = 25; // allowed
        Assert.AreEqual(25, copy.Value);
    }

    [Test]
    public void StableCheck_IsStableReturnsCorrectly()
    {
        var user = new StablizableUser();
        Assert.IsFalse(user.speed.isStable, "Non-stable before Stabilize");

        user.Stabilize();
        Assert.IsTrue(user.speed.isStable, "Stable after Stabilize");
    }

    [Test]
    public void MultipleCopies_PreAndPostStabilize_WorkAsExpected()
    {
        var user = new StablizableUser();

        var preCopy = user.speed;   // before stabilization
        user.Stabilize();
        var postCopy = user.speed;  // after stabilization

        preCopy.Value = 33; // non-stable, allowed
        postCopy.Value = 77; // stable reference

        Assert.AreEqual(77, user.speed.Value);
        Assert.AreEqual(33, preCopy.Value);
    }
}
