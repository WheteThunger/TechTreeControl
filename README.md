## Features

- Allows customizing research costs
- Allows restricting specific blueprints
- Allows skipping specific blueprints
- Allows unlocking blueprints out of order

## How it works

It is not possible to customize the vanilla tech tree UI, but this plugin is able to detect when a player clicks on the "Unlock", "No Path" or "Can't Afford" buttons, in order to allow or disallow the unlock based on configurable criteria in this plugin.

## Permissions

The following permissions allow players to unlock blueprints in any order.

- `techtreecontrol.anyorder.level1` -- Applies to the level 1 workbench.
- `techtreecontrol.anyorder.level2` -- Applies to the level 2 workbench.
- `techtreecontrol.anyorder.level3` -- Applies to the level 3 workbench.

Additional permissions will be generated based on the plugin configuration, which can allow specific blueprints to be restricted and/or skipped. See the configuration section for details.

## Configuration

Default configuration:

```json
{
  "ResearchCosts": {},
  "BlueprintRulesets": []
}
```

- `ResearchCosts` -- This option allows you to override the vanilla research cost for any item based on it's short name. Only applies to the tech tree, not the research table. Applies to all players (not based on permission). See below for examples.
- `BlueprintRulesets` -- This option allows you to restrict blueprints based on player permission. See below for examples. Each ruleset defined here generates a permission of the format `techtreecontrol.ruleset.<name>`. Granting a ruleset to a player determines which blueprints they are allowed to unlock and/or skip. Granting multiple rulesets to a player will cause only the last to apply (based on the order in the config).
  - `OptionalBlueprints` -- This list of item short names determines which blueprints are allowed to be skipped by players with this ruleset. This enables players to progress beyond the optional blueprints as though they were unlocked.
  - `DisallowedBlueprints` -- This list of item short names determines which blueprints are not allowed to be unlocked by players with this ruleset. If you want to allow players to skip these, you should also add them to `OptionalBlueprints`, or else players will be prevented players from advancing.
  - `BlueprintsWithNoPrerequisites` -- This list of item short names determines which blueprints can be unlocked without unlocking any prior blueprints.

Note: While this plugin allows players to skip blueprints in some cases, it won't be obvious to players that this is possible due to limitations in modding the UI.

### Example config using `ResearchCosts`

```json
{
  "ResearchCosts": {
    "explosive.timed": 1000,
    "ammo.rocket.basic": 500
  },
  "BlueprintRulesets": []
}
```

### Example config using `BlueprintRulesets`

```json
{
  "ResearchCosts": {},
  "BlueprintRulesets": [
    {
      "Name": "allowall",
      "AllowSkippingDisallowedBlueprints": true,
      "OptionalBlueprints": [],
      "DisallowedBlueprints": [],
      "BlueprintsWithNoPrerequisites": []
    },
    {
      "Name": "noexplosives",
      "AllowSkippingDisallowedBlueprints": true,
      "OptionalBlueprints": [],
      "DisallowedBlueprints": [
        "ammo.grenadelauncher.he",
        "ammo.rocket.basic",
        "ammo.rocket.fire",
        "ammo.rocket.hv",
        "ammo.rifle.explosive",
        "explosive.satchel",
        "explosive.timed",
        "explosives",
      ],
      "BlueprintsWithNoPrerequisites": []
    }
  ]
}
```

The above example config would generate the following permissions.
- `techtreecontrol.ruleset.allowall` -- Allows all blueprints to be unlocked.
- `techtreecontrol.ruleset.noexplosives` -- Blocks most explosives from being unlocked.

## FAQ

#### Is it possible to customize the tech tree layout?

Not possible. That is all client-side.

#### Is it possible to customize the displayed prices?

Not possible. That is all client-side. Plugins cannot detect whether the tech tree is open, nor which tech tree node the player has selected. Plugins can only detect when a player clicks the button to unlock a specific blueprint.

## Future development plans (when time permits)

- Show a persistent UI message while the player is looting a workbench, which should state that the tech tree is modded, with the following details depending on the level of the workbench (and corresponding tech tree).
  - List of optional blueprints
  - List of disallowed blueprints
  - List of price modifications
  - Whether the player has permission to skip any blueprint
- Show a brief UI message in the following situations.
  - When a player is charged a non-vanilla price, show the amount they were charged.
  - When the player fails to unlock a node that uses a non-vanilla price, show the amount required.
  - When the player fails to unlock a node that is disallowed, clearly state that it is not allowed and whether it can be skipped.
- Support for alternate items or currencies (Economics, Server Rewards)

Please let me know if you would like support for the following features so that I can prioritize them.
- Different prices based on player permission.
- Different prices when using the research table.
