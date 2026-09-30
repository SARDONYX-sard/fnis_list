# Span based High Performance FNIS List Parser

This project is an implementation candidate for integration into Pandora Behavior Engine+.

Based on the FNIS list parser in [`fnis_list`(Rust)](https://github.com/SARDONYX-sard/d-merge/tree/2.7.4/core/fnis_list).

## Progress

| Status | Feature                         | Abbreviation(s) |
| ------ | ------------------------------- | --------------- |
| [x]    | Basic                           | `b`             |
| [x]    | Sequenced Animations            | `s`, `so`       |
| [x]    | Arm Offset Animations           | `ofa`           |
| [x]    | Furniture Animations            | `fu`, `fuo`     |
| [x]    | Paired Animations and KillMoves | `pa`, `km`      |
| [x]    | Chair Animations                | `ch`            |
| [x]    | Alternate Animations            | `AAprefix`      |

## Example

```csharp
using System;
using fnis_list;

// string source = File.ReadAllText("FNIS_List.txt");
string source = """
Version 7.0

b Attack attack.hkx
MD 1.25 10 20 30
RD 1.25 45

b Walk walk.hkx
MD 2.0 0 10 0
RD 2.0 -30
""";

ReadOnlySpan<char> span = source.AsSpan();

FnisListReader reader = new(span);
FnisListParseResult<FnisPattern> result = reader.Parse();

Assert.True(result.IsSuccess);

FnisPattern pattern = result.Value;

string[] expectedEvents = ["Attack", "Walk"];
string[] expectedFiles = ["attack.hkx", "walk.hkx"];
float[][] expectedMotionData = [[1.25f, 10.0f, 20.0f, 30.0f], [2.0f, 0.0f, 10.0f, 0.0f]];
float[] expectedRotationAngles = [45.0f, -30.0f];

Assert.Equal(expectedEvents.Length, pattern.Animations.Count);

for (int i = 0; i < pattern.Animations.Count; i++)
{
    FnisAnimation animation = pattern.Animations[i];

    Assert.Equal(FnisAnimType.Basic, animation.Type);
    Assert.Equal(expectedEvents[i], animation.AnimEvent(span));
    Assert.Equal(expectedFiles[i], animation.AnimFile(span));


    Assert.Equal(1, animation.MotionDataCount);
    Assert.True(animation.TryGetMotionData(0, out FnisMotionData motion));
    Assert.Equal(expectedMotionData[i][0], motion.Time);
    Assert.Equal(expectedMotionData[i][1], motion.DeltaX);
    Assert.Equal(expectedMotionData[i][2], motion.DeltaY);
    Assert.Equal(expectedMotionData[i][3], motion.DeltaZ);

    Assert.Equal(1, animation.RotationDataCount);

    Assert.True(animation.TryGetRotationData(0, out FnisRotationData rotation));
    Assert.Equal(FnisRotationDataKind.DeltaZAngle, rotation.Kind);
    Assert.Equal(expectedRotationAngles[i], rotation.ZAngle);
}
```
