using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectEve.Core.Scene;

/// <summary>
/// Server-owned spatial / physical interaction engine for an active scene.
///
/// IMPORTANT:
/// - Distance is world truth, not descriptive flavor.
/// - 0 ft in UI means ACTIVE PHYSICAL CONTACT. The coordinate engine may keep
///   bodies about 1 ft apart internally so two actors do not occupy one point.
/// - An attempted hug/kiss/grab/strike is not automatically a completed action.
/// - Freeze/hesitation is never treated as acceptance.
/// - NPC behavior can create distance without telling the player why.
/// </summary>
public interface ISceneSpatialInteractionService
{
    Task<SceneSpatialTurnPreparation> PrepareActorTurnAsync(
        SceneSpatialTurnRequest request,
        CancellationToken cancellationToken = default);

    Task<SceneSpatialCueResult> ApplyNpcCueAsync(
        SceneSpatialCueRequest request,
        CancellationToken cancellationToken = default);

    Task<double> GetDisplayDistanceAsync(
        string sceneId,
        string observerCharacterKey,
        string otherCharacterKey,
        double physicalDistanceFeet,
        CancellationToken cancellationToken = default);

    Task<string> BuildActorSpatialContextAsync(
        string sceneId,
        string actorCharacterKey,
        CancellationToken cancellationToken = default);

    Task<ScenePairInteractionState?> GetPairStateAsync(
        string sceneId,
        string characterAKey,
        string characterBKey,
        CancellationToken cancellationToken = default);
}

public sealed class SceneSpatialTurnRequest
{
    public string SceneId { get; set; } = "";
    public string ActorCharacterKey { get; set; } = "";
    public string ActorName { get; set; } = "";
    public string ActionText { get; set; } = "";
    public string SpeechText { get; set; } = "";
    public string VoiceLevel { get; set; } = "normal";
    public IReadOnlyList<int> AddressedNpcIds { get; set; } = Array.Empty<int>();
}

public sealed class SceneSpatialTurnPreparation
{
    /// <summary>World-safe physical action text used by the scene transcript.</summary>
    public string ActionText { get; set; } = "";
    public string SpeechText { get; set; } = "";
    public string VoiceLevel { get; set; } = "normal";
    public IReadOnlyList<int> AddressedNpcIds { get; set; } = Array.Empty<int>();
    public IReadOnlyList<SceneSpatialEventSummary> SpatialEvents { get; set; }
        = Array.Empty<SceneSpatialEventSummary>();
}

public sealed class SceneSpatialCueRequest
{
    public string SceneId { get; set; } = "";
    public string ActorCharacterKey { get; set; } = "";
    public string ActorName { get; set; } = "";
    public string CueText { get; set; } = "";
    public string CueKind { get; set; } = "action"; // action | body_language
}

public sealed class SceneSpatialCueResult
{
    public bool ChangedWorldState { get; set; }
    public string ResolvedText { get; set; } = "";
    public SceneSpatialEventSummary? SpatialEvent { get; set; }
}

public sealed class SceneSpatialEventSummary
{
    public long Id { get; set; }
    public string SceneId { get; set; } = "";
    public string ActorCharacterKey { get; set; } = "";
    public string? TargetCharacterKey { get; set; }
    public string IntentKind { get; set; } = SceneSpatialIntentKinds.None;
    public string ContactKind { get; set; } = SceneContactKinds.None;
    public string Status { get; set; } = "observed";
    public double? PreviousDistanceFeet { get; set; }
    public double? NewDistanceFeet { get; set; }
    public string OriginalText { get; set; } = "";
    public string ResolvedText { get; set; } = "";
    public DateTimeOffset GameTime { get; set; }
}

public sealed class ScenePairInteractionState
{
    public string SceneId { get; set; } = "";
    public string CharacterAKey { get; set; } = "";
    public string CharacterBKey { get; set; } = "";
    public string InitiatorCharacterKey { get; set; } = "";
    public string ContactKind { get; set; } = SceneContactKinds.None;

    /// <summary>none | pending | active | hesitant | frozen | rejected | avoided | broken | completed</summary>
    public string State { get; set; } = "none";

    /// <summary>unknown | pending | welcomed | mutual | hesitant | frozen | withdrawn | refused | avoided | interrupted</summary>
    public string ReactionState { get; set; } = "unknown";

    public DateTimeOffset UpdatedGameTime { get; set; }
}

public static class SceneSpatialIntentKinds
{
    public const string None = "none";
    public const string Approach = "approach";
    public const string MoveCloser = "move_closer";
    public const string MoveAway = "move_away";
    public const string LeanIn = "lean_in";
    public const string LeanAway = "lean_away";
    public const string StandClose = "stand_close";
    public const string CrowdPersonalSpace = "crowd_personal_space";
    public const string Circle = "circle";
    public const string WalkPast = "walk_past";
    public const string Follow = "follow";
    public const string BlockPath = "block_path";
    public const string Corner = "corner";
    public const string SquareUp = "square_up";
    public const string HoldGround = "hold_ground";
    public const string Retreat = "retreat";
    public const string Flee = "flee";
    public const string ContactAttempt = "contact_attempt";
    public const string ContactAccept = "contact_accept";
    public const string ContactReject = "contact_reject";
    public const string ContactBreak = "contact_break";
    public const string Freeze = "freeze";
    public const string Hesitate = "hesitate";
}

/// <summary>
/// Extensible physical-contact vocabulary. These are world-state labels, not
/// permission or consent labels. Contact completion is tracked separately.
/// </summary>
public static class SceneContactKinds
{
    public const string None = "none";

    // Social / ordinary touch
    public const string HandTouch = "hand_touch";
    public const string TouchArm = "touch_arm";
    public const string TouchShoulder = "touch_shoulder";
    public const string TouchBack = "touch_back";
    public const string PatBack = "pat_back";
    public const string PatShoulder = "pat_shoulder";
    public const string Handshake = "handshake";
    public const string HighFive = "high_five";
    public const string FistBump = "fist_bump";
    public const string SideHug = "side_hug";
    public const string Hug = "hug";
    public const string LongHug = "long_hug";
    public const string Embrace = "embrace";
    public const string ArmAroundShoulders = "arm_around_shoulders";
    public const string ArmAroundWaist = "arm_around_waist";
    public const string HoldingHands = "holding_hands";
    public const string LinkArms = "link_arms";
    public const string HelpUp = "help_up";
    public const string SteadyPerson = "steady_person";
    public const string GuideByArm = "guide_by_arm";
    public const string DanceHold = "dance_hold";

    // Affection / romance
    public const string ForeheadTouch = "forehead_touch";
    public const string CheekKiss = "cheek_kiss";
    public const string ForeheadKiss = "forehead_kiss";
    public const string Kiss = "kiss";
    public const string LongKiss = "long_kiss";
    public const string MakeOut = "make_out";
    public const string Cuddle = "cuddle";
    public const string Spoon = "spoon";
    public const string SitOnLap = "sit_on_lap";
    public const string CaressFace = "caress_face";
    public const string CaressHair = "caress_hair";
    public const string StrokeBack = "stroke_back";
    public const string Nuzzle = "nuzzle";
    public const string EmbraceFromBehind = "embrace_from_behind";

    // Argument / intimidation / control
    public const string FingerPoint = "finger_point";
    public const string PokeChest = "poke_chest";
    public const string ShoulderBump = "shoulder_bump";
    public const string ChestBump = "chest_bump";
    public const string GetInFace = "get_in_face";
    public const string LoomOver = "loom_over";
    public const string BlockExit = "block_exit";
    public const string CornerPerson = "corner_person";
    public const string GrabClothing = "grab_clothing";
    public const string GrabArm = "grab_arm";
    public const string GrabWrist = "grab_wrist";
    public const string Shove = "shove";
    public const string Push = "push";
    public const string Pull = "pull";
    public const string Drag = "drag";

    // Striking
    public const string Slap = "slap";
    public const string Backhand = "backhand";
    public const string Punch = "punch";
    public const string Jab = "jab";
    public const string Cross = "cross";
    public const string Hook = "hook";
    public const string Uppercut = "uppercut";
    public const string Hammerfist = "hammerfist";
    public const string ElbowStrike = "elbow_strike";
    public const string ForearmStrike = "forearm_strike";
    public const string Headbutt = "headbutt";
    public const string KneeStrike = "knee_strike";
    public const string Kick = "kick";
    public const string FrontKick = "front_kick";
    public const string SideKick = "side_kick";
    public const string RoundKick = "round_kick";
    public const string Stomp = "stomp";

    // Grappling / restraint
    public const string Grab = "grab";
    public const string Grapple = "grapple";
    public const string FightClinch = "fight_clinch";
    public const string BodyLock = "body_lock";
    public const string BearHugRestraint = "bear_hug_restraint";
    public const string Wrestle = "wrestle";
    public const string Tackle = "tackle";
    public const string Trip = "trip";
    public const string Throw = "throw";
    public const string Takedown = "takedown";
    public const string GroundGrapple = "ground_grapple";
    public const string Mount = "mount";
    public const string Pin = "pin";
    public const string HoldDown = "hold_down";
    public const string ArmLock = "arm_lock";
    public const string WristLock = "wrist_lock";
    public const string JointLock = "joint_lock";
    public const string Chokehold = "chokehold";
    public const string Headlock = "headlock";
    public const string Restrain = "restrain";
    public const string DragToGround = "drag_to_ground";

    // Adult intimate physical-state vocabulary. The action label still does NOT
    // mean mutual participation; the pair state must separately become active.
    public const string IntimateTouch = "intimate_touch";
    public const string SexualTouch = "sexual_touch";
    public const string MutualTouch = "mutual_touch";
    public const string MakingOut = "making_out";
    public const string SexualEmbrace = "sexual_embrace";
    public const string SexualContact = "sexual_contact";
    public const string UndressingSelf = "undressing_self";
    public const string UndressingPartner = "undressing_partner";
    public const string ManualSex = "manual_sex";
    public const string OralSex = "oral_sex";
    public const string VaginalSex = "vaginal_sex";
    public const string AnalSex = "anal_sex";
    public const string MutualMasturbation = "mutual_masturbation";
    public const string SexualPositioning = "sexual_positioning";
    public const string Aftercare = "aftercare";

    public static readonly IReadOnlySet<string> AdultOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        IntimateTouch, SexualTouch, MutualTouch, MakingOut, SexualEmbrace,
        SexualContact, UndressingPartner, ManualSex, OralSex, VaginalSex,
        AnalSex, MutualMasturbation, SexualPositioning
    };
}

public static class SceneDistanceBands
{
    public static string FromFeet(double feet, bool activeContact = false)
    {
        if (activeContact) return "contact";
        if (feet <= 2.0) return "very_close";
        if (feet <= 5.0) return "conversation";
        if (feet <= 10.0) return "nearby";
        if (feet <= 20.0) return "across_room";
        return "distant";
    }
}
