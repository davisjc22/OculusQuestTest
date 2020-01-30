using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class OVRArmSwinger
{

    /***** CLASS VARIABLES *****/

    /** Inspector Variables **/

    // ReadMe Location
    [Tooltip("For your reference only, doesn't affect script operation in any way.")]
    public string GithubProjectAndDocs = "https://github.com/ElectricNightOwl/ArmSwinger";

    // General Settings
    [Header("General Settings")]
    [Tooltip("General - Scale World Units To Camera Rig Scale\n\nBy default, several unit- and speed-based settings are in absolute world units regardless of CameraRig scale.  If this setting is true, all of those settings will be automatically scaled to match the X scale of this CameraRig.  If you use a non-default CameraRig scale, enabling this setting will allow you to specify all settings in meters-per-second in relation to the CameraRig rather than in world units.\n\n(Default: false)")]
    public bool generalScaleWorldUnitsToCameraRigScale = false;
    [Tooltip("General - Auto Adjust Fixed Timestep\n\nIn order for ArmSwinger to handle movement and wall collisions correctly, Time.fixedDeltaTime must be 0.0111 (90 per second) or less.  If this feature is enabled, the setting will be adjusted automatically if it is higher than 0.0111.  If disabled, an error will be generated but the value will not be changed.\n\n(Default: true)")]
    public bool generalAutoAdjustFixedTimestep = true;

    // Arm Swing Settings
    [Header("Arm Swing Settings")]
    [Tooltip("Arm Swing - Navigation\n\nEnables variable locomotion using the controllers to determine speed and direction.  Activated according to the selected Mode. \n\n(Default: true)")]
    public bool armSwingNavigation = true;
    [SerializeField]
    [Tooltip("Arm Swing - Button\nOnly if Arm Swing Navigation is enabled\n\nDefines which controller button is used to activate ArmSwinger.  The button is the same on both controllers.\n\n(Default: Grip button)")]
    private ControllerButton _armSwingButton = ControllerButton.Grip;
    [SerializeField]
    [Tooltip("Arm Swing - Mode\nOnly if Arm Swing Navigation is enabled\n\nDetermines what is necessary to activate arm swing locomotion, and what controller is used when determining speed/direction.\n\nBoth Buttons Both Controllers - Activate by pushing both buttons on both controllers.  Both controllers are used for speed/direction.\n\nLeft Button Both Controllers - Activate by pushing the left controller button.  Both controllers are used for speed/direction.\n\nRight Button Both Controllers - Activate by pushing the right controller button.  Both controllers are used for speed/direction.\n\nOne Button Same Controller - Activate by pushing either controller's button.  That controller is used for speed/direction.  Can be combined with the other controller.\n\nOne Button Same Controller Exclusive - Activate by pushing either controller's button.  That controller is used for speed/direction.  Squeezing the button on the other controller will have no effect until the first controller button is released.\n\n(Default: One Button Same Controller)")]
    private ArmSwingMode _armSwingMode = ArmSwingMode.OneButtonSameController;
    [Tooltip("Arm Swing - Controller To Movement Curve\nOnly if Arm Swing Navigation is enabled.\n\nCurve that determines how much a given controller change translates into camera rig movement.  The far left of the curve is no controller movement and no virtual movement.  The far right is Controller Speed For Max Speed (controller movement) and Max Speed (virtual momvement).\n\n(Default: Linear)")]
    public AnimationCurve armSwingControllerToMovementCurve = new AnimationCurve(new Keyframe(0, 0, 1, 1), new Keyframe(1, 1, 1, 1));
    [SerializeField]
    [Tooltip("Arm Swing - Controller Speed For Max Speed\nOnly if Arm Swing Navigation is enabled\n\nThe number of CameraRig local units per second a controller needs to be moving to be considered going max speed.\n\n(Default:3)")]
    private float _armSwingControllerSpeedForMaxSpeed = 3f;
    [SerializeField]
    [Tooltip("Arm Swing - Max Speed\nOnly if Arm Swing Navigation is enabled\n\nThe fastest base speed (in world units) a player can travel when moving controllers at Controller Movement Per Second For Max Speed.  The actual max speed of the player will depend on the both/single controller coefficients you configure.\n\n(Default: 8)")]
    private float _armSwingMaxSpeed = 8;
    [Tooltip("Arm Swing - Both Controllers Coefficient\nOnly if Arm Swing Navigation is enabled and Swing Activation Mode allows both controllers to be used for arm swinging.\n\nUsed to boost or nerf the player's speed when using boths controllers for arm swinging.  A value of 1.0 will not modify the curve / max speed calculation.\n\n(Default: 1.0)")]
    [Range(0f, 10f)]
    [SerializeField]
    private float _armSwingBothControllersCoefficient = 1.0f;
    [Tooltip("Arm Swing - Single Controller Coefficient\nOnly if Arm Swing Navigation is enabled and Swing Activation Mode allows a single controller to be used for arm swinging.\n\nUsed to boost or nerf the player's speed when using a single controller for arm swinging.  A value of 1.0 will not modify the curve / max speed calculation.\n\n(Default:.7)")]
    [Range(0f, 10f)]
    [SerializeField]
    private float _armSwingSingleControllerCoefficient = .7f;

    // Controller Smoothing Settings
    [Header("Controller Smoothing Settings")]
    [Tooltip("Controller Smoothing\n\nUses controller movement sampling to help eliminate jerks and unpleasant movement when controllers suddenly change position due to tracking inaccuracies.  It is highly recommended to turn this setting on if using movingInertia\n\n(Default: true)")]
    public bool controllerSmoothing = true;
    [Tooltip("Controller Smoothing - Mode\nOnly if Controller Smoothing is enabled\n\nDetermines how controller smoothing calculates the smoothed movement value used by arm swinging.\n\nLowest\nUse the lowest value in the cache.  Should only be used with small cache sizes.\n\nAverage\nUse the average of all values in the cache.\n\nAverage Minus Highest\nUse the average of all values in the cache, but disregard the highest value.  When a controller jitters, the value change in that frame is almost always higher than normal values and will be discarded.\n\n(Default: Average Minus Highest)")]
    public ControllerSmoothingMode controllerSmoothingMode = ControllerSmoothingMode.AverageMinusHighest;
    [Range(2, 90)]
    [Tooltip("Controller Smoothing - Cache Size\nOnly if Controller Smoothing is enabled\n\nSets the number of calculated controller movements to keep in the cache.  Setting this number too low may allow a jittering controller to cause jerky movements for the player.  Setting this number too high increases lag time from controller movement to camera rig movement.\n\n(Default: 3)")]
    public int controllerSmoothingCacheSize = 3;

    // Inertia Settings
    [Header("Inertia Settings")]
    [SerializeField]
    [Tooltip("Moving Inertia\n\nSimulates inertia while arm swinging.  If the controllers change position slower than the moving inertia calculation, the inertia calculation will be used to determine forward movement.\n\n(Default: true)")]
    private bool _movingInertia = true;
    [Tooltip("Moving Inertia - Time To Stop At Max Speed\nOnly if Moving Inertia is enabled\n\nThe time it will take to go from armSwingMaxSpeed to 0 if arm swinging is engaged and the player does not move the controllers.  Speeds lower than armSwingMaxSpeed will scale their stopping time linearly.\n\n(Default: .5)")]
    public float movingInertiaTimeToStopAtMaxSpeed = .5f;
    [SerializeField]
    [Tooltip("Stopping Inertia\n\nSimulates inertia when arm swinging stops.\n\n(Default: true)")]
    private bool _stoppingInertia = true;
    [Tooltip("Stopping Inertia - Time To Stop At Max Speed\nOnly if Stopping Inertia is enabled\n\nThe time it will take to go from armSwingMaxSpeed to 0 when arm swinging is disengaged.  Speeds lower than armSwingMaxSpeed will scale their stopping time linearly.\n\n(Default:.35)")]
    public float stoppingInertiaTimeToStopAtMaxSpeed = .35f;

    // Raycast Settings
    [Header("Raycast Settings")]
    [Tooltip("Raycast - Ground Layer Mask\n\nLayers that ArmSwinger will consider 'the ground' when determining Y movement of the play space and when calculating angle-based prevention methods. \n\n(Default: Everything)")]
    public LayerMask raycastGroundLayerMask = -1;
    [SerializeField]
    [Tooltip("Raycast - Max Length\n\nThe length of the headset raycasts (in CameraRig local units) used for play height adjustment and falling/climbing prevention. Should be the value of the largest height difference you ever expect the player to come across.\n\n(Default: 100)")]
    private float _raycastMaxLength = 100f;
    [Range(2f, 90f)]
    [Tooltip("Raycast - Average Height Cache Size\n\nNumber of Raycasts to average together when determining where to place the play area.  These raycasts are done once per frame.  Lower numbers will make the play area moving feel more responsive.  Higher numbers will smooth out terrain bumps but may feel laggy.\n\n(Default: 3)")]
    [SerializeField]
    private int _raycastAverageHeightCacheSize = 3;
    [Tooltip("Raycast - Only Height Adjust While Arm Swinging\n\nWill prevent the camera rig height from being adjusted while the player is not Arm Swinging.  See the README on Github for more details.\n\n(Default:false)")]
    public bool raycastOnlyHeightAdjustWhileArmSwinging = false;

    // Prevent Wall Clip Settings
    [Header("Prevent Wall Clip Settings")]
    [Tooltip("Prevent Wall Clip\n\nPrevents players from putting their headset through walls and ground that are in the preventWallClipLayerMask list.\n\n(Default: true)")]
    [SerializeField]
    private bool _preventWallClip = true;
    [Tooltip("Prevent Wall Clip - Layer Mask\nOnly if Prevent Wall Clip is enabled\n\nLayers that ArmSwinger will consider 'walls' when determining if the headset has gone out of bounds.\n\n(Default: Everything)")]
    public LayerMask preventWallClipLayerMask = -1;
    [Tooltip("Prevent Wall Clip - Mode\nOnly if Prevent Wall Clip is enabled\n\nChanges how Prevent Wall Clip reacts when the player attempst to clip into a wall.\n\nRewind\nFade out, rewind rewindNumSavedPositionsToRewind postitions, fade back in.\n\nPush Back\nDo not allow the player to make the move.  Instead, adjust the position of the play area so that they cannot enter the wall.\n\n(Default: Push Back)")]
    public PreventionMode preventWallClipMode = PreventionMode.PushBack;
    [SerializeField]
    [Tooltip("Prevent Wall Clip - Headset Collider Radius\nOnly if Prevent Wall Clip is enabled\n\nSets the radius of the sphere collider used to detect the headset entering geometry.\n\n(Default: (.11f)")]
    private float _preventWallClipHeadsetColliderRadius = .11f;
    [Range(0f, 90f)]
    [Tooltip("Prevent Wall Clip - Min Angle To Trigger\nOnly if Prevent Wall Clip is enabled\n\nSets the minimum angle a \"wall\" should be in order to trigger Prevent Wall Clip if the headset collides with it.  0 is flat ground, 90 degree is a straight up wall.  This prevents rewinds from happening if the headset is placed on the physical floor and the headset collides with the virtual floor.\n\n(Default: 20)")]
    [SerializeField]
    private float _preventWallClipMinAngleToTrigger = 20f;

    // Prevent Climbing Settings
    [Header("Prevent Climbing Settings")]
    [Tooltip("Prevent Climbing\n\nPrevents the player from climbing walls and steep slopes.  \n\n(Default: true)")]
    public bool preventClimbing = true;
    [Range(0f, 90f)]
    [Tooltip("Prevent Climbing - Max Angle Player Can Climb\nOnly if Prevent Climbing is enabled\n\nThe maximum angle from the ground to the approached slope that a player can climb.  0 is flat ground, 90 is a vertical wall.  \n\n(Default: 45)")]
    [SerializeField]
    private float _preventClimbingMaxAnglePlayerCanClimb = 45f;

    // Prevent Falling Settings
    [Header("Prevent Falling Settings")]
    [Tooltip("Prevent Falling\n\nPrevents the player from falling down steep slopes.\n\n(Default: true)")]
    public bool preventFalling = true;
    [Range(0f, 90f)]
    [Tooltip("Prevent Falling - Max Angle Player Can Fall\nOnly if Prevent Falling is enabled\n\nThe maximum angle a player can try to descend.  0 is flat ground, 90 is a sheer cliff.  (Default: 60)")]
    [SerializeField]
    private float _preventFallingMaxAnglePlayerCanFall = 60f;

    // Prevent Wall Walking Settings
    [Header("Prevent Wall Walking Settings")]
    [Tooltip("Prevent Wall Walking\n\nPrevents the player from traversing across steep slopes.  Uses preventClimbingMaxAnglePlayerCanClimb when wall walking up, and preventClimbingMaxAnglePlayerCanClimb when wall walking down.\n\n(Default: true)")]
    public bool preventWallWalking = true;

    // Instant Height Settings
    [Header("Instant Height Settings")]
    [SerializeField]
    [Tooltip("Instant Height - Max Change\nOnly if Prevent Climbing / Falling or Only Height Adjust While Arm Swinging are enabled\n\nThe maximum height in world units a player can climb or descend in a single frame without triggering a rewind.  Allows climbing of steps this size or below, and prevents jumping over walls or falling off cliffs.  Also affects raycastOnlyHeightAdjustWhileArmSwinging.\n\n(Default: .2)")]
    private float _instantHeightMaxChange = .2f;
    [Tooltip("Instant Height - Climb Prevention Mode\nOnly if Prevent Climbing is enabled\n\nChanges how Prevent Climbing reacts when a player tries to instantly climb greater than instantHeightMaxChange.\n\nRewind\nFade out, rewind rewindNumSavedPositionsToRewind postitions, fade back in.\n\nPush Back\nDo not allow the player to make the move.  Instead, adjust the position of the play area so that they cannot climb the height.\n\n(Default: Push Back)")]
    public PreventionMode instantHeightClimbPreventionMode = PreventionMode.PushBack;
    [Tooltip("Instant Height - Fall Prevention Mode\nOnly if Prevent Falling is enabled\n\nChanges how Prevent Falling reacts when a player tried to instantly fall greater than instantHeightMaxChange.\n\nRewind\nFade out, rewind rewindNumSavedPositionsToRewind postitions, fade back in.\n\nPush Back\nDo not allow the player to make the move.  Instead, adjust the position of the play area so that they cannot fall down.\n\n(Default: Rewind)")]
    public PreventionMode instantHeightFallPreventionMode = PreventionMode.Rewind;

    // Check Settings
    [Header("Check Settings")]
    [Tooltip("Checks - Num Climb Fall Checks OOB Before Rewind\nOnly if Prevent Climbing / Falling is enabled\n\nThe number of checks in a row the player must be falling or climbing to trigger a rewind.  Checks are performed in sync with rewinds (rewindMinDistanceChangeToSavePosition).  Lower numbers will result in more false positives.  Higher numbers may allow the player to overcome the limits you set.\n\n(Default: 5)")]
    public int checksNumClimbFallChecksOOBBeforeRewind = 5;
    [Tooltip("Checks - Num Wall Walk Checks OOB Before Rewind\nOnly if Prevent Wall Walking is enabled\n\nThe number of checks in a row the player must be considered wall walking to trigger a rewind.  Checks are performed in sync with rewinds (rewindMinDistanceChangeToSavePosition).  Lower numbers will result in more false positives.  Higher numbers may allow the player to overcome the limits you set.\n\n(Default: 15)")]
    public int checksNumWallWalkChecksOOBBeforeRewind = 15;

    // Push Back Override Settings
    [Header("Push Back Override Settings")]
    [Tooltip("Push Back Override\nOnly if a Prevention method is using mode Push Back\n\nUses a token bucket system to determine if a player has been getting pushed back for too long.  Also helps players who have gotten stuck in geometry.  For more information, see the README file on GitHub.\n\n(Default: true)")]
    public bool pushBackOverride = true;
    [Tooltip("Push Back Override - Refill Per Sec\nOnly if Push Back Override is enabled\n\nThe amount of tokens that are added to the bucket every second.  The correct proportion of tokens are added each frame to add up to this number per second.\n\n(Default: 30)")]
    public float pushBackOverrideRefillPerSec = 30;
    [Tooltip("Push Back Override - Max Tokens\nOnly if Push Back Override is enabled\n\nThe maximum number of tokens in the bucket.  Additional tokens 'spill out' and are lost.\n\n(Default: 90")]
    public float pushBackOverrideMaxTokens = 90;

    // Rewind Settings
    [Header("Rewind Settings")]
    [SerializeField]
    [Tooltip("Rewind - Min Distance Change To Save Position\nOnly if a prevention method is enabled\n\nMinimum distance in world units that the player must travel to trigger another saved rewind position.\n\n(Default: .05)")]
    private float _rewindMinDistanceChangeToSavePosition = .05f;
    [Tooltip("Rewind - Dont Save Unsafe Climb Fall Positions\nOnly if both Prevent Climbing and Prevent Falling are enabled\n\nIf true, positions that can be climbed but not fallen down (or vice versa) won't be saved as rewind positions.  If false, the position will be saved anyways and the player might get stuck.\n\n(Default: true)")]
    public bool rewindDontSaveUnsafeClimbFallPositions = true;
    [Tooltip("Rewind - Dont Save Unsafe Wall Walk Positions\nOnly if Prevent Wall Walking is enabled\n\nIf true, positions that are considered wall walking but that haven't yet triggered a rewind won't be saved as possible rewind positions.  If false, the position will be saved anyways and the player might get stuck.\n\n(Default: true)")]
    public bool rewindDontSaveUnsafeWallWalkPositions = true;
    [Tooltip("Rewind - Num Positions To Store\nOnly if a prevention method is enabled\n\nThe number of saved positions to cache total.  Allows multiple consecutive rewinds to go even further back in time as necessary.  Must be higher than rewindNumSavedPositionsToRewind.\n\n(Default:28)")]
    public int rewindNumSavedPositionsToStore = 28;
    [Tooltip("Rewind - Num Positions To Rewind\nOnly if a prevention method is enabled\n\nThe number of saved positions to rewind when a player goes out of bounds and a rewind is triggered.\n\n(Default: 7)")]
    public int rewindNumSavedPositionsToRewind = 7;
    [Tooltip("Rewind - Fade Out Sec\nOnly if a prevention method is enabled\n\nTime in seconds to fade the player view OUT if a rewind is triggered.\n\n(Default: .15f)")]
    public float rewindFadeOutSec = .15f;
    [Tooltip("Rewind - Fade In Sec\nOnly if a prevention method is enabled\n\nTime in seconds to fade the player view IN once the player position is corrected.\n(Default: .35f)")]
    public float rewindFadeInSec = .35f;

    // Enums
    public enum ArmSwingMode
    {
        BothButtonsBothControllers,
        LeftButtonBothControllers,
        RightButtonBothControllers,
        OneButtonSameController,
        OneButtonSameControllerExclusive
    };

    public enum ControllerButton
    {
        Menu,
        Grip,
        TouchPad,
        Trigger
    };

    public enum PreventionReason { CLIMBING, FALLING, INSTANT_CLIMBING, INSTANT_FALLING, OHAWAS, HEADSET, WALLWALK, MANUAL, NO_GROUND, NONE };
    public enum PreventionMode { Rewind, PushBack };
    private enum PreventionCheckType { CLIMBFALL, WALLWALK };

    public enum ControllerSmoothingMode { Lowest, Average, AverageMinusHighest };

    // Informational public bools
    [HideInInspector]
    public bool outOfBounds = false;
    [HideInInspector]
    public bool rewindInProgress = false;
    [HideInInspector]
    public bool armSwinging = false;

    // Pause Variables
    [Header("Pause Variables")]
    [SerializeField]
    [Tooltip("Arm Swinging Paused\n\nPrevents the player from arm swinging while true.\n\n(Default: false)")]
    private bool _armSwingingPaused = false;
    [SerializeField]
    [Tooltip("Preventions Paused\n\nPauses all prevention methods (Climbing, Falling, Instant, Wall Clip, etc) while true.\n\n(Default: false)")]
    private bool _preventionsPaused = false;
    [SerializeField]
    [Tooltip("Angle Preventions Paused\n\nPauses all angle-based prevention methods (Climbing, Falling, Instant) while true.\n\n(Default: false)")]
    private bool _anglePreventionsPaused = false;
    [SerializeField]
    [Tooltip("Wall Clip Prevention Paused\n\nPauses wall clip prevention while true.\n\n(Default: false)")]
    private bool _wallClipPreventionPaused = false;
    [SerializeField]
    [Tooltip("Play Area Height Adjustment Paused\n\nPauses play area height adjustment unconditionally.  When this is changed from true to false, the play area will immediately be adjusted to the ground.\n\n(Default: false)")]
    private bool _playAreaHeightAdjustmentPaused = false;

    // Controller positions
    private Vector3 leftControllerLocalPosition;
    private Vector3 rightControllerLocalPosition;
    private Vector3 leftControllerPreviousLocalPosition;
    private Vector3 rightControllerPreviousLocalPosition;

    // Headset/Camera Rig saved position history
    private LinkedList<Vector3> previousHeadsetRewindPositions = new LinkedList<Vector3>();
    private Vector3 lastHeadsetRewindPositionSaved = new Vector3(0, 0, 0);
    private LinkedList<Vector3> previousCameraRigRewindPositions = new LinkedList<Vector3>();
    private Vector3 lastCameraRigRewindPositionSaved = new Vector3(0, 0, 0);
    private Vector3 previousAngleCheckHeadsetPosition;

    // Pushback positions
    private int previousPushbackPositionSize = 5;
    private LinkedList<Vector3> previousCameraRigPushBackPositions = new LinkedList<Vector3>();
    private LinkedList<Vector3> previousHeadsetPushBackPositions = new LinkedList<Vector3>();

    // RaycastHit histories
    private List<RaycastHit> headsetCenterRaycastHitHistoryPrevention = new List<RaycastHit>(); // History of headset center RaycastHits used for prevention checks
    private List<RaycastHit> headsetCenterRaycastHitHistoryHeight = new List<RaycastHit>(); // History of headset center RaycastHits used for height adjustments each frame
    private RaycastHit lastRaycastHitWhileArmSwinging; // The last every-frame headset center RaycastHit that was seen while the player was arm swinging

    // Prevention Reason histories
    private Queue<PreventionReason> climbFallPreventionReasonHistory = new Queue<PreventionReason>();
    private Queue<PreventionReason> wallWalkPreventionReasonHistory = new Queue<PreventionReason>();
    private PreventionReason _currentPreventionReason = PreventionReason.NONE;

    // Controller Movement Result History
    private LinkedList<float> controllerMovementResultHistory = new LinkedList<float>(); // The controller movement results after the Swing Mode calculations but before inertia and 1/2-hand coefficients

    // Saved angles
    private float latestCenterChangeAngle;
    private float latestSideChangeAngle;

    // Saved movement
    private float latestArtificialMovement;
    private Quaternion latestArtificialRotation;
    private float previousTimeDeltaTime = 0f;
    private Vector3 previousAngleCheckCameraRigPosition;

    // Inertia curves
    // WARNING: must be linear for now
    private AnimationCurve movingInertiaCurve = new AnimationCurve(new Keyframe(0, 1, -1, -1), new Keyframe(1, 0, -1, -1));
    private AnimationCurve stoppingInertiaCurve = new AnimationCurve(new Keyframe(0, 1, -1, -1), new Keyframe(1, 0, -1, -1));

    // Used for test scene only
    private bool _useNonLinearMovementCurve = true;
    private AnimationCurve inspectorCurve;

    //// Controller buttons ////
    //private Valve.VR.EVRButtonId steamVRArmSwingButton;
    private bool leftButtonPressed = false;
    private bool rightButtonPressed = false;

    //// Controllers ////
    //private GameObject leftControllerGameObject;
    //private GameObject rightControllerGameObject;
    //private SteamVR_TrackedObject leftControllerTrackedObj;
    //private SteamVR_TrackedObject rightControllerTrackedObj;
    //private SteamVR_Controller.Device leftController;
    //private SteamVR_Controller.Device rightController;
    private int leftControllerIndex;
    private int rightControllerIndex;

    // Wall Clip tracking
    [HideInInspector]
    public bool wallClipThisFrame = false;
    [HideInInspector]
    public bool rewindThisFrame = false;

    // Push back override
    private float pushBackOverrideValue;
    private bool pushBackOverrideActive = false;

    // One Button Same Controller Exclusive mode only
    private GameObject activeSwingController = null;

    //// Prevent Wall Clip's HeadsetCollider script
    //private HeadsetCollider headsetCollider;

    // GameObjects
    private GameObject headsetGameObject;
    private GameObject cameraRigGameObject;

    // Camera Rig scaling
    private float cameraRigScaleModifier = 1.0f;

    public OVRArmSwinger()
    {

    }

   public void updateControllerPosition(Vector3 leftControllerPosition, Vector3 rightControllerPosition)
    {
        leftControllerLocalPosition = leftControllerPosition;
        rightControllerLocalPosition = rightControllerPosition;
    }

    public void updatePreviousControllerPosition(Vector3 leftControllerPosition, Vector3 rightControllerPosition)
    {
        leftControllerPreviousLocalPosition = leftControllerPosition;
        rightControllerPreviousLocalPosition = rightControllerLocalPosition;
    }

    public Vector3 getLeftControllerPosition()
    {
        return leftControllerLocalPosition;
    }

    public Vector3 getRightControllerPosition()
    {
        return rightControllerLocalPosition;
    }



    /***** CORE FUNCTIONS *****/
    // Variable Arm Swing locomotion
    public Vector3 variableArmSwingMotion()
    {

        // Initialize movement variables
        float movementAmount = 0f;
        Quaternion movementRotation = Quaternion.identity;
        bool movedThisFrame = false;

        movedThisFrame = swingBothButtonsBothControllers(ref movementAmount, ref movementRotation);

        if (movedThisFrame)
        {
            return getForwardXZ(movementAmount, movementRotation);
        }
        else
        {
            return Vector3.zero;
        }
    }

    // Arm Swing when armSwingMode is BothButtonsBothControllers
   bool swingBothButtonsBothControllers(ref float movement, ref Quaternion rotation)
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger) && OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
        {
            // The rotation is the average of the two controllers
            rotation = determineAverageControllerRotation();

            // Find the change in controller position since last Update()
            float leftControllerChange = Vector3.Distance(leftControllerPreviousLocalPosition, leftControllerLocalPosition);
            float rightControllerChange = Vector3.Distance(rightControllerPreviousLocalPosition, rightControllerLocalPosition);

            // Calculate what camera rig movement the change should be converted to
            float leftMovement = calculateMovement(armSwingControllerToMovementCurve, leftControllerChange, armSwingControllerSpeedForMaxSpeed, armSwingMaxSpeed);
            float rightMovement = calculateMovement(armSwingControllerToMovementCurve, rightControllerChange, armSwingControllerSpeedForMaxSpeed, armSwingMaxSpeed);

            // Controller movement is the average of the two controllers' change times the both controllers coefficient
            float controllerMovement = (leftMovement + rightMovement) / 2 * armSwingBothControllersCoefficient;

            movement = controllerMovement;
            return true;
        }
        else
        {
            return false;
        }
    }

    /***** HELPER FUNCTIONS *****/

    // Returns the average of two Quaternions
    Quaternion averageRotation(Quaternion rot1, Quaternion rot2)
    {
        return Quaternion.Slerp(rot1, rot2, 0.5f);
    }

    public static Vector3 vector3XZOnly(Vector3 vec)
    {
        return new Vector3(vec.x, 0f, vec.z);
    }

    // Returns a forward vector given the distance and direction
    public static Vector3 getForwardXZ(float forwardDistance, Quaternion direction)
    {
        Vector3 forwardMovement = direction * Vector3.forward * forwardDistance;
        return vector3XZOnly(forwardMovement);
    }

    // Returns the average rotation of the two controllers
    Quaternion determineAverageControllerRotation()
    {
        // Build the average rotation of the controller(s)
        Quaternion newRotation;

        // Both controllers are present
        newRotation = averageRotation(OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));

        return newRotation;
    }

    static float calculateMovement(AnimationCurve curve, float change, float maxInput, float maxSpeed)
    {
        float changeInWUPS = change / Time.deltaTime;
        float movement = Mathf.Lerp(0, maxSpeed, curve.Evaluate(changeInWUPS / maxInput)) * Time.deltaTime;

        return movement;
    }

    /***** PUBLIC FUNCTIONS *****/
    // Moves the camera to another world position without a rewind
    // Also resets all caches and saved variables to prevent false OOB events
    // Allows other scripts and ArmSwinger mechanisms to artifically move the player without a rewind happening

    /***** GET SET *****/
    public float armSwingBothControllersCoefficient
    {
        get
        {
            return _armSwingBothControllersCoefficient;
        }

        set
        {
            float min = 0f;
            float max = 10f;

            if (value >= min && value <= max)
            {
                _armSwingBothControllersCoefficient = value;
            }
            else
            {
                Debug.LogWarning("ArmSwinger:armSwingBothControllersCoefficient:: Requested new value " + value + " is out of range (" + min + ".." + max + ")");
            }
        }

    }

    public float armSwingControllerSpeedForMaxSpeed
    {
        get
        {
            return _armSwingControllerSpeedForMaxSpeed;
        }
        set
        {
            _armSwingControllerSpeedForMaxSpeed = value;
        }
    }

    public float armSwingMaxSpeed
    {
        get
        {
            return _armSwingMaxSpeed * cameraRigScaleModifier;
        }
        set
        {
            _armSwingMaxSpeed = value;
        }
    }

}
