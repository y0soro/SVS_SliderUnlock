using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Character;
using CharacterCreation;
using HarmonyLib;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SVS_SliderUnlock;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private static class NativeMethods
    {
        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000,
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 1,
            ReadOnly = 2,
            ReadWrite = 4,
            WriteCopy = 8,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400,
        }

        public static uint PAGE_EXECUTE_READWRITE = 64u;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualProtect(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            AllocationType lAllocationType,
            MemoryProtection flProtect
        );
    }

    internal static new ManualLogSource Log;

    private static IntPtr GetInfo_o;

    private static IntPtr dictionary_get_item_by_key;

    private static IntPtr list_get_item;

    internal static Harmony Harmony { get; } = new Harmony("SVS_SliderUnlock");

    public static ConfigEntry<int> Minimum { get; private set; }

    public static ConfigEntry<int> Maximum { get; private set; }

    public override unsafe void Load()
    {
        Log = base.Log;
        Minimum = base.Config.Bind(
            "Slider Limits",
            "Minimum slider value",
            -100,
            new ConfigDescription(
                "Changes will take effect next time the editor is loaded or a character is loaded.",
                new AcceptableValueRange<int>(-500, 0)
            )
        );
        Maximum = base.Config.Bind(
            "Slider Limits",
            "Maximum slider value",
            200,
            new ConfigDescription(
                "Changes will take effect next time the editor is loaded or a character is loaded.",
                new AcceptableValueRange<int>(100, 500)
            )
        );
        ManualLogSource log = Log;
        BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new(
            18,
            1,
            out var isEnabled
        );
        if (isEnabled)
        {
            bepInExInfoLogInterpolatedStringHandler.AppendLiteral("Plugin ");
            bepInExInfoLogInterpolatedStringHandler.AppendFormatted("SVS_SliderUnlock");
            bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" is loaded!");
        }
        log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
        Harmony.PatchAll(typeof(Plugin));
        ProcessModule processModule;
        try
        {
            processModule = ((IEnumerable)Process.GetCurrentProcess().Modules)
                .Cast<ProcessModule>()
                .First((ProcessModule x) => x.ModuleName == "GameAssembly.dll");
        }
        catch (Exception t)
        {
            ManualLogSource log2 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                34,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "Failed to find GameAssembly.dll - "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
            }
            log2.LogError(bepInExErrorLogInterpolatedStringHandler);
            return;
        }
        IntPtr intPtr = processModule.BaseAddress;
        int num = processModule.ModuleMemorySize;
        using UnmanagedMemoryStream unmanagedMemoryStream = new(
            (byte*)(void*)intPtr,
            num,
            num,
            FileAccess.ReadWrite
        );

        var anchors = new Anchors(Log, unmanagedMemoryStream, 4096);

        byte[] array = [144, 144, 144];
        unmanagedMemoryStream.Seek(anchors.EntryUndo_CheckSliderRatio, SeekOrigin.Begin);
        IntPtr lpAddress = (IntPtr)unmanagedMemoryStream.PositionPointer;
        uint lpflOldProtect2;
        if (
            !NativeMethods.VirtualProtect(
                lpAddress,
                (UIntPtr)(ulong)array.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE,
                out var lpflOldProtect
            )
        )
        {
            ManualLogSource log3 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                129,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "[CharacterCreation.HumanCustom.__c__DisplayClass194_0._EntryUndo_b__4] Failed to change memory protection, aborting. Error code: "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(
                    Marshal.GetLastWin32Error()
                );
            }
            log3.LogError(bepInExErrorLogInterpolatedStringHandler);
        }
        else
        {
            unmanagedMemoryStream.Write(array, 0, 2);
            unmanagedMemoryStream.Seek(anchors.EntryUndo_ClampSliderRatioToOne, SeekOrigin.Begin);
            unmanagedMemoryStream.Write(array, 0, 3);
            NativeMethods.VirtualProtect(
                lpAddress,
                (UIntPtr)(ulong)array.Length,
                lpflOldProtect,
                out lpflOldProtect2
            );
            ManualLogSource log4 = Log;
            bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(
                79,
                0,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExInfoLogInterpolatedStringHandler.AppendLiteral(
                    "[CharacterCreation.HumanCustom.__c__DisplayClass194_0._EntryUndo_b__4] Success!"
                );
            }
            log4.LogInfo(bepInExInfoLogInterpolatedStringHandler);
        }
        byte[] array2 = [144, 144];
        unmanagedMemoryStream.Seek(anchors.IsFace_BranchLessThanZero, SeekOrigin.Begin);
        IntPtr lpAddress2 = (IntPtr)unmanagedMemoryStream.PositionPointer;
        if (
            !NativeMethods.VirtualProtect(
                lpAddress2,
                (UIntPtr)(ulong)array2.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE,
                out var lpflOldProtect3
            )
        )
        {
            ManualLogSource log5 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                67,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "[IsFace] Failed to change memory protection, aborting. Error code: "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(
                    Marshal.GetLastWin32Error()
                );
            }
            log5.LogError(bepInExErrorLogInterpolatedStringHandler);
        }
        else
        {
            unmanagedMemoryStream.Write(array2, 0, array2.Length);
            byte[] array3 = [144, 233];
            unmanagedMemoryStream.Seek(anchors.IsFace_BranchLessThanZero2, SeekOrigin.Begin);
            unmanagedMemoryStream.Write(array3, 0, array3.Length);
            NativeMethods.VirtualProtect(
                lpAddress2,
                (UIntPtr)(ulong)array2.Length,
                lpflOldProtect3,
                out lpflOldProtect2
            );
            ManualLogSource log6 = Log;
            bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(
                17,
                0,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[IsFace] Success!");
            }
            log6.LogInfo(bepInExInfoLogInterpolatedStringHandler);
        }
        byte[] array4 = [144, 144];
        unmanagedMemoryStream.Seek(anchors.IsBody_BranchLessThanZero, SeekOrigin.Begin);
        IntPtr lpAddress3 = (IntPtr)unmanagedMemoryStream.PositionPointer;
        if (
            !NativeMethods.VirtualProtect(
                lpAddress3,
                (UIntPtr)(ulong)array4.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE,
                out var lpflOldProtect4
            )
        )
        {
            ManualLogSource log7 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                67,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "[IsFace] Failed to change memory protection, aborting. Error code: "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(
                    Marshal.GetLastWin32Error()
                );
            }
            log7.LogError(bepInExErrorLogInterpolatedStringHandler);
        }
        else
        {
            unmanagedMemoryStream.Write(array4, 0, array4.Length);
            byte[] array5 = [144, 233];
            unmanagedMemoryStream.Seek(anchors.IsBody_BranchLessThanZero2, SeekOrigin.Begin);
            unmanagedMemoryStream.Write(array5, 0, array5.Length);
            NativeMethods.VirtualProtect(
                lpAddress3,
                (UIntPtr)(ulong)array4.Length,
                lpflOldProtect4,
                out lpflOldProtect2
            );
            ManualLogSource log8 = Log;
            bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(
                17,
                0,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[IsFace] Success!");
            }
            log8.LogInfo(bepInExInfoLogInterpolatedStringHandler);
        }

        // mov    eax,0x1
        // ret
        // ; return 1
        byte[] array6 = [184, 1, 0, 0, 0, 195];

        unmanagedMemoryStream.Seek(anchors.IsBody_ValidateHumanDataBodyField, SeekOrigin.Begin);
        IntPtr lpAddress4 = (IntPtr)unmanagedMemoryStream.PositionPointer;
        if (
            !NativeMethods.VirtualProtect(
                lpAddress4,
                (UIntPtr)(ulong)array6.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE,
                out var lpflOldProtect5
            )
        )
        {
            ManualLogSource log9 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                74,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "[sub_180010BC0] Failed to change memory protection, aborting. Error code: "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(
                    Marshal.GetLastWin32Error()
                );
            }
            log9.LogError(bepInExErrorLogInterpolatedStringHandler);
        }
        else
        {
            unmanagedMemoryStream.Write(array6, 0, array6.Length);
            NativeMethods.VirtualProtect(
                lpAddress4,
                (UIntPtr)(ulong)array6.Length,
                lpflOldProtect5,
                out lpflOldProtect2
            );
            ManualLogSource log10 = Log;
            bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(
                24,
                0,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[sub_180010BC0] Success!");
            }
            log10.LogInfo(bepInExInfoLogInterpolatedStringHandler);
        }

        dictionary_get_item_by_key = intPtr + (int)anchors.GetInfo_DictGetItemWithKey;
        list_get_item = intPtr + (int)anchors.GetInfo_ListGetItem;
        byte[] array7 = [255, 37, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        IntPtr intPtr2 = (IntPtr)
            (delegate* unmanaged<IntPtr, IntPtr, float, IntPtr, IntPtr, IntPtr, IntPtr, byte>)(
                &GetInfo_hk
            );
        for (int i = 0; i < 8; i++)
        {
            array7[6 + i] = (byte)((intPtr2.ToInt64() >> i * 8) & 0xFF);
        }
        unmanagedMemoryStream.Seek(anchors.AnimationKeyInfo_Controller_GetInfo, SeekOrigin.Begin);
        IntPtr intPtr3 = (IntPtr)unmanagedMemoryStream.PositionPointer;
        if (
            !NativeMethods.VirtualProtect(
                intPtr3,
                (UIntPtr)(ulong)array7.Length,
                NativeMethods.PAGE_EXECUTE_READWRITE,
                out var lpflOldProtect6
            )
        )
        {
            ManualLogSource log11 = Log;
            BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new(
                71,
                1,
                out isEnabled
            );
            if (isEnabled)
            {
                bepInExErrorLogInterpolatedStringHandler.AppendLiteral(
                    "[GetInfo_hk] Failed to change memory protection, aborting. Error code: "
                );
                bepInExErrorLogInterpolatedStringHandler.AppendFormatted(
                    Marshal.GetLastWin32Error()
                );
            }
            log11.LogError(bepInExErrorLogInterpolatedStringHandler);
            return;
        }
        GetInfo_o = NativeMethods.VirtualAlloc(
            IntPtr.Zero,
            (UIntPtr)4096uL,
            NativeMethods.AllocationType.Commit | NativeMethods.AllocationType.Reserve,
            NativeMethods.MemoryProtection.ExecuteReadWrite
        );
        byte[] array8 = [255, 37, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        IntPtr intPtr4 = intPtr3 + 15;
        for (int j = 0; j < 8; j++)
        {
            array8[6 + j] = (byte)((intPtr4.ToInt64() >> j * 8) & 0xFF);
        }
        using UnmanagedMemoryStream unmanagedMemoryStream2 = new(
            (byte*)(void*)GetInfo_o,
            4096L,
            4096L,
            FileAccess.ReadWrite
        );
        byte[] buffer = new byte[15];
        // save first 14 bytes to jumper for original function
        unmanagedMemoryStream.Read(buffer, 0, 15);
        unmanagedMemoryStream2.Write(buffer, 0, 15);
        unmanagedMemoryStream2.Seek(15L, SeekOrigin.Begin);
        // JMP to unmodified ILLGames.Unity.AnimationKeyInfo.Controller$$GetInfo+0x0f
        unmanagedMemoryStream2.Write(array8, 0, array8.Length);

        unmanagedMemoryStream.Seek(anchors.AnimationKeyInfo_Controller_GetInfo, SeekOrigin.Begin);
        // modify first 14 bytes for jumping to hook function
        unmanagedMemoryStream.Write(array7, 0, array7.Length);
        NativeMethods.VirtualProtect(
            intPtr3,
            (UIntPtr)(ulong)array7.Length,
            lpflOldProtect6,
            out lpflOldProtect2
        );
        ManualLogSource log12 = Log;
        bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(
            21,
            0,
            out isEnabled
        );
        if (isEnabled)
        {
            bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[GetInfo_hk] Success!");
        }
        log12.LogInfo(bepInExInfoLogInterpolatedStringHandler);
    }

    [UnmanagedCallersOnly]
    private static unsafe byte GetInfo_hk(
        IntPtr a1,
        IntPtr name,
        float rate,
        IntPtr a4,
        IntPtr a5,
        IntPtr a6,
        IntPtr flags
    )
    {
        float num = rate;
        if (rate > 1f)
        {
            num = 1f;
        }
        else if (rate < 0f)
        {
            num = 0f;
        }
        byte b = (
            (delegate* unmanaged<IntPtr, IntPtr, float, IntPtr, IntPtr, IntPtr, IntPtr, byte>)
                (void*)GetInfo_o
        )(a1, name, num, a4, a5, a6, flags);
        if (b != 0 && (rate < 0f || rate > 1f))
        {
            IntPtr intPtr = *(IntPtr*)(void*)(a1 + 16);
            IntPtr intPtr2 = default(IntPtr);
            if (
                (
                    (delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr*, byte>)
                        (void*)dictionary_get_item_by_key
                )(IntPtr.Zero, IntPtr.Zero, intPtr, name, &intPtr2) != 0
            )
            {
                delegate* unmanaged<IntPtr, ulong, IntPtr, IntPtr> delegate_002A =
                    (delegate* unmanaged<IntPtr, ulong, IntPtr, IntPtr>)list_get_item.ToPointer();
                ulong num2 = *(ulong*)(void*)(intPtr2 + 24);
                IntPtr intPtr3 = delegate_002A(intPtr2, 0uL, IntPtr.Zero);
                IntPtr intPtr4 = delegate_002A(intPtr2, num2 - 1, IntPtr.Zero);
                bool flag = *(byte*)(void*)(flags + 32) != 0;
                bool flag2 = *(byte*)(void*)(flags + 33) != 0;
                bool flag3 = *(byte*)(void*)(flags + 34) != 0;
                if (flag)
                {
                    float* ptr = (float*)(void*)(intPtr3 + 20);
                    float* ptr2 = (float*)(void*)(intPtr4 + 20);
                    Vector3 vector = new(*ptr, ptr[1], ptr[2]);
                    Vector3 vector2 = new(*ptr2, ptr2[1], ptr2[2]);
                    Vector3 vector3 = vector + (vector2 - vector) * rate;
                    float* ptr3 = (float*)(void*)a4;
                    *ptr3 = vector3.x;
                    ptr3[1] = vector3.y;
                    ptr3[2] = vector3.z;
                }
                if (flag2)
                {
                    uint len = *(uint*)(void*)(name + 16);
                    IntPtr ptr4 = name + 20;
                    string text = Marshal.PtrToStringUni(ptr4, (int)len);
                    if (
                        (!text.Contains("thigh") || !text.Contains("01"))
                        && (!text.StartsWith("cf_a_bust") || !text.EndsWith("_size"))
                    )
                    {
                        float* ptr5 = (float*)(void*)(intPtr3 + 32);
                        float* ptr6 = (float*)(void*)(intPtr4 + 32);
                        IntPtr intPtr5 = delegate_002A(intPtr2, 1uL, IntPtr.Zero);
                        float* ptr7 = (float*)(void*)(intPtr5 + 32);
                        Vector3 rot = new(*ptr5, ptr5[1], ptr5[2]);
                        Vector3 rot2 = new(*ptr6, ptr6[1], ptr6[2]);
                        Vector3 rot3 = new(*ptr7, ptr7[1], ptr7[2]);
                        Vector3 vector4 = SliderMath.CalculateRotation(rot, rot3, rot2, rate);
                        float* ptr8 = (float*)(void*)a5;
                        *ptr8 = vector4.x;
                        ptr8[1] = vector4.y;
                        ptr8[2] = vector4.z;
                    }
                }
                if (flag3)
                {
                    float* ptr9 = (float*)(void*)(intPtr3 + 44);
                    float* ptr10 = (float*)(void*)(intPtr4 + 44);
                    Vector3 vector5 = new(*ptr9, ptr9[1], ptr9[2]);
                    Vector3 vector6 = new(*ptr10, ptr10[1], ptr10[2]);
                    Vector3 vector7 = vector5 + (vector6 - vector5) * rate;
                    float* ptr11 = (float*)(void*)a6;
                    *ptr11 = vector7.x;
                    ptr11[1] = vector7.y;
                    ptr11[2] = vector7.z;
                }
            }
        }
        return b;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HumanCustom), "ConvertTextFromRate")]
    private static void ConvertTextFromRateHook(ref string __result, int min, int max, float value)
    {
        if (min == 0 && max == 100)
        {
            ConvertTextFromRate01Hook(ref __result, value);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HumanCustom), "ConvertTextFromRate")]
    private static void ConvertTextFromRate01Hook(ref string __result, float value)
    {
        __result = Math.Round(100f * value).ToString(CultureInfo.InvariantCulture);
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(HumanCustom),
        "ConvertRateFromText",
        [typeof(int), typeof(int), typeof(string)]
    )]
    private static void ConvertRateFromTextHook(ref float __result, int min, int max, string buf)
    {
        if (min == 0 && max == 100)
        {
            ConvertRateFromTextHook_1a(ref __result, buf);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HumanCustom), "ConvertRateFromText", [typeof(string)])]
    private static void ConvertRateFromTextHook_1a(ref float __result, string buf)
    {
        float result;
        if (buf == null || buf == "")
        {
            __result = 0f;
        }
        else if (!float.TryParse(buf, out result))
        {
            __result = 0f;
        }
        else
        {
            __result = result / 100f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(HumanCustom),
        "EntryUndo",
        [
            typeof(HumanCustom.IInputSlider),
            typeof(Il2CppSystem.Func<float>),
            typeof(Il2CppSystem.Func<float, bool>),
            typeof(CompositeDisposable),
        ]
    )]
    private static void EntryUndo_slider_hook(HumanCustom.IInputSlider pack)
    {
        Slider slider = pack.Slider;
        if (slider.maxValue == 1f && slider.minValue == 0f)
        {
            slider.maxValue = (float)Maximum.Value / 100f;
            slider.minValue = (float)Minimum.Value / 100f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(HumanCustom),
        "EntryUndo",
        [
            typeof(HumanCustom.IInputSliderButton),
            typeof(Il2CppSystem.Func<float>),
            typeof(Il2CppSystem.Func<float, bool>),
            typeof(Il2CppSystem.Func<HumanData, float>),
            typeof(CompositeDisposable),
        ]
    )]
    private static void EntryUndo_sliderbutton_hook(HumanCustom.IInputSliderButton pack)
    {
        HumanCustom.IInputSlider inputSlider = new(pack.Pointer);
        Slider slider = inputSlider.Slider;
        if (slider.maxValue == 1f && slider.minValue == 0f)
        {
            slider.maxValue = (float)Maximum.Value / 100f;
            slider.minValue = (float)Minimum.Value / 100f;
        }
    }
}
