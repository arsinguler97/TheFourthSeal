# TheFourthSeal Dev Notes

## Project State

This project currently has a working floor-to-room combat loop in Unity.

Implemented at a usable prototype level:
- floor map selection
- room generation
- turn-based combat
- player and enemy stat systems
- equipment and consumables
- reward tiles
- miniboss / boss support
- enemy hover cards
- player stat card
- item cards in inventory
- wallet / gold gain from enemy kills
- ranged weapon and ranged enemy projectile support

Still planned / not finished:
- full shop flow
- cursed item flow
- multi-floor progression with key + exit gate
- more enemy and item variety
- more cleanup of scene/editor setup

---

## Core Run Flow

### Current flow
- `RunManager` is the persistent run-state singleton.
- `FloorScene` is used to pick the next room.
- entering a room loads `RoomScene`.
- `RoomScene` generates a combat room from current run state.
- clearing the room returns the player to `FloorScene`.
- player death resets the run and returns to the start flow.

### Current persistent state in `RunManager`
- selected room template
- selected enemy count override
- current floor node id
- pending floor node id
- cleared floor nodes
- current room config
- player health carried between rooms

---

## Floor Map

### Current behavior
- `FloorMapController` rebuilds the floor map UI on `FloorScene`.
- `FloorMapPlayerUI` moves the player marker to the current node.
- `RoomNode` stores connectivity between nodes.
- `RoomButton` should route room entry through `FloorMapController`.
- cleared rooms are tracked in run state.

### Planned update
- cleared rooms should remain non-enterable
- but the player marker should still be able to move across cleared nodes on the floor map

---

## Room Generation

### Current room generation
`RoomScene` currently generates:
- player start tile
- exit tile
- reward tile
- enemy spawn positions
- lava tiles
- blocked tiles

### `RoomConfig`
Current room config data includes:
- `start`
- `exit`
- `reward`
- `enemySpawns`
- `enemyDefinitions`
- `lavaTiles`
- `blockedTiles`
- `isRewardOpened`

### `RoomTemplateSO`
Room templates currently support:
- `enemyCount`
- `lavaTileCount`
- `blockedTileCount`
- `possibleEnemies`
- optional `bossEnemy`

### Current special room behavior
- if `bossEnemy` is assigned:
  - that boss always spawns
  - that boss spawns only once
  - remaining enemy slots are filled from normal room enemy pool

### Node-specific enemy overrides
- `RoomNode` can override enemy pool per node
- if node overrides are set, they are used first
- otherwise the room template enemy pool is used

---

## Combat Stats

Implemented combat stats:
- `Health`
- `Attack`
- `Defence`
- `Speed`
- `Strength`
- `Range`
- `ActionPoints`

### Current damage model
- attack roll is `1..Attack`
- strength is added after the roll
- defence reduces final incoming damage
- minimum damage taken from a successful hit is `1`

### Important rule
- dice display should reflect the raw attack roll
- damage popup should reflect final damage after strength and defence

---

## Units

### Current combat actors
- `CombatUnit` = shared combat base
- `PlayerUnit` = runtime player combat state
- `EnemyUnit` = runtime enemy combat state

### Current behavior
- health bars
- damage popups
- hit VFX
- turn indicators
- damage flash
- death handling
- initiative participation

### Health persistence
- the player no longer fully heals every room
- current HP carries between rooms
- on entering the next room, the player recovers a small amount instead of full heal

---

## Turn System

### Current turn behavior
- initiative is rolled at the start of room combat
- units are sorted by initiative
- each unit acts in order

### Player turn rules
- player starts the turn with `Stats.ActionPoints`
- `Move` can be used once per turn
- `Attack` can be used once per turn
- `Skip` ends the turn
- turn does not auto-end after acting

### Current actions
- `Move`
- `Attack`
- `Skip`
- `HealthPotion` consumable action

### UI feedback
- action point text
- attack / move mode highlights
- consumable button appears dynamically when a consumable is equipped

---

## Dice System

### Current dice logic
- actual attack roll is always based on `1..Attack`
- dice result is shown before damage is applied
- after the final roll is shown, the system waits briefly, then resolves the attack

### Current display logic
- normal dice assets are used for supported low values
- overflow / text fallback is used for higher unsupported values

### Important note
- low attack values should still display correctly using available dice visuals
- high attack values use the fallback text dice visual

---

## Equipment and Inventory

### Current equipment slots
- `Helmet`
- `BodyArmor`
- `Gloves`
- `Boots`
- `Shield`
- `Weapon`
- `Consumable`
- `Spare`

### Current behavior
- room loadout panel opens before combat if there is a spare item to manage
- player can move or swap items between valid slots
- `Spare <-> slot` swap is supported
- two-handed weapon logic is supported
- delete flow uses confirmation
- `Start Run` uses confirmation
- if no pending spare item exists, inventory panel does not open and combat starts directly

### Current item card behavior
- inventory slots instantiate card prefabs
- cards bind directly from `ItemSO`
- stat text now shows values directly, including `0`

### Current player card behavior
- player card updates from live player stats
- item changes update displayed attack / health / strength / range / speed / defence

---

## Item Data

### `ItemSO` currently supports
- `itemName`
- `itemDescription`
- `type`
- `tier`
- `cost`
- `isCursed`
- `equipmentSubtype`
- `weaponHandedness`
- `weaponAttackStyle`
- `projectilePrefab`
- `consumableHealAmount`
- `stats`
- `card`

### Notes
- `icon` has been removed from item data
- card visuals now use `card`

### Current item categories in use
- weapons
- equipment pieces
- consumables

---

## Consumables

### Current behavior
- consumables equip into the `Consumable` slot
- if equipped, the related action appears in the action bar
- using the consumable costs AP
- current health potion heals the player
- after use, it is consumed and removed

### Current health potion behavior
- one-time use
- AP cost applied
- heal amount comes from `ItemSO`
- consumable use VFX can play on the player

---

## Enemy System

### Current enemy data
Enemies are driven by `EnemyDefinitionSO`.

Current definition data includes:
- display name
- base stats
- world sprite
- turn order icon
- gold value
- attack style
- projectile prefab
- immunity settings
- missed-hit VFX

### Current special enemy behaviors
- Bandit can ignore odd player attack rolls
- this only applies to player attack rolls
- environmental damage like lava is not ignored

- Dark Knight / similar boss can ignore attack rolls below a threshold
- this threshold now works as “less than X”, not “X and below”

### Miss / immunity feedback
- ignored hits can play a missed VFX
- missed VFX now spawns in front of the unit sorting-wise

---

## Enemy Cards

### Current behavior
- there is one shared enemy card UI
- hovering an enemy shows that enemy’s data
- card uses enemy definition data plus runtime values
- current health and runtime stats update while the card is visible

### Current displayed data
- name
- world sprite portrait
- attack
- health
- strength
- range
- speed
- defence

---

## Rewards

### Current reward tile behavior
- each room can generate a reward tile
- stepping on it opens it
- opened state changes the tile visual
- reward can also be considered claimed when the room is cleared

### Current reward item behavior
- room rewards can grant a random item
- reward item tries to go into `Spare`
- if `Spare` is occupied, reward is blocked

---

## Wallet / Gold

### Current behavior
- enemies have `goldValue`
- killing enemies grants gold
- wallet persists during the run
- wallet resets on run reset / death flow
- gain/loss popup feedback already exists

### Not finished yet
- shop purchases
- item buying flow
- spending gold in shop

---

## Hazards

### Current lava behavior
- lava damages both player and enemies
- entering lava applies damage
- ending a turn on lava applies damage again
- damage popups are shown
- damage flash plays on hit

### Projectile behavior with hazards
- projectiles are not affected by lava

---

## Ranged Combat and Projectiles

### Current weapon behavior
Only weapons use the melee/ranged split.

Weapon attack styles:
- `Melee`
- `Ranged`

### Current enemy behavior
Enemy definitions also support:
- `Melee`
- `Ranged`

### Current projectile system
- projectile uses a prefab
- projectile is currently visual-first, not physics-driven
- hit resolution is calculated by code
- projectile then flies to the resolved hit position

### Current projectile rules
- no diagonal attacks
- same row or same column only
- projectile is blocked by blocked tiles
- projectile is not affected by lava
- projectile hits the first valid target in the path

### Current ranged AI behavior
- ranged enemies stop when the player is within valid attack range
- they do not need to move adjacent if range allows an attack

### Projectile setup note
- projectile prefab can be a simple object with a `SpriteRenderer`
- projectile visual rotates toward movement direction
- current rotation logic assumes arrow art is authored facing up

---

## Current Scene Setup Notes

### `RoomScene` should contain
- `GridManager`
- `RoomGenerator`
- `CombatManager`
- `TurnManager`
- room UI canvas
- action buttons
- optional defeat UI

### `FloorScene`
- should keep floor-map-specific controllers scene-local
- do not put floor-map scene logic on persistent manager objects

### Persistent singletons currently expected
- `RunManager`
- `EquipmentManager`
- `PlayerWallet`

---

## Current Editor Notes

### For ranged weapons
- set `weaponAttackStyle = Ranged`
- assign `projectilePrefab`
- set item `stats.range` if the weapon should increase range

### For melee weapons
- set `weaponAttackStyle = Melee`
- projectile prefab can stay empty

### For ranged enemies
- set enemy `attackStyle = Ranged`
- assign `projectilePrefab`

### For item cards
- card prefab references must be assigned correctly per slot
- if a stat is not visible, check the text reference in the prefab first

### Important current example
- `Bow` will not increase range unless its `stats.range` value is actually greater than `0`

---

## Planned Roadmap

### 1. Shop economy
- keep wallet across floors within the same run
- use `ItemSO.cost`
- use `ItemSO.tier`
- support cursed item offers

### 2. Cursed items
- at minimum, shop should be able to guarantee one cursed offer
- cursed items may later move to a dedicated subclass / extra data model if needed

### 3. Shop scene
- use one dedicated `ShopScene`
- each shop should show 4 random items
- at least 1 item should be cursed
- shop tier should follow floor progression:
  - shop 1 -> tier 1
  - shop 2 -> tier 2
  - shop 3 -> tier 3
  - shop 4 -> tier 4

### 4. Shop purchase flow
- shop items on the left
- player inventory / loadout on the right
- purchase rules:
  - if matching slot is empty, buy and equip directly
  - if matching slot is occupied, send purchase to `Spare`
  - if matching slot and `Spare` are both occupied, block purchase

### 5. Multi-floor progression
- keep one reusable `FloorScene`
- do not create separate floor scenes unless reuse becomes impossible
- expected long-term flow:
  - `Floor 1 -> Shop -> Floor 2 -> Shop -> Floor 3 -> Shop -> Floor 4`

### 6. Miniboss key system
- each floor should contain one miniboss room
- miniboss room should generate a `Key Tile`
- player gets the floor key if:
  - they step on the key tile
  - or they clear the miniboss room

### 7. Floor exit gate
- floor map should contain an exit gate / door
- exit is visible but locked by default
- after obtaining the key, exit changes to opened state
- using the opened exit should load the shop, then next floor

### 8. Floor-map traversal update
- cleared rooms should remain non-enterable
- but the player marker should still be able to move across cleared rooms

### 9. Content expansion
- more enemy types
- more boss / miniboss types
- more item types
- cursed items
- more room modifiers
- more polish on VFX / animation / feedback

