# High Performance FNIS List Parser

A proof-of-concept FNIS list parser using `ReadOnlySpan<char>` to minimize heap allocations.
Only `triggers` and `animObjects` require heap allocation.

This project is an implementation candidate for integration into Pandora Behavior Engine+.

Based on the FNIS list parser in [`fnis_list`(Rust)](https://github.com/SARDONYX-sard/d-merge/tree/2.7.3/core/fnis_list).

## Progress

| Status | Feature                         | Abbreviation(s) |
| ------ | ------------------------------- | --------------- |
| [x]    | Basic                           | `b`             |
| [x]    | Sequenced Animations            | `s`, `so`       |
| [ ]    | Arm Offset Animations           | `ofa`           |
| [ ]    | Furniture Animations            | `fu`, `fuo`     |
| [x]    | Paired Animations and KillMoves | `pa`, `km`      |
| [ ]    | Chair Animations                | `ch`            |
| [ ]    | Alternate Animations            | `AAprefix`      |
