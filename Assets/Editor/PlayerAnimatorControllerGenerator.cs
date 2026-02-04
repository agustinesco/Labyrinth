using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class PlayerAnimatorControllerGenerator
{
    [MenuItem("Tools/Generate Player Animator Controller")]
    public static void GeneratePlayerAnimatorController()
    {
        // Create the animator controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(
            "Assets/Sprites/Animations/Player/PlayerAnimatorController.controller");

        // Add parameters
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Load animation clips
        var walkDown = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprites/Animations/Player/PlayerWalkingDown.anim");
        var walkUp = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprites/Animations/Player/PlayerWalkingUp.anim");
        var walkHorizontal = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Sprites/Animations/Player/PlayerWalkingHorizontally.anim");

        // Create states
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        var walkDownState = rootStateMachine.AddState("WalkDown", new Vector3(300, 100, 0));
        var walkUpState = rootStateMachine.AddState("WalkUp", new Vector3(300, -100, 0));
        var walkHorizontalState = rootStateMachine.AddState("WalkHorizontal", new Vector3(500, 0, 0));

        // Assign motion clips
        if (walkDown != null) walkDownState.motion = walkDown;
        if (walkUp != null) walkUpState.motion = walkUp;
        if (walkHorizontal != null) walkHorizontalState.motion = walkHorizontal;

        // Use walk down as idle (first frame)
        if (walkDown != null) idleState.motion = walkDown;

        // Set default state
        rootStateMachine.defaultState = idleState;

        // Create transitions from Idle
        // Idle -> WalkDown (when moving down)
        var idleToDown = idleState.AddTransition(walkDownState);
        idleToDown.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveY");
        idleToDown.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToDown.duration = 0;
        idleToDown.hasExitTime = false;

        // Idle -> WalkUp (when moving up)
        var idleToUp = idleState.AddTransition(walkUpState);
        idleToUp.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveY");
        idleToUp.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToUp.duration = 0;
        idleToUp.hasExitTime = false;

        // Idle -> WalkHorizontal (when moving horizontally)
        var idleToHorizontal = idleState.AddTransition(walkHorizontalState);
        idleToHorizontal.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveX");
        idleToHorizontal.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToHorizontal.duration = 0;
        idleToHorizontal.hasExitTime = false;

        var idleToHorizontalLeft = idleState.AddTransition(walkHorizontalState);
        idleToHorizontalLeft.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveX");
        idleToHorizontalLeft.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToHorizontalLeft.duration = 0;
        idleToHorizontalLeft.hasExitTime = false;

        // WalkDown -> Idle (when stopped)
        var downToIdle = walkDownState.AddTransition(idleState);
        downToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        downToIdle.duration = 0;
        downToIdle.hasExitTime = false;

        // WalkUp -> Idle (when stopped)
        var upToIdle = walkUpState.AddTransition(idleState);
        upToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        upToIdle.duration = 0;
        upToIdle.hasExitTime = false;

        // WalkHorizontal -> Idle (when stopped)
        var horizontalToIdle = walkHorizontalState.AddTransition(idleState);
        horizontalToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        horizontalToIdle.duration = 0;
        horizontalToIdle.hasExitTime = false;

        // Cross transitions between walk states
        // WalkDown -> WalkUp
        var downToUp = walkDownState.AddTransition(walkUpState);
        downToUp.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveY");
        downToUp.duration = 0;
        downToUp.hasExitTime = false;

        // WalkDown -> WalkHorizontal
        var downToHorizontal = walkDownState.AddTransition(walkHorizontalState);
        downToHorizontal.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveX");
        downToHorizontal.AddCondition(AnimatorConditionMode.Greater, -0.5f, "MoveY");
        downToHorizontal.AddCondition(AnimatorConditionMode.Less, 0.5f, "MoveY");
        downToHorizontal.duration = 0;
        downToHorizontal.hasExitTime = false;

        var downToHorizontalLeft = walkDownState.AddTransition(walkHorizontalState);
        downToHorizontalLeft.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveX");
        downToHorizontalLeft.AddCondition(AnimatorConditionMode.Greater, -0.5f, "MoveY");
        downToHorizontalLeft.AddCondition(AnimatorConditionMode.Less, 0.5f, "MoveY");
        downToHorizontalLeft.duration = 0;
        downToHorizontalLeft.hasExitTime = false;

        // WalkUp -> WalkDown
        var upToDown = walkUpState.AddTransition(walkDownState);
        upToDown.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveY");
        upToDown.duration = 0;
        upToDown.hasExitTime = false;

        // WalkUp -> WalkHorizontal
        var upToHorizontal = walkUpState.AddTransition(walkHorizontalState);
        upToHorizontal.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveX");
        upToHorizontal.AddCondition(AnimatorConditionMode.Greater, -0.5f, "MoveY");
        upToHorizontal.AddCondition(AnimatorConditionMode.Less, 0.5f, "MoveY");
        upToHorizontal.duration = 0;
        upToHorizontal.hasExitTime = false;

        var upToHorizontalLeft = walkUpState.AddTransition(walkHorizontalState);
        upToHorizontalLeft.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveX");
        upToHorizontalLeft.AddCondition(AnimatorConditionMode.Greater, -0.5f, "MoveY");
        upToHorizontalLeft.AddCondition(AnimatorConditionMode.Less, 0.5f, "MoveY");
        upToHorizontalLeft.duration = 0;
        upToHorizontalLeft.hasExitTime = false;

        // WalkHorizontal -> WalkUp
        var horizontalToUp = walkHorizontalState.AddTransition(walkUpState);
        horizontalToUp.AddCondition(AnimatorConditionMode.Greater, 0.5f, "MoveY");
        horizontalToUp.duration = 0;
        horizontalToUp.hasExitTime = false;

        // WalkHorizontal -> WalkDown
        var horizontalToDown = walkHorizontalState.AddTransition(walkDownState);
        horizontalToDown.AddCondition(AnimatorConditionMode.Less, -0.5f, "MoveY");
        horizontalToDown.duration = 0;
        horizontalToDown.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Debug.Log("Player Animator Controller created successfully!");
    }
}
