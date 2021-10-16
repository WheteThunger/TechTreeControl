using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System;
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

        private static TechTreeControl _pluginInstance;
        private static Configuration _pluginConfig;

        private const string PermissionAnyOrderLevel1 = "techtreecontrol.anyorder.level1";
        private const string PermissionAnyOrderLevel2 = "techtreecontrol.anyorder.level2";
        private const string PermissionAnyOrderLevel3 = "techtreecontrol.anyorder.level3";

        private const string PermissionRulesetFormat = "techtreecontrol.ruleset.{0}";

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;

            permission.RegisterPermission(PermissionAnyOrderLevel1, this);
            permission.RegisterPermission(PermissionAnyOrderLevel2, this);
            permission.RegisterPermission(PermissionAnyOrderLevel3, this);

            foreach (var ruleset in _pluginConfig.BlueprintRulesets)
            {
                if (!string.IsNullOrEmpty(ruleset.Name))
                    permission.RegisterPermission(GetRulesetPermission(ruleset.Name), this);
            }
        }

        private void Unload()
        {
            _pluginInstance = null;
            _pluginConfig = null;
        }

        private bool? CanUnlockTechTreeNode(BasePlayer player, NodeInstance node, TechTreeData techTree)
        {
            var blueprintRuleset = _pluginConfig.GetPlayerBlueprintRuleset(player.UserIDString);
            if (blueprintRuleset == null)
                return  null;

            if (node.itemDef != null && !blueprintRuleset.IsAllowed(node.itemDef))
                return false;

            return null;
        }

        private bool? CanUnlockTechTreeNodePath(BasePlayer player, NodeInstance node, TechTreeData techTree)
        {
            if (HasPermissionToUnlockAny(player, techTree))
                return true;

            var blueprintRuleset = _pluginConfig.GetPlayerBlueprintRuleset(player.UserIDString);
            if (blueprintRuleset == null)
                return  null;

            if (node.itemDef != null && !blueprintRuleset.HasPrerequisites(node.itemDef))
                return true;

            if (HasUnlockPath(player, node, techTree, blueprintRuleset))
                return true;

            return null;
        }

        private int? OnResearchCostDetermine(ItemDefinition itemDefinition)
        {
            return _pluginConfig.GetResearchCostOverride(itemDefinition);
        }

        #endregion

        #region Helper Methods

        private static bool TechTreeNodeUnlockWasBlocked(Drone drone, BasePlayer deployer)
        {
            object hookResult = Interface.CallHook("OnTechTreeNodeForceUnlock", drone, deployer);
            return hookResult is bool && (bool)hookResult == false;
        }

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
                    hasUnlockPath = true;
            }

            return hasUnlockPath;
        }

        private static bool HasPermissionToUnlockAny(BasePlayer player, TechTreeData techTree)
        {
            if (techTree.name == "TechTreeT3")
                return _pluginInstance.permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel3);
            if (techTree.name == "TechTreeT2")
                return _pluginInstance.permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel2);
            else if (techTree.name == "TechTreeT0")
                return _pluginInstance.permission.UserHasPermission(player.UserIDString, PermissionAnyOrderLevel1);

            return false;
        }

        private static string GetRulesetPermission(string name) =>
            String.Format(PermissionRulesetFormat, name);

        #endregion

        #region Configuration

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("ResearchCosts")]
            public Dictionary<string, int> ResearchCosts = new Dictionary<string, int>();

            [JsonProperty("BlueprintRulesets")]
            public BlueprintRuleset[] BlueprintRulesets = new BlueprintRuleset[0];

            public int? GetResearchCostOverride(ItemDefinition itemDefinition)
            {
                foreach (var entry in _pluginConfig.ResearchCosts)
                {
                    if (entry.Key == itemDefinition.shortname)
                        return entry.Value;
                }
                return null;
            }

            public BlueprintRuleset GetPlayerBlueprintRuleset(string userIdString)
            {
                var blueprintRulesets = BlueprintRulesets;
                if (blueprintRulesets == null)
                    return null;

                for (var i = blueprintRulesets.Length - 1; i >= 0; i--)
                {
                    var ruleset = blueprintRulesets[i];
                    if (!string.IsNullOrEmpty(ruleset.Name)
                        && _pluginInstance.permission.UserHasPermission(userIdString, GetRulesetPermission(ruleset.Name)))
                        return ruleset;
                }

                return BlueprintRuleset.DefaultRuleset;
            }
        }

        private class BlueprintRuleset
        {
            public static BlueprintRuleset DefaultRuleset = new BlueprintRuleset();

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

            public bool HasPrerequisites(ItemDefinition itemDefinition) =>
                !BlueprintsWithNoPrerequisites.Contains(itemDefinition.shortname);

            public bool IsAllowed(ItemDefinition itemDefinition)
            {
                if (AllowedBlueprints != null)
                    return AllowedBlueprints.Contains(itemDefinition.shortname);

                if (DisallowedBlueprints != null)
                    return !DisallowedBlueprints.Contains(itemDefinition.shortname);

                return true;
            }

            public bool IsOptional(ItemDefinition itemDefinition) =>
                OptionalBlueprints.Contains(itemDefinition.shortname);
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
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

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

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

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
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
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion
    }
}
