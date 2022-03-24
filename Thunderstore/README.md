# ror2-sotv-patcher

This patcher for BepInEx aims to automatically fix "simple" plugins for Risk of Rain 2, that were written before DLC1 came out.
It tries to point old mods to the new location of many things, but does not change of logic of that mod.

## Known NonFixable things
These things cannot be fixed automatically or require such significant effort (at least, at first glance) that I will not be writing code to fix them.
* Loading assets through addressables
* Using outdated R2API features (modules, serialible contentpacks(?))

## Known Issues:
Contributions to solve these issues are more than welcomed.
*  `[Assembly-CSharp]RoR2.Networking` was reworked in `[RoR2]RoR2.NetworkSystem`, there exists no current mapping to fix this.
*  `KinematicCharacterController` was moved to it's own DLL. There's no mapping to fix this yet.

This list is exhaustive of "known" issues, but others may indeed exist, please do let me know.
