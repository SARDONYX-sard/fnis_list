namespace fnis_list;

/// <summary>
/// Core FNIS animation types from <c>&lt;AnimType&gt;</c> syntax.
/// </summary>
/// <remarks>
/// Based on and quoted from Fore's
/// <c>FNIS for Modders_V6.2.pdf</c> (c)Fore
/// which is part of the FNIS (Fores New Idles in Skyrim) modding documentation.
/// </remarks>
public enum FnisAnimType
{
    /// <summary>
    /// <c>b</c> – Basic: simple idle animation with one animation file.
    /// </summary>
    Basic,

    /// <summary>
    /// <c>s</c> – Sequenced Animation (SA): first of at least 2 animations played as a sequence.
    /// </summary>
    Sequenced,

    /// <summary>
    /// <c>so</c> – Sequenced Optimized: SA with AnimObjects and optimized Equip/UnEquip.
    /// </summary>
    SequencedOptimized,

    /// <summary>
    /// <c>fu</c> – Furniture Animation: first of at least 3 animations played on a furniture object.
    /// </summary>
    Furniture,

    /// <summary>
    /// <c>fuo</c> – Furniture Animation Optimized: fu with AnimObjects and optimized Equip/UnEquip.
    /// </summary>
    FurnitureOptimized,

    /// <summary>
    /// <c>+</c> – Second-to-last animation of a s/so/fu/fuo or ch definition.
    /// </summary>
    SequencedContinued,

    /// <summary>
    /// <c>o</c> – AnimObject: basic animation with one or more AnimObjects.
    /// </summary>
    AnimObject,

    /// <summary>
    /// <c>ofa</c> – Offset Arm Animation: modifies arm position while other animations play.
    /// </summary>
    OffsetArm,

    /// <summary>
    /// <c>pa</c> – Paired Animation: contains animation data for two actors in one animation file.
    /// </summary>
    Paired,

    /// <summary>
    /// <c>km</c> – KillMove: paired animation used for the final blow in combat.
    /// </summary>
    KillMove,

    /// <summary>
    /// <c>AAPrefix, AASet, T</c> – Alternate Animation.
    /// </summary>
    Alternate,

    /// <summary>
    /// <c>ch</c> – Chair Animation.
    /// </summary>
    Chair,

    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// An animation variable type added for cases where <c>AnimVar</c>
    /// is declared at the top of the FNIS list.
    /// </summary>
    AnimVar,
}

/// <summary>
/// FNIS animation modifier flags from <c>&lt;option&gt;</c> syntax.
/// </summary>
/// <remarks>
/// Based on and quoted from Fore's
/// <c>FNIS for Modders_V6.2.pdf</c> (c) Fore,
/// which is part of the FNIS (Fores New Idles in Skyrim) modding documentation.
/// </remarks>
[System.Flags]
public enum FnisAnimFlags : uint
{
    /// <summary>
    /// No special options.
    /// </summary>
    None = 0,

    /// <summary>
    /// <c>a</c> – Acyclic: plays only once (default is cyclic loop).
    /// </summary>
    Acyclic = 1u,

    /// <summary>
    /// <c>o</c> – Animation uses one or more AnimObjects.
    /// </summary>
    AnimObjects = 1u << 1,

    /// <summary>
    /// <c>ac</c> – Animated Camera: allows camera control via <c>Camera3rd [Cam3]</c> bone.
    /// </summary>
    AnimatedCamera = 1u << 2,

    /// <summary>
    /// <c>ac1</c> – Animated Camera Set: enable animated camera at animation start.
    /// </summary>
    AnimatedCameraSet = 1u << 3,

    /// <summary>
    /// <c>ac0</c> – Animated Camera Reset: disable animated camera at animation end.
    /// </summary>
    AnimatedCameraReset = 1u << 4,

    /// <summary>
    /// <c>bsa</c> – Animation file is part of a BSA archive (excluded from consistency check).
    /// </summary>
    BSA = 1u << 5,

    /// <summary>
    /// <c>h</c> – Headtracking ON (default is OFF).
    /// </summary>
    HeadTracking = 1u << 6,

    /// <summary>
    /// <c>k</c> – "Known" animation (vanilla or from another mod; excluded from consistency check).
    /// </summary>
    Known = 1u << 7,

    /// <summary>
    /// <c>md</c> – Motion is driven by actor AI instead of animation data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Motion driven" means motion comes from the actor's package or
    /// player input.
    /// </para>
    /// <para>
    /// "Animation driven" means motion comes from the animation's motion data.
    /// Most animations default to "animation driven", which disables AI movement.
    /// </para>
    /// <para>
    /// Use <c>-md</c> to keep AI movement active.
    /// </para>
    /// </remarks>
    MotionDriven = 1u << 8,

    /// <summary>
    /// <c>st</c> – Sticky AO. Animation Object will not be unequipped at the end of the animation.
    /// </summary>
    Sticky = 1u << 9,

    /// <summary>
    /// <c>Tn</c> – Character keeps position after a <c>-a</c> animation (no IdleForceDefaultState).
    /// </summary>
    TransitionNext = 1u << 10,
}
