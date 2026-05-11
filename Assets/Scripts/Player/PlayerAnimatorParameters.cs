using UnityEngine;

public static class PlayerAnimatorParameters
{
    public static readonly int Walk = Animator.StringToHash("walk");
    public static readonly int Run = Animator.StringToHash("run");
    public static readonly int Midair = Animator.StringToHash("midair");
    public static readonly int Roll = Animator.StringToHash("roll");
    public static readonly int Moving = Animator.StringToHash("moving");
    public static readonly int Aim = Animator.StringToHash("aim");
    public static readonly int Draw = Animator.StringToHash("draw");
    public static readonly int StrafeForward = Animator.StringToHash("strafeForward");
    public static readonly int StrafeBack = Animator.StringToHash("strafeBack");
    public static readonly int StrafeLeft = Animator.StringToHash("strafeLeft");
    public static readonly int StrafeRight = Animator.StringToHash("strafeRight");
    public static readonly int Climb = Animator.StringToHash("climb");
    public static readonly int Crouch = Animator.StringToHash("crouch");
    public static readonly int Armor = Animator.StringToHash("armor");
    public static readonly int Slash = Animator.StringToHash("slash");
    public static readonly int Fight = Animator.StringToHash("fight");
    public static readonly int Parry = Animator.StringToHash("Parry");
    public static readonly int HammerParry = Animator.StringToHash("HammerParry");
    public static readonly int ShieldParry = Animator.StringToHash("shieldParry");

    public static bool TrySetBool(Animator animator, int hash, bool value)
    {
        if (animator == null)
        {
            return false;
        }

#if UNITY_EDITOR
        if (!HasParameter(animator, hash, AnimatorControllerParameterType.Bool))
        {
            return false;
        }
#endif

        animator.SetBool(hash, value);
        return true;
    }

#if UNITY_EDITOR
    static bool HasParameter(Animator animator, int hash, AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.nameHash == hash && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }
#endif
}
