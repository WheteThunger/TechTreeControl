using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using static TechTreeData;

namespace Oxide.Plugins
{
    [Info("Tech Tree Control", "WhiteThunder", "0.2.0")]
    [Description("Allows customizing Tech Tree research requirements.")]
    internal class TechTreeControl : CovalencePlugin
    {
        #region Fields

        private const string PermissionAnyOrderLevel1 = "techtreecontrol.anyorder.level1";
        private const string PermissionAnyOrderLevel2 = "techtreecontrol.anyorder.level2";
        private const string PermissionAnyOrderLevel3 = "techtreecontrol.anyorder.level3";

        private readonly object True = true;
        private readonly object False = false;

        private Configuration _config;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionAnyOrderLevel1, this);
            permission.RegisterPermission(PermissionAnyOrderLevel2, this);
            permission.RegisterPermission(PermissionAnyOrderLevel3, this);

            _config.Init(this);
        }

        private object CanUnlockTechTreeNode(BasePlayer player, NodeInstance node, TechTreeData techTree)
        {
            var blueprintRuleset = _config.GetPlayerBlueprintRuleset(this, player.UserIDString);
            if (blueprintRuleset == null)
                return null;

            if (node.itemDef != null && !blueprintRuleset.IsAllowed(node.itemDef))
                return False;

            return null;
        }

        private object CanUnlockTechTreeNodePath(BasePlayer player, NodeInstance node, TechTreeData techTree)
        {
            if (HasPermissionToUnlockAny(player, techTree))
                return True;

            var blueprintRuleset = _config.GetPlayerBlueprintRuleset(this, player.UserIDString);
            if (blueprintRuleset == null)
                return null;

            if (node.itemDef != null && !blueprintRuleset.HasPrerequisites(node.itemDef))
                return True;

            if (HasUnlockPath(player, node, techTree, blueprintRuleset))
                return True;

            return null;
        }

        private object OnResearchCostDetermine(ItemDefinition itemDefinition)
        {
            return _config.GetResearchCostOverride(itemDefinition);
        }

        #endregion

        #region Helper Methods

        private static bool HasUnlockPath(BasePlayer player, NodeInstance node, TechTreeData techTree, BlueprintRuleset blueprintRuleset)
        {
            if (node.inputs.Count == 0)
                return true;

            var hasUnlockPath = false;

            foreach (var inputNodeId in node.inputs)
            {
                var inputNode = techTree.GetByID(inputNodeId);
                if (inputNode.itemDef == null)
                {
                    // This input node appears to be an entry node, so consider it unlocked.
                    return true;
                }

                if (!techTree.HasPlayerUnlocked(player, inputNode) && !blueprintRuleset.IsOptional(inputNode.itemDef))
                {
                    // This input node does not provide an unlock path.
                    // Continue iterating the other input nodes in case they provide an unlock path.
                    continue;
                }

                if (HasUnlockPath(player, inputNode, techTree, blueprintRuleset))
                {
                    hasUnlockPath = true;
                }
            }

            return hasUnlockPath;
        }

        private bool HasPermissionToUnlockAny(BasePlayer player, TechTreeData techTree)
        {
            if (techTree.name == "TechTreeT3")
                return permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel3);

            if (techTree.name == "TechTreeT2")
                return permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel2);

            if (techTree.name == "TechTreeT0")
                return permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel1);

            return false;
        }

        #endregion

        #region Configuration

        [JsonObject(MemberSerialization.OptIn)]
        private class BlueprintRuleset
        {
            public static readonly BlueprintRuleset DefaultRuleset = new BlueprintRuleset();

            [JsonProperty("Name")]
            public string Name;

            [JsonProperty("OptionalBlueprints")]
            public HashSet<string> OptionalBlueprints = new HashSet<string>();

            [JsonProperty("AllowedBlueprints", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public HashSet<string> AllowedBlueprints;

            [JsonProperty("DisallowedBlueprints", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public HashSet<string> DisallowedBlueprints;

            [JsonProperty("BlueprintsWithNoPrerequisites")]
            public HashSet<string> BlueprintsWithNoPrerequisites = new HashSet<string>();

            [JsonIgnore]
            public string Permission { get; private set; }

            public void Init(TechTreeControl plugin)
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    Permission = $"{nameof(TechTreeControl)}.ruleset.{Name}".ToLower();
                    plugin.permission.RegisterPermission(Permission, plugin);
                }
            }

            public bool HasPrerequisites(ItemDefinition itemDefinition)
            {
                return !BlueprintsWithNoPrerequisites.Contains(itemDefinition.shortname);
            }

            public bool IsAllowed(ItemDefinition itemDefinition)
            {
                if (AllowedBlueprints != null)
                    return AllowedBlueprints.Contains(itemDefinition.shortname);

                if (DisallowedBlueprints != null)
                    return !DisallowedBlueprints.Contains(itemDefinition.shortname);

                return true;
            }

            public bool IsOptional(ItemDefinition itemDefinition)
            {
                return OptionalBlueprints.Contains(itemDefinition.shortname);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class Configuration : BaseConfiguration
        {
            [JsonProperty("ResearchCosts")]
            public Dictionary<string, int> ResearchCosts = new Dictionary<string, int>();

            [JsonProperty("BlueprintRulesets")]
            public BlueprintRuleset[] BlueprintRulesets = Array.Empty<BlueprintRuleset>();

            public void Init(TechTreeControl plugin)
            {
                if (BlueprintRulesets != null)
                {
                    foreach (var ruleset in BlueprintRulesets)
                    {
                        ruleset.Init(plugin);
                    }
                }
            }

            public int? GetResearchCostOverride(ItemDefinition itemDefinition)
            {
                foreach (var entry in ResearchCosts)
                {
                    if (entry.Key == itemDefinition.shortname)
                        return entry.Value;
                }

                return null;
            }

            public BlueprintRuleset GetPlayerBlueprintRuleset(TechTreeControl plugin, string userIdString)
            {
                if (BlueprintRulesets != null)
                {
                    for (var i = BlueprintRulesets.Length - 1; i >= 0; i--)
                    {
                        var ruleset = BlueprintRulesets[i];
                        if (ruleset.Permission != null
                            && plugin.permission.UserHasPermission(userIdString, ruleset.Permission))
                            return ruleset;
                    }
                }

                return BlueprintRuleset.DefaultRuleset;
            }
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #region Configuration Helpers

        private class BaseConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(BaseConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            var changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_config, true);
        }

        #endregion

        #endregion
    }
}
