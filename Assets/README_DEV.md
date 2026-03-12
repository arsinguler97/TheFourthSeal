# TheFourthSeal Dev Notes

## Current Combat Slice

This project currently has a first-pass room combat loop implemented.

### Room flow
- `FloorScene` room selection stores the chosen `RoomTemplateSO` in `RunManager`.
- `RoomNode` can add a node-specific enemy count bonus through `additionalEnemyCount`.
- `RoomButton` passes the final enemy count override into `RunManager`.
- `RoomScene` uses `RoomGenerator` to generate:
  - player start: first row, random column
  - exit: last row, random column
  - reward: random row excluding first and last
  - enemies: reserved before lava/blocked placement
  - lava / blocked tiles after all reserved positions are chosen
- enemy spawns must not be within Manhattan distance 2 of player start.

### Room data
- `RoomTemplateSO`
  - `enemyCount`
  - `lavaTileCount`
  - `blockedTileCount`
- `RoomConfig`
  - `start`
  - `exit`
  - `reward`
  - `enemySpawns`
  - `lavaTiles`
  - `blockedTiles`

## Combat System

### Stats
Implemented combat stats:
- `Health`
- `Attack`
- `Defence`
- `Speed`
- `Strength`
- `Range`
- `ActionPoints`

Files:
- `Assets/Scripts/Combat/StatType.cs`
- `Assets/Scripts/Combat/StatBlockData.cs`
- `Assets/Scripts/Combat/RuntimeStatBlock.cs`
- `Assets/Scripts/Combat/StatModifierData.cs`

### Units
- `CombatUnit` is the base class for combat actors.
- `PlayerUnit` wraps the player runtime combat state.
- `EnemyUnit` wraps enemy runtime combat state.

Implemented behavior:
- attack damage is rolled as `1..Attack`, then `Strength` is added
- defence reduces incoming damage
- minimum damage taken is currently `1`
- initiative roll is `1d20 + speed`

Files:
- `Assets/Scripts/Combat/CombatUnit.cs`
- `Assets/Scripts/Combat/PlayerUnit.cs`
- `Assets/Scripts/Combat/EnemyUnit.cs`

### Turn order
- `TurnManager` must exist in `RoomScene`
- initiative is rolled after player/enemies are spawned
- living units are sorted by initiative
- turns advance through that ordered list

### Player turn rules
- player starts turn with `CurrentActionPoints = Stats.ActionPoints`
- each action can currently be used only once per turn
- `Move` can be used once
- `Attack` can be used once
- if both were used, or AP reaches 0, turn ends

### Action rules
- `Move` action:
  - costs 1 AP
  - enables move mode
  - allows movement up to `Speed` tiles total
  - movement is step-based
  - no diagonal movement
  - can use WASD / arrows
  - can also click only an adjacent tile
- `Attack` action:
  - costs 1 AP
  - enables attack mode
  - click enemy tile to attack
  - no diagonal attack
  - target must be in same row or column
  - range check uses `Range` stat
- `Skip` action:
  - implemented as an `ActionDefinitionSO`
  - ends current turn immediately

Files:
- `Assets/Scripts/Combat/ActionType.cs`
- `Assets/Scripts/Combat/ActionDefinitionSO.cs`
- `Assets/Scripts/Combat/TurnManager.cs`
- `Assets/Scripts/PlayerController.cs`

## Enemy AI

Current AI is intentionally simple.

On its turn, each enemy:
- checks if player is already in straight-line attack range
- if not, moves toward the player up to `Speed` steps
- movement is non-diagonal
- if the player becomes reachable in straight-line `Range`, attacks

AI currently lives in:
- `Assets/Scripts/Combat/CombatManager.cs`

There is not yet a separate `EnemyAIController`.

## Current Enemy Spawn Limitation

Right now, room generation supports multiple enemy instances but only from a single enemy prefab reference.

Current setup:
- `RoomGenerator` has one `enemyUnitPrefab`
- if a room spawns multiple enemies, they are all spawned from that same prefab
- this is good enough for the current first combat slice, but it is not the final intended setup

This means:
- multiple goblins can exist
- different enemy types are not yet selected during room generation

## Planned Enemy Type Expansion

Next step for enemy variety should be moving from a single prefab reference to enemy-specific data.

Recommended direction:
- add an `EnemyDefinitionSO`
- let each enemy definition carry:
  - display name
  - stats
  - visuals / portrait / card art
  - optional AI flavor or behavior type
  - prefab reference if needed

Then extend room data so each room can choose from more than one enemy type.

Possible future shapes:
- `RoomTemplateSO` contains `List<EnemyDefinitionSO> possibleEnemies`
- or a weighted list like `EnemySpawnEntry`

Example future weighted entry:
- enemy definition
- spawn weight

That would allow rooms like:
- mostly goblins
- occasional ranged enemy
- rare heavy enemy

Recommended future implementation order:
1. stabilize current combat loop
2. create `EnemyDefinitionSO`
3. let `RoomTemplateSO` reference possible enemy types
4. make `RoomGenerator` pick enemy type per spawn
5. later connect that to enemy card UI and enemy-specific behaviors

## RoomScene Setup Checklist

### Required scene objects
- `GridManager`
- `RoomGenerator`
- `CombatManager`
- `TurnManager`
- `Main Camera`
- UI canvas with action buttons

### Required prefabs/components
- player object/prefab:
  - `PlayerController`
  - `PlayerUnit`
  - `PlayerInput`
- enemy prefab:
  - `EnemyUnit`
  - visual renderer

### Required assignments
- `RoomGenerator.playerControllerPrefab`
- `RoomGenerator.enemyUnitPrefab`
- `TurnManager.moveActionDefinition`
- `TurnManager.attackActionDefinition`
- `TurnManager.skipActionDefinition`
- `GridManager` tile and tile-type references

### UI button bindings
- Move button -> `TurnManager.SelectMoveAction()`
- Attack button -> `TurnManager.SelectAttackAction()`
- Skip button -> `TurnManager.ExecuteSkipAction()`

## Useful Debug Logs

The current code logs:
- initiative rolls
- turn start
- move mode / attack mode selection
- attack rolled damage
- damage taken
- death
- invalid move / invalid attack click reasons

## Known Next Steps

Likely next implementation steps:
- make attack target selection more robust / clearer visually
- show current HP / AP / turn state in UI
- move enemy AI out of `CombatManager` into its own controller
- add player/enemy card UI
- add item/equipment/consumable systems on top of current combat/stat foundation
- hovering over enemies display their corresponding card on top of them
- inventory system