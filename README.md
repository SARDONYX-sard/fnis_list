# Span based High Performance FNIS List Parser

This project is an implementation candidate for integration into Pandora Behavior Engine+.

Based on the FNIS list parser in [`fnis_list`(Rust)](https://github.com/SARDONYX-sard/d-merge/tree/2.7.5/core/fnis_list).

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

The following example demonstrates parsing animation data.

See [Tests](FnisList.Tests/Parsing/FnisListReader.cs) for the executable version of this example.
