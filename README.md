# SVS_SliderUnlock revived

The credit of SliderUnlock functionalities goes to the original author @Samsung Galaxy Note 10+. I am just doing porting stuff.

The SVS_SliderUnlock is now made portable by using byte pattern searches to locate patching anchors. However if this stop working for future version of SVS, the updates is not guaranteed unless it's just some simple byte pattern changes.

## Known Issue

- Issue: Eye material size can still only be adjusted in 0-100 range.
- Solution, TODO: remove all ratio clamping in callers of ILLGames.Unity.Animations.EyeLookMaterialControll$${ApplyPosition,ApplyPupilSize}

## COPYING

Source files SVS_SliderUnlock/Plugin.cs and SVS_SliderUnlock/SliderMath.cs are decompiled from SVS_SliderUnlock.dll binary, so the copyright belongs to the original author.

For other files the copyright belongs to me(@y0soro).

As I didn't own the decompiled source code and the license status of original source is unclear, this will be published as ALL-RIGHTS-RESERVED, use at your own risk!
