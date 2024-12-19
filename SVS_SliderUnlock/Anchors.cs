using System;
using System.IO;
using BepInEx.Logging;

namespace SVS_SliderUnlock;

internal class Anchors
{
    // CharacterCreation.HumanCustom.<>c__DisplayClass194_0$$<EntryUndo>b__4 + 0x1c1
    // check slider percentage?
    public readonly long EntryUndo_CheckSliderRatio;

    // CharacterCreation.HumanCustom.<>c__DisplayClass194_0$$<EntryUndo>b__4 + 0x1d0
    public readonly long EntryUndo_ClampSliderRatioToOne;

    // Character.HumanDataCheck$$IsFace + 0x1d8
    public readonly long IsFace_BranchLessThanZero;

    // Character.HumanDataCheck$$IsFace + 0x21b
    public readonly long IsFace_BranchLessThanZero2;

    // Character.HumanDataCheck$$IsBody + 0x178
    public readonly long IsBody_BranchLessThanZero;

    // Character.HumanDataCheck$$IsBody + 0x1bb
    public readonly long IsBody_BranchLessThanZero2;

    // called in Character.HumanDataCheck$$IsBody to check HumanDataBody fields
    // sample from v1.1.2:
    //     cVar5 = func_0x0001800126b0(0,(body->fields).areolaSize,0x3f800000);
    public readonly long IsBody_ValidateHumanDataBodyField;

    // ILLGames.Unity.AnimationKeyInfo.Controller$$GetInfo
    public readonly long AnimationKeyInfo_Controller_GetInfo;

    // called in ILLGames.Unity.AnimationKeyInfo.Controller$$GetInfo
    public readonly long GetInfo_DictGetItemWithKey;

    // called in ILLGames.Unity.AnimationKeyInfo.Controller$$GetInfo
    public readonly long GetInfo_ListGetItem;

    private static void AssertAddr(long expectedWithBase, long actualOffset)
    {
#if DEBUG && false
        // assert for SVS v1.1.2
        Debug.Assert(actualOffset == (expectedWithBase - 0x180000000));
#endif
    }

    public Anchors(ManualLogSource Log, UnmanagedMemoryStream stream, long searchBegin = 4096)
    {
        long Search(string pattern, long seekPos = -1, long assert = -1)
        {
            stream.Seek(seekPos < 0 ? searchBegin : seekPos, SeekOrigin.Begin);
            if (!new BytePattern(pattern).Search(stream, out long pos))
            {
                throw new Exception($"patten {pattern} not found");
            }

            if (assert > 0)
            {
                AssertAddr(assert, pos);
            }

            Log.LogDebug($"Found pattern in 0x{pos + 0x180000000:X}: {pattern}");
            return pos;
        }

        // 0x180727a11 of SVS v1.1.2(same for addresses below)
        EntryUndo_CheckSliderRatio = Search(
            "77 ?? f3 0f 10 0d ?? ?? ?? ?? 0f 2f f1 76 ?? 0f 28 f1 eb ?? 0f 57 f6 33 d2 0f 28 c6 e8",
            assert: 0x180727a11
        );

        // 0x180727a20
        EntryUndo_ClampSliderRatioToOne = EntryUndo_CheckSliderRatio + 0xf;
        AssertAddr(0x180727a20, EntryUndo_ClampSliderRatioToOne);

        // 0x180680518
        IsFace_BranchLessThanZero = Search(
            "7f ?? ?? 8b 46 38 ?? 89 75 7f ?? 8b 00 f6 80 32 01 00 00 01 75 ?? ?? 8b c8 e8 ?? ?? ?? ?? ?? 8d 55 7f ?? 8b c8 e8",
            assert: 0x180680518
        );

        // 0x18068055b
        IsFace_BranchLessThanZero2 = Search(
            "0f 85 ?? ?? ?? ?? ?? 85 ff 74 ?? ?? 8b 0d ?? ?? ?? ?? ?? 39 b1 e0 00 00 00 75 ?? e8",
            IsFace_BranchLessThanZero,
            assert: 0x18068055b
        );

        // 0x180681728
        IsBody_BranchLessThanZero = Search(
            "7f ?? ?? 8b 46 38 ?? 89 65 48 ?? 8b 00 f6 80 32 01 00 00 01 75 ?? ?? 8b c8 e8 ?? ?? ?? ?? ?? 8d 55 48",
            assert: 0x180681728
        );

        // 0x18068176b
        IsBody_BranchLessThanZero2 = Search(
            "0f 85 ?? ?? ?? ?? ?? 85 f6 74 ?? ?? 8b 0d ?? ?? ?? ?? ?? 39 a1 e0 00 00 00",
            IsBody_BranchLessThanZero,
            assert: 0x18068176b
        );

        // 0x1800126b0
        IsBody_ValidateHumanDataBodyField = Search(
            "40 53 ?? 83 ec 30 ?? 8b 1d ?? ?? ?? ?? 0f 29 74 ?? 20 0f 28 f0 f3 0f 11 4c ?? 48 f3 0f 11 54 ?? 50",
            assert: 0x1800126b0
        );

        // 0x182320d00
        AnimationKeyInfo_Controller_GetInfo = Search(
            "?? 8b c4 ?? 89 58 10 ?? 89 70 18 55 57 ?? 54 ?? 55 ?? 56 ?? 8d 68 c1 ?? 81 ec d0 00 00 00 80 3d ?? ?? ?? ?? 00",
            assert: 0x182320d00
        );

        // 0x182320e8c
        var call_GetInfo_DictGetItemWithKey = Search(
            "e8 ?? ?? ?? ?? 84 c0 0f 84 ?? ?? ?? ?? ?? 83 7c ?? 18 00 0f 86 ?? ?? ?? ?? ?? 80 7c ?? 20 00 0f 57 ff f3 ?? 0f 10 1d",
            AnimationKeyInfo_Controller_GetInfo,
            assert: 0x182320e8c
        );

        byte[] relAddrLe = new byte[4];
        stream.Seek(call_GetInfo_DictGetItemWithKey + 1, SeekOrigin.Begin);
        stream.Read(relAddrLe, 0, 4);

        if (!BitConverter.IsLittleEndian)
            Array.Reverse(relAddrLe);

        // 0x18025d5d0
        GetInfo_DictGetItemWithKey =
            call_GetInfo_DictGetItemWithKey + 5 + BitConverter.ToInt32(relAddrLe);
        AssertAddr(0x18025d5d0, GetInfo_DictGetItemWithKey);

        // 0x182320ed8
        var call_GetInfo_ListGetItem = Search(
            "e8 ?? ?? ?? ?? ?? 8b c8 ?? 85 c0 0f 84 ?? ?? ?? ?? f2 0f 10 41 14 8b 40 1c f2 0f 11 03 89 43 08 e9",
            call_GetInfo_DictGetItemWithKey,
            assert: 0x182320ed8
        );
        stream.Seek(call_GetInfo_ListGetItem + 1, SeekOrigin.Begin);
        stream.Read(relAddrLe, 0, 4);

        if (!BitConverter.IsLittleEndian)
            Array.Reverse(relAddrLe);

        // 0x180002f20
        GetInfo_ListGetItem = call_GetInfo_ListGetItem + 5 + BitConverter.ToInt32(relAddrLe);
        AssertAddr(0x180002f20, GetInfo_ListGetItem);
    }
}
