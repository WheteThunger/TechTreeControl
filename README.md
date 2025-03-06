## Features

- Allows customizing research costs
- Allows restricting blueprints
- Allows skipping blueprints

## How it works

It is not possible to customize the vanilla tech tree UI, but this plugin is able to detect when a player clicks on the "Unlock", "No Path" or "Can't Afford" buttons, in order to allow or disallow the unlock based on configurable criteria in this plugin.

## Permissions

The following permissions allow players to unlock blueprints in any order.

- `techtreecontrol.anyorder.level1` -- Applies to the level 1 workbench.
- `techtreecontrol.anyorder.level2` -- Applies to the level 2 workbench.
- `techtreecontrol.anyorder.level3` -- Applies to the level 3 workbench.
- `techtreecontrol.anyorder.io` -- Applies to the IO engineering workbench.

Additional permissions will be generated based on the plugin configuration, which can allow specific blueprints to be restricted and/or skipped. See the configuration section for details.

## Configuration

Default configuration:

```json
{
  "Enable chat feedback": true,
  "Enable PopupNotifications integration": false,
  "Research costs": {},
  "Custom currency": {
    "Enabled": false,
    "Item short name": "scrap"
  },
  "Blueprint rulesets": []
}
```

- `Enable chat feedback` (`true` or `false`) -- While `true`, players will receive feedback in chat when failing to unlock a blueprint that isn't allowed. Note: The player will not see the chat message until they close the tech tree.
- `Enable PopupNotifications integration` (`true` or `false`) -- While `true`, players will receive feedback via the Popup Notifications plugin when failing to unlock a blueprint that isn't allowed.
- `Research costs` -- This option allows you to override the vanilla research cost for any item based on it's short name. Only applies to the tech tree, not the research table. Applies to all players (not based on permission). See below for examples. Note: Players must still pay workbench tax rates.
- `Custom currency` -- This option allows you to change the currency required to unlock blueprints via the tech tree.
  - `Enabled` (`true` or `false`) -- While `true`, the custom currency item (`Item short name` below) will be used.
  - `Item short name` -- The short name of the item to use instead of scrap.
- `Blueprint rulesets` -- This option allows you to control which blueprints players can unlock or skip based on player permission. See below for examples. Each ruleset defined here generates a permission of the format `techtreecontrol.ruleset.<name>`. Granting a ruleset to a player determines which blueprints they are allowed to unlock and/or skip. Granting multiple rulesets to a player will cause only the last to apply (based on the order in the config).
  - `Optional blueprints` -- This list of item short names determines which blueprints are allowed to be skipped by players with this ruleset. Making a blueprint optional enables players to progress beyond it without unlocking it.
  - `Allowed blueprints` -- This list of item short names determines which blueprints are allowed to be unlocked by players with this ruleset. This is an alternative to `Disallowed blueprints`.
  - `Disallowed blueprints` -- This list of item short names determines which blueprints are **not** allowed to be unlocked by players with this ruleset. If you want to allow players to skip these, you should also add them to `Optional blueprints`, or else players will be prevented from advancing. This option is ignored if `Allowed blueprints` is defined in the ruleset.
  - `Blueprints with no prerequisites` -- This list of item short names determines which blueprints can be unlocked without unlocking any prior blueprints.

Note: While this plugin allows players to skip blueprints in some cases, it won't be obvious to players that this is possible due to limitations in modding the UI.

### Example config using `Research costs`

```json
{
  "Enable chat feedback": true,
  "Enable PopupNotifications integration": false,
  "Research costs": {
    "explosive.timed": 1000,
    "ammo.rocket.basic": 500
  },
  "Blueprint rulesets": []
}
```

### Example config using `Blueprint rulesets`

```json
{
  "Enable chat feedback": true,
  "Enable PopupNotifications integration": false,
  "Research costs": {},
  "Blueprint rulesets": [
    {
      "Name": "disallowall",
      "Allowed blueprints": []
    },
    {
      "Name": "allowall",
      "Disallowed blueprints": []
    },
    {
      "Name": "noexplosives",
      "Optional blueprints": [],
      "Disallowed blueprints": [
        "ammo.grenadelauncher.he",
        "ammo.rocket.basic",
        "ammo.rocket.fire",
        "ammo.rocket.hv",
        "ammo.rifle.explosive",
        "explosive.satchel",
        "explosive.timed",
        "explosives"
      ],
      "Blueprints with no prerequisites": []
    }
  ]
}
```

The above example config would generate the following permissions.
- `techtreecontrol.ruleset.disallowall` -- Denies all blueprints from being unlocked.
- `techtreecontrol.ruleset.allowall` -- Allows all blueprints to be unlocked.
- `techtreecontrol.ruleset.noexplosives` -- Blocks most explosives from being unlocked.

## FAQ

#### Is it possible to customize the tech tree layout?

Not possible. That is all client-side.

#### Is it possible to customize the displayed prices?

Not possible. That is all client-side. Plugins cannot detect whether the tech tree is open, nor which tech tree node the player has selected. Plugins can only detect when a player clicks the button to unlock a specific blueprint.
