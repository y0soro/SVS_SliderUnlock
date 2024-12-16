using Il2CppSystem.Collections.Generic;
using ILLGames.Unity.AnimationKeyInfo;
using UnityEngine;

namespace SVS_SliderUnlock;

internal class SliderMath
{
    public static Vector3 CalculateScale(List<Data> list, float rate)
    {
        Vector3 scl = list[0].Scl;
        Vector3 scl2 = list[list.Count - 1].Scl;
        return scl + (scl2 - scl) * rate;
    }

    public static Vector3 CalculatePosition(List<Data> list, float rate)
    {
        Vector3 pos = list[0].Pos;
        Vector3 pos2 = list[list.Count - 1].Pos;
        return pos + (pos2 - pos) * rate;
    }

    public static Vector3 CalculateRotation(List<Data> list, float rate)
    {
        Vector3 rot = list[0].Rot;
        Vector3 rot2 = list[1].Rot;
        Vector3 rot3 = list[list.Count - 1].Rot;
        Vector3 vector = rot2 - rot;
        Vector3 vector2 = rot3 - rot;
        bool flag = vector.x >= 0f;
        bool flag2 = vector.y >= 0f;
        bool flag3 = vector.z >= 0f;
        if (vector2.x > 0f && !flag)
        {
            vector2.x -= 360f;
        }
        else if (vector2.x < 0f && flag)
        {
            vector2.x += 360f;
        }
        if (vector2.y > 0f && !flag2)
        {
            vector2.y -= 360f;
        }
        else if (vector2.y < 0f && flag2)
        {
            vector2.y += 360f;
        }
        if (vector2.z > 0f && !flag3)
        {
            vector2.z -= 360f;
        }
        else if (vector2.z < 0f && flag3)
        {
            vector2.z += 360f;
        }
        if (rate < 0f)
        {
            return rot - vector2 * Mathf.Abs(rate);
        }
        return rot3 + vector2 * Mathf.Abs(rate - 1f);
    }

    public static Vector3 CalculateRotation(Vector3 rot1, Vector3 rot2, Vector3 rot3, float rate)
    {
        Vector3 vector = rot2 - rot1;
        Vector3 vector2 = rot3 - rot1;
        bool flag = vector.x >= 0f;
        bool flag2 = vector.y >= 0f;
        bool flag3 = vector.z >= 0f;
        if (vector2.x > 0f && !flag)
        {
            vector2.x -= 360f;
        }
        else if (vector2.x < 0f && flag)
        {
            vector2.x += 360f;
        }
        if (vector2.y > 0f && !flag2)
        {
            vector2.y -= 360f;
        }
        else if (vector2.y < 0f && flag2)
        {
            vector2.y += 360f;
        }
        if (vector2.z > 0f && !flag3)
        {
            vector2.z -= 360f;
        }
        else if (vector2.z < 0f && flag3)
        {
            vector2.z += 360f;
        }
        if (rate < 0f)
        {
            return rot1 - vector2 * Mathf.Abs(rate);
        }
        return rot3 + vector2 * Mathf.Abs(rate - 1f);
    }

    public static Vector3 SafeCalculateRotation(
        Vector3 original,
        string name,
        List<Data> list,
        float rate
    )
    {
        if (
            name.Contains("cf_s_Mune")
            || name.Contains("cf_s_Mouth")
            || name.Contains("cf_s_LegLow")
            || name.Contains("cf_s_MayuTip")
            || (name.Contains("thigh") && name.Contains("01"))
            || (name.StartsWith("cf_a_bust") && name.EndsWith("_size"))
        )
        {
            return original;
        }
        return CalculateRotation(list, rate);
    }

    public static float Lerp(float min, float max, float value)
    {
        return value * (max - min) + min;
    }

    public static float InverseLerp(float min, float max, float value)
    {
        return (value - min) / (max - min);
    }
}
