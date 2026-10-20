using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// All game controls in one place. Actions are created in code so nothing needs to be set up in the Inspector.
/// Keyboard + mouse and gamepad are both supported.
/// </summary>
public static class GameInput
{
    static InputAction move, attack, skill, interact, healthPotion, manaPotion, pause, map;

    public static Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
    public static bool AttackPressed => Pressed(attack);
    public static bool SkillPressed => Pressed(skill);
    public static bool InteractPressed => Pressed(interact);
    public static bool HealthPotionPressed => Pressed(healthPotion);
    public static bool ManaPotionPressed => Pressed(manaPotion);
    public static bool PausePressed => Pressed(pause);
    public static bool MapPressed => Pressed(map);

    /// <summary>True when the last attack/skill press came from the mouse, so we aim at the cursor.</summary>
    public static bool AttackUsedMouse => attack?.activeControl?.device is Mouse || skill?.activeControl?.device is Mouse;

    public static Vector2 MouseWorldPosition(Camera cam)
    {
        if (Mouse.current == null || cam == null) return Vector2.zero;
        return cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    static bool Pressed(InputAction action) => action != null && action.WasPressedThisFrame();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        DisposeAll(); // needed when "Enter Play Mode" skips domain reload

        move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");

        attack = Button("Attack", "<Mouse>/leftButton", "<Keyboard>/j", "<Gamepad>/buttonWest");
        skill = Button("Skill", "<Mouse>/rightButton", "<Keyboard>/k", "<Gamepad>/buttonNorth");
        interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
        healthPotion = Button("HealthPotion", "<Keyboard>/1", "<Gamepad>/leftShoulder");
        manaPotion = Button("ManaPotion", "<Keyboard>/2", "<Gamepad>/rightShoulder");
        pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
        map = Button("Map", "<Keyboard>/m", "<Keyboard>/tab", "<Gamepad>/select");

        foreach (var a in new[] { move, attack, skill, interact, healthPotion, manaPotion, pause, map })
            a.Enable();
    }

    static InputAction Button(string name, params string[] paths)
    {
        var action = new InputAction(name, InputActionType.Button);
        foreach (var path in paths) action.AddBinding(path);
        return action;
    }

    static void DisposeAll()
    {
        foreach (var a in new[] { move, attack, skill, interact, healthPotion, manaPotion, pause, map })
            a?.Dispose();
    }
}
