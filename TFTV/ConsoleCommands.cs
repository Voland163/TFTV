using Base.Core;
using Base.Defs;
using Base.Utils.GameConsole;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsSharedData;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Research.Requirement;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Abilities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TFTV;
using TFTV.TFTVBaseRework;
using TFTV.TFTVIncidents;
using UnityEngine;

namespace MadSkunkyTweaks.Tools
{
    public class ConsoleCommands
    {
        [ConsoleCommand(
    Command = "monster_tint_debug",
    Description = "Usage: monster_tint_debug <actorNameContains> <primary|secondary>")]
        public static void DebugMonsterTint(
    IConsole console,
    string actorNameContains,
    string colorChannel)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterTint] This command must be used during a tactical mission.");
                    return;
                }

                string shaderProperty;

                if (colorChannel.Equals(
                    "primary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    shaderProperty = "_PrimaryColor";
                }
                else if (colorChannel.Equals(
                    "secondary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    shaderProperty = "_SecondaryColor";
                }
                else
                {
                    TFTVLogger.Always(
                        "[MonsterTint] Use either 'primary' or 'secondary'.");
                    return;
                }

                foreach (TacticalActor actor in level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor =>
                        actor != null &&
                        actor.name != null &&
                        actor.name.IndexOf(
                            actorNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (actor.AddonsManager?.RootAddon == null)
                    {
                        continue;
                    }

                    TFTVLogger.Always(
                        $"[MonsterTint] Inspecting '{actor.name}' " +
                        $"property '{shaderProperty}'.");

                    foreach (TacticalItem item in
                        actor.AddonsManager.RootAddon.OfType<TacticalItem>())
                    {
                        if (item.VisualRoot == null)
                        {
                            continue;
                        }

                        foreach (Renderer renderer in
                            item.VisualRoot.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer == null ||
                                renderer is ParticleSystemRenderer)
                            {
                                continue;
                            }

                            MaterialPropertyBlock propertyBlock =
                                new MaterialPropertyBlock();

                            renderer.GetPropertyBlock(propertyBlock);

                            Color blockColor =
                                propertyBlock.GetColor(shaderProperty);

                            for (int materialIndex = 0;
                                materialIndex < renderer.sharedMaterials.Length;
                                materialIndex++)
                            {
                                Material material =
                                    renderer.sharedMaterials[materialIndex];

                                if (material == null ||
                                    !material.HasProperty(shaderProperty))
                                {
                                    continue;
                                }

                                Color materialColor =
                                    material.GetColor(shaderProperty);

                                TFTVLogger.Always(
                                    $"[MonsterTint] Item='{item.ItemDef?.name}', " +
                                    $"renderer='{renderer.name}', " +
                                    $"material[{materialIndex}]=" +
                                    $"'{material.name}', " +
                                    $"shader='{material.shader?.name}', " +
                                    $"materialColor={FormatColor(materialColor)}, " +
                                    $"propertyBlockColor={FormatColor(blockColor)}.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static string FormatColor(Color color)
        {
            return
                $"RGBA({color.r:F3}, {color.g:F3}, " +
                $"{color.b:F3}, {color.a:F3})";
        }

        [ConsoleCommand(
    Command = "monster_tint",
    Description = "Usage: monster_tint <actorNameContains> <primary|secondary> <red 0-255> <green 0-255> <blue 0-255>")]
        public static void TintMonsterDirectly(
    IConsole console,
    string actorNameContains,
    string colorChannel,
    int red,
    int green,
    int blue,
    int intensityPercent)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterTint] This command must be used during a tactical mission.");
                    return;
                }

                string shaderProperty;
                bool isEmission;

                if (colorChannel.Equals(
                    "primary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    shaderProperty = "_PrimaryColor";
                    isEmission = false;
                }
                else if (colorChannel.Equals(
                    "secondary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    shaderProperty = "_SecondaryColor";
                    isEmission = false;
                }
                else if (colorChannel.Equals(
                    "emission",
                    StringComparison.OrdinalIgnoreCase))
                {
                    shaderProperty = "_EmissionColor";
                    isEmission = true;
                }
                else
                {
                    TFTVLogger.Always(
                        "[MonsterTint] The color channel must be " +
                        "'primary', 'secondary', or 'emission'.");
                    return;
                }

                red = Mathf.Clamp(red, 0, 255);
                green = Mathf.Clamp(green, 0, 255);
                blue = Mathf.Clamp(blue, 0, 255);

                Color tint = new Color(
                    red / 255f,
                    green / 255f,
                    blue / 255f,
                    1f);

                if (isEmission)
                {
                    float intensity =
                        Mathf.Max(0, intensityPercent) / 100f;

                    tint.r *= intensity;
                    tint.g *= intensity;
                    tint.b *= intensity;
                }

                List<TacticalActor> matchingActors = level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor =>
                        actor != null &&
                        actor.name != null &&
                        actor.name.IndexOf(
                            actorNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (matchingActors.Count == 0)
                {
                    TFTVLogger.Always(
                        $"[MonsterTint] No tactical actor name contains " +
                        $"'{actorNameContains}'.");
                    return;
                }

                int changedActors = 0;
                int changedRenderers = 0;

                foreach (TacticalActor actor in matchingActors)
                {
                    if (actor.AddonsManager?.RootAddon == null)
                    {
                        TFTVLogger.Always(
                            $"[MonsterTint] Skipping '{actor.name}': " +
                            "the actor has no initialized root add-on.");
                        continue;
                    }

                    int actorChangedRenderers = 0;

                    foreach (TacticalItem item in
                        actor.AddonsManager.RootAddon.OfType<TacticalItem>())
                    {
                        if (item.VisualRoot == null)
                        {
                            continue;
                        }

                        foreach (Renderer renderer in
                            item.VisualRoot.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer == null ||
                                renderer is ParticleSystemRenderer)
                            {
                                continue;
                            }

                            bool supportsProperty = renderer.sharedMaterials.Any(
                                material =>
                                    material != null &&
                                    material.HasProperty(shaderProperty));

                            if (!supportsProperty)
                            {
                                continue;
                            }

                            MaterialPropertyBlock propertyBlock =
                                new MaterialPropertyBlock();

                            renderer.GetPropertyBlock(propertyBlock);
                            propertyBlock.SetColor(shaderProperty, tint);
                            renderer.SetPropertyBlock(propertyBlock);

                            Color appliedColor =
                                propertyBlock.GetColor(shaderProperty);

                            TFTVLogger.Always(
                                $"[MonsterTint] Actor='{actor.name}', " +
                                $"item='{item.ItemDef?.name}', " +
                                $"renderer='{renderer.name}', " +
                                $"property='{shaderProperty}', " +
                                $"requested={FormatColor(tint)}, " +
                                $"block={FormatColor(appliedColor)}.");

                            actorChangedRenderers++;
                            changedRenderers++;
                        }
                    }

                    if (actorChangedRenderers > 0)
                    {
                        changedActors++;
                    }

                    TFTVLogger.Always(
                        $"[MonsterTint] '{actor.name}': changed " +
                        $"{actorChangedRenderers} renderers.");
                }

                TFTVLogger.Always(
                    $"[MonsterTint] Finished. Changed {changedRenderers} renderers " +
                    $"across {changedActors} actors.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static readonly string[] CandidateAlienColorProperties =
{
    "_Color",
    "_BaseColor",
    "_TintColor",
    "_MainColor",
    "_PrimaryColor",
    "_SecondaryColor",
    "_ColorPrimary",
    "_ColorSecondary",
    "_SkinColor",
    "_BodyColor",
    "_ArmorColor",
    "_EmissionColor"
};

        [ConsoleCommand(
            Command = "monster_shader_props",
            Description = "Usage: monster_shader_props <actorNameContains>")]
        public static void ListMonsterShaderProperties(
            IConsole console,
            string actorNameContains)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterShader] This command must be used in tactical.");
                    return;
                }

                foreach (TacticalActor actor in level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor =>
                        actor != null &&
                        actor.name != null &&
                        actor.name.IndexOf(
                            actorNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if (actor.AddonsManager?.RootAddon == null)
                    {
                        TFTVLogger.Always(
                            $"[MonsterShader] '{actor.name}' has no root add-on.");
                        continue;
                    }

                    TFTVLogger.Always(
                        $"[MonsterShader] Inspecting '{actor.name}'.");

                    foreach (TacticalItem item in
                        actor.AddonsManager.RootAddon.OfType<TacticalItem>())
                    {
                        if (item.VisualRoot == null)
                        {
                            continue;
                        }

                        foreach (Renderer renderer in
                            item.VisualRoot.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer is ParticleSystemRenderer)
                            {
                                continue;
                            }

                            foreach (Material material in renderer.sharedMaterials)
                            {
                                if (material == null)
                                {
                                    continue;
                                }

                                string matchingProperties = string.Join(
                                    ", ",
                                    CandidateAlienColorProperties.Where(
                                        material.HasProperty));

                                TFTVLogger.Always(
                                    $"[MonsterShader] Renderer='{renderer.name}', " +
                                    $"material='{material.name}', " +
                                    $"shader='{material.shader?.name}', " +
                                    $"candidate properties=[" +
                                    $"{matchingProperties}]");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
    Command = "monster_color_debug",
    Description = "Usage: monster_color_debug <actorNameContains> <colorTagNameContains>")]
        public static void DebugMonsterColor(
    IConsole console,
    string actorNameContains,
    string colorTagNameContains)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] This command must be used in a tactical mission.");
                    return;
                }

                DefRepository repository =
                    GameUtl.GameComponent<DefRepository>();

                SharedData sharedData =
                    GameUtl.GameComponent<SharedData>();

                if (repository == null || sharedData?.SharedGameTags == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] DefRepository or SharedData is unavailable.");
                    return;
                }

                CustomizationColorTagDef colorTag = repository
                    .GetAllDefs<CustomizationColorTagDef>()
                    .FirstOrDefault(tag =>
                        tag != null &&
                        tag.name.IndexOf(
                            colorTagNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0);

                if (colorTag == null)
                {
                    TFTVLogger.Always(
                        $"[MonsterColor] No color tag contains " +
                        $"'{colorTagNameContains}'.");
                    return;
                }

                List<TacticalActor> actors = level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor =>
                        actor != null &&
                        actor.name != null &&
                        actor.name.IndexOf(
                            actorNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (actors.Count == 0)
                {
                    TFTVLogger.Always(
                        $"[MonsterColor] No actor name contains " +
                        $"'{actorNameContains}'.");
                    return;
                }

                foreach (TacticalActor actor in actors)
                {
                    bool conditional =
                        actor.GameTags.Contains(
                            sharedData.SharedGameTags.ConditionalCustomizationTag);

                    TFTVLogger.Always(
                        $"[MonsterColor] Actor '{actor.name}': " +
                        $"conditional={conditional}, " +
                        $"addonsManager={actor.AddonsManager != null}, " +
                        $"shaderParam='{colorTag.ShaderParamName}'.");

                    if (actor.AddonsManager?.RootAddon == null)
                    {
                        continue;
                    }

                    foreach (TacticalItem item in
                        actor.AddonsManager.RootAddon.OfType<TacticalItem>())
                    {
                        TFTVLogger.Always(
                            $"[MonsterColor]   Item '{item.ItemDef?.name}', " +
                            $"AlwaysCustomizeColor=" +
                            $"{item.ItemDef?.AlwaysCustomizeColor}, " +
                            $"VisualRoot={item.VisualRoot != null}");

                        if (item.VisualRoot == null)
                        {
                            continue;
                        }

                        foreach (Renderer renderer in
                            item.VisualRoot.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer is ParticleSystemRenderer)
                            {
                                continue;
                            }

                            Material[] materials = renderer.sharedMaterials;

                            for (int materialIndex = 0;
                                materialIndex < materials.Length;
                                materialIndex++)
                            {
                                Material material = materials[materialIndex];

                                if (material == null)
                                {
                                    continue;
                                }

                                bool hasColorProperty =
                                    !string.IsNullOrEmpty(colorTag.ShaderParamName) &&
                                    material.HasProperty(colorTag.ShaderParamName);

                                TFTVLogger.Always(
                                    $"[MonsterColor]     Renderer='{renderer.name}', " +
                                    $"material[{materialIndex}]=" +
                                    $"'{material.name}', " +
                                    $"shader='{material.shader?.name}', " +
                                    $"has '{colorTag.ShaderParamName}'=" +
                                    $"{hasColorProperty}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
    Command = "monster_color",
    Description = "Usage: monster_color <actorNameContains> <colorTagNameContains> <primary|secondary>")]
        public static void ChangeMonsterColor(
    IConsole console,
    string actorNameContains,
    string colorTagNameContains,
    string colorChannel)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] This command must be used during a tactical mission.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(actorNameContains))
                {
                    TFTVLogger.Always(
                        "[MonsterColor] actorNameContains cannot be empty.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(colorTagNameContains))
                {
                    TFTVLogger.Always(
                        "[MonsterColor] colorTagNameContains cannot be empty.");
                    return;
                }

                bool usePrimaryColor;

                if (colorChannel.Equals(
                    "primary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    usePrimaryColor = true;
                }
                else if (colorChannel.Equals(
                    "secondary",
                    StringComparison.OrdinalIgnoreCase))
                {
                    usePrimaryColor = false;
                }
                else
                {
                    TFTVLogger.Always(
                        "[MonsterColor] The color channel must be either " +
                        "'primary' or 'secondary'.");
                    return;
                }

                DefRepository repository =
                    GameUtl.GameComponent<DefRepository>();

                if (repository == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] DefRepository is unavailable.");
                    return;
                }

                List<CustomizationColorTagDef> matchingColorTags =
                    usePrimaryColor
                        ? repository
                            .GetAllDefs<CustomizationPrimaryColorTagDef>()
                            .Where(tag =>
                                tag != null &&
                                tag.name.IndexOf(
                                    colorTagNameContains,
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                            .Cast<CustomizationColorTagDef>()
                            .OrderBy(tag => tag.name)
                            .ToList()
                        : repository
                            .GetAllDefs<CustomizationSecondaryColorTagDef>()
                            .Where(tag =>
                                tag != null &&
                                tag.name.IndexOf(
                                    colorTagNameContains,
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                            .Cast<CustomizationColorTagDef>()
                            .OrderBy(tag => tag.name)
                            .ToList();

                if (matchingColorTags.Count == 0)
                {
                    TFTVLogger.Always(
                        $"[MonsterColor] No {colorChannel} color tag contains " +
                        $"'{colorTagNameContains}'.");
                    return;
                }

                if (matchingColorTags.Count > 1)
                {
                    TFTVLogger.Always(
                        $"[MonsterColor] '{colorTagNameContains}' matches multiple " +
                        $"{colorChannel} color tags. Use a more specific name:");

                    foreach (CustomizationColorTagDef matchingTag in matchingColorTags)
                    {
                        TFTVLogger.Always(
                            $"[MonsterColor]   {matchingTag.name} " +
                            $"(shader parameter: {matchingTag.ShaderParamName})");
                    }

                    return;
                }

                CustomizationColorTagDef colorTag = matchingColorTags[0];

                List<TacticalActor> matchingActors = level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor =>
                        actor != null &&
                        actor.name != null &&
                        actor.name.IndexOf(
                            actorNameContains,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (matchingActors.Count == 0)
                {
                    TFTVLogger.Always(
                        $"[MonsterColor] No tactical actor name contains " +
                        $"'{actorNameContains}'.");
                    return;
                }

                int changedActors = 0;

                foreach (TacticalActor actor in matchingActors)
                {
                    if (actor.GameTags == null)
                    {
                        TFTVLogger.Always(
                            $"[MonsterColor] Skipping {actor.name}: " +
                            "the actor has no GameTags list.");
                        continue;
                    }

                    if (actor.AddonsManager == null ||
                        actor.AddonsManager.RootAddon == null)
                    {
                        TFTVLogger.Always(
                            $"[MonsterColor] Skipping {actor.name}: " +
                            "the actor has no initialized AddonsManager.");
                        continue;
                    }

                    SharedGameTagsDataDef sharedTags =
    GameUtl.GameComponent<SharedData>()?.SharedGameTags;

                    if (sharedTags?.ConditionalCustomizationTag != null &&
                        actor.GameTags.Contains(sharedTags.ConditionalCustomizationTag))
                    {
                        actor.GameTags.Remove(sharedTags.ConditionalCustomizationTag);

                        TFTVLogger.Always(
                            $"[MonsterColor] Removed ConditionalCustomizationTag from " +
                            $"'{actor.name}'.");
                    }

                    actor.GameTags.Add(
                        colorTag,
                        GameTagAddMode.ReplaceExistingExclusive);

                    int refreshedItems = 0;

                    foreach (TacticalItem item in
                        actor.AddonsManager.RootAddon.OfType<TacticalItem>())
                    {
                        item.RefreshTags();
                        refreshedItems++;
                    }

                    changedActors++;

                    TFTVLogger.Always(
                        $"[MonsterColor] Applied {colorChannel} tag " +
                        $"'{colorTag.name}' to '{actor.name}'. " +
                        $"Refreshed {refreshedItems} tactical items. " +
                        $"Shader parameter: '{colorTag.ShaderParamName}'.");
                }

                TFTVLogger.Always(
                    $"[MonsterColor] Finished. Changed {changedActors} of " +
                    $"{matchingActors.Count} matching actors.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
    Command = "monster_color_list_actors",
    Description = "Lists tactical actors that can be selected by monster_color.")]
        public static void ListMonsterColorActors(IConsole console)
        {
            try
            {
                TacticalLevelController level =
                    GameUtl.CurrentLevel()?.GetComponent<TacticalLevelController>();

                if (level?.Map == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] This command must be used during a tactical mission.");
                    return;
                }

                foreach (TacticalActor actor in level.Map
                    .GetActors<TacticalActor>()
                    .Where(actor => actor != null)
                    .OrderBy(actor => actor.name))
                {
                    int itemCount =
                        actor.AddonsManager?.RootAddon == null
                            ? 0
                            : actor.AddonsManager.RootAddon
                                .OfType<TacticalItem>()
                                .Count();

                    TFTVLogger.Always(
                        $"Actor: '{actor.name}', " +
$"AddonsManager: {actor.AddonsManager != null}, " +
$"tactical items: {itemCount}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
    Command = "monster_color_list_tags",
    Description = "Usage: monster_color_list_tags [optionalNameContains]")]
        public static void ListMonsterColorTags(
    IConsole console,
    params string[] nameParts)
        {
            try
            {
                DefRepository repository =
                    GameUtl.GameComponent<DefRepository>();

                if (repository == null)
                {
                    TFTVLogger.Always(
                        "[MonsterColor] DefRepository is unavailable.");
                    return;
                }

                string filter =
                    nameParts == null || nameParts.Length == 0
                        ? string.Empty
                        : string.Join(" ", nameParts).Trim();

                IEnumerable<CustomizationColorTagDef> tags =
                    repository
                        .GetAllDefs<CustomizationColorTagDef>()
                        .Where(tag =>
                            tag != null &&
                            (string.IsNullOrEmpty(filter) ||
                             tag.name.IndexOf(
                                 filter,
                                 StringComparison.OrdinalIgnoreCase) >= 0))
                        .OrderBy(tag => tag.GetType().Name)
                        .ThenBy(tag => tag.name);

                int count = 0;

                foreach (CustomizationColorTagDef tag in tags)
                {
                    string channel;

                    if (tag is CustomizationPrimaryColorTagDef)
                    {
                        channel = "primary";
                    }
                    else if (tag is CustomizationSecondaryColorTagDef)
                    {
                        channel = "secondary";
                    }
                    else
                    {
                        channel = tag.GetType().Name;
                    }

                    TFTVLogger.Always(
                        $"[MonsterColor] {channel}: '{tag.name}', " +
                        $"shader parameter: '{tag.ShaderParamName}'");

                    count++;
                }

                TFTVLogger.Always(
                    $"[MonsterColor] Listed {count} matching color tags.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }


        private static readonly LeaderSelection.AffinityApproach[] AllAffinityApproaches =
        {
            LeaderSelection.AffinityApproach.PsychoSociology,
            LeaderSelection.AffinityApproach.Exploration,
            LeaderSelection.AffinityApproach.Occult,
            LeaderSelection.AffinityApproach.Biotech,
            LeaderSelection.AffinityApproach.Machinery,
            LeaderSelection.AffinityApproach.Compute
        };

        [ConsoleCommand(Command = "checkcrates", Description = "tell me what's inside the crates")]
        public static void SayHello(IConsole console)
        {
            TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

            foreach (TacticalActorBase actor in tacticalLevelController.Map.GetActors<TacticalActorBase>())
            {
                TFTVLogger.Always($"{actor?.name}");

                if (actor is CrateItemContainer crate)
                {
                    foreach (Item item in crate.Inventory.Items)
                    {
                        TFTVLogger.Always($"item in crate is {item.ItemDef.name}");
                    }
                }
            }
        }

        [ConsoleCommand(
            Command = "list_affinity_ops",
            Description = "Lists Phoenix operative IDs for affinity testing.")]
        public static void ListAffinityOperatives(IConsole console)
        {
            try
            {
                GeoLevelController level = GetCurrentGeoLevel();
                if (level?.PhoenixFaction?.Characters == null)
                {
                    TFTVLogger.Always("[AffinityTest] Geoscape level not available.");
                    return;
                }

                foreach (GeoCharacter operative in level.PhoenixFaction.Characters
                    .Where(c => c != null)
                    .OrderBy(c => c.Id))
                {
                    TFTVLogger.Always($"[AffinityTest] ID {operative.Id}: {GetOperativeName(operative)}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
            Command = "aff_psycho",
            Description = "Usage: aff_psycho <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyPsychoSociologyAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.PsychoSociology,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_exploration",
            Description = "Usage: aff_exploration <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyExplorationAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Exploration,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_occult",
            Description = "Usage: aff_occult <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyOccultAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Occult,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_biotech",
            Description = "Usage: aff_biotech <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyBiotechAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Biotech,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_machinery",
            Description = "Usage: aff_machinery <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyMachineryAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Machinery,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "aff_compute",
            Description = "Usage: aff_compute <operativeId> <rank 1-3> <geoOption 1-2> <tacticalOption 1-2>")]
        public static void ApplyComputeAffinity(IConsole console, int operativeId, int rank, int geoOption, int tacticalOption)
        {
            ApplyAffinityCommand(
                LeaderSelection.AffinityApproach.Compute,
                operativeId,
                rank,
                geoOption,
                tacticalOption);
        }

        [ConsoleCommand(
            Command = "incident_list",
            Description = "Lists available incident IDs.")]
        public static void ListIncidents(IConsole console)
        {
            try
            {
                if (!EnsureIncidentDefinitionsAvailable())
                {
                    TFTVLogger.Always("[IncidentTest] Incident definitions are not available.");
                    return;
                }

                foreach (Objects.GeoIncidentDefinition incident in GeoscapeEvents.IncidentDefinitions
                    .Where(i => i != null && i.IntroEvent != null)
                    .OrderBy(i => i.Id))
                {
                    string factionShortName = incident.FactionDef != null && incident.FactionDef.PPFactionDef != null
                        ? incident.FactionDef.PPFactionDef.ShortName
                        : "ANY";

                    TFTVLogger.Always($"[IncidentTest] {incident.Id} ({factionShortName}) -> {incident.IntroEvent.EventID}");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        [ConsoleCommand(
            Command = "incident_trigger",
            Description = "Usage: incident_trigger <incidentId> [siteNameContains]")]
        public static void TriggerIncident(IConsole console, int incidentId, params string[] siteNameParts)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    TFTVLogger.Always("[IncidentTest] Base Rework is disabled.");
                    return;
                }

                GeoLevelController level = GetCurrentGeoLevel();
                if (level == null)
                {
                    TFTVLogger.Always("[IncidentTest] This command must be used in geoscape.");
                    return;
                }

                string siteNameFilter = siteNameParts == null || siteNameParts.Length == 0
                    ? string.Empty
                    : string.Join(" ", siteNameParts).Trim();

                if (!Roll.TryTriggerIncident(level, incidentId, siteNameFilter))
                {
                    string suffix = string.IsNullOrEmpty(siteNameFilter) ? string.Empty : $" for site filter '{siteNameFilter}'";
                    TFTVLogger.Always($"[IncidentTest] Failed to trigger incident {incidentId}{suffix}.");
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool EnsureIncidentDefinitionsAvailable()
        {
            if (GeoscapeEvents.IncidentDefinitions != null && GeoscapeEvents.IncidentDefinitions.Count > 0)
            {
                return true;
            }

            GeoscapeEvents.CreateGeoscapeEvents();
            return GeoscapeEvents.IncidentDefinitions != null && GeoscapeEvents.IncidentDefinitions.Count > 0;
        }

        /// Injcecting the mods console commands to the base game console handler
        public static void InjectConsoleCommands()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                foreach (MethodInfo methodInfo in types[i].GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute(methodInfo, typeof(ConsoleCommandAttribute)) is ConsoleCommandAttribute consoleCommandAttribute)
                    {
                        if (!methodInfo.IsPublic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not public."
                            }));
                        }
                        if (!methodInfo.IsStatic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not static."
                            }));
                        }
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0 || !typeof(IConsole).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that does not have something implementing IConsole as first argument."
                            }));
                        }
                        int k = 1;
                        int num = parameters.Length;
                        while (k < num)
                        {
                            ParameterInfo parameterInfo = parameters[k];
                            if (k == parameters.Length - 1 && parameterInfo.ParameterType.IsArray && parameterInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length != 0 && parameterInfo.ParameterType.GetElementType() == typeof(string))
                            {
                                typeof(ConsoleCommandAttribute).GetField("_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, true);
                            }
                            else if (!TypeToConvertFunc.ContainsKey(parameterInfo.ParameterType))
                            {
                                throw new InvalidOperationException(string.Concat(new string[]
                                {
                                    "ConsoleCommandAttribute is defined on method ",
                                    methodInfo.DeclaringType.FullName,
                                    ".",
                                    methodInfo.Name,
                                    " that has a parameter ",
                                    parameterInfo.Name,
                                    " that is of unsupported type."
                                }));
                            }
                            k++;
                        }
                        typeof(ConsoleCommandAttribute).GetField("_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, methodInfo);
                        string key = consoleCommandAttribute.Command ?? methodInfo.Name;

                        // get access to the base game private static field of the console command handler to inject all commands from this mod
                        // Original: ConsoleCommandAttribute.CommandToInfo[key] = consoleCommandAttribute;
                        SortedList<string, ConsoleCommandAttribute> BaseCommandToInfo = (SortedList<string, ConsoleCommandAttribute>)typeof(ConsoleCommandAttribute).GetField("CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                        BaseCommandToInfo[key] = consoleCommandAttribute;
                    }
                }
            }
        }

        private static void ApplyAffinityCommand(
            LeaderSelection.AffinityApproach approach,
            int operativeId,
            int rank,
            int geoOption,
            int tacticalOption)
        {
            try
            {
                if (!BaseReworkCheck.BaseReworkEnabled)
                {
                    TFTVLogger.Always("[AffinityTest] Base Rework is disabled.");
                    return;
                }

                GeoLevelController level = GetCurrentGeoLevel();
                if (level?.PhoenixFaction?.Characters == null)
                {
                    TFTVLogger.Always("[AffinityTest] This command must be used in geoscape.");
                    return;
                }

                GeoCharacter operative = level.PhoenixFaction.Characters.FirstOrDefault(c => c != null && c.Id == operativeId);
                if (operative == null)
                {
                    TFTVLogger.Always($"[AffinityTest] No operative found with ID {operativeId}. Use list_affinity_ops first.");
                    return;
                }

                PassiveModifierAbilityDef abilityToAdd = GetAffinityAbilityForRank(approach, rank, out int normalizedRank);
                if (abilityToAdd == null)
                {
                    TFTVLogger.Always($"[AffinityTest] Could not resolve affinity data for {approach}.");
                    return;
                }

                int removedAbilities = RemoveAllAffinityAbilities(operative);

                if (operative.Progression != null && !operative.Progression.Abilities.Contains(abilityToAdd))
                {
                    operative.Progression.AddAbility(abilityToAdd);
                }

                int normalizedGeoOption = NormalizeOption(geoOption);
                int normalizedTacticalOption = NormalizeOption(tacticalOption);

                Affinities.AffinityBenefitsChoices.SetGeoscapeBenefitChoice(level, approach, normalizedGeoOption);
                Affinities.AffinityBenefitsChoices.SetTacticalBenefitChoice(level, approach, normalizedTacticalOption);
                Affinities.AffinityBenefitsChoices.CaptureTacticalBenefitChoiceSnapshot(level);
                Affinities.AffinityBenefitsChoices.RefreshTacticalAbilityDescriptionsFromSnapshot();

                TFTVLogger.Always(
                    $"[AffinityTest] Applied {approach} rank {normalizedRank} to {GetOperativeName(operative)} (ID {operative.Id}). " +
                    $"Geo option {normalizedGeoOption}, tactical option {normalizedTacticalOption}, removed {removedAbilities} existing affinity ability entries.");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static GeoLevelController GetCurrentGeoLevel()
        {
            try
            {
                return GameUtl.CurrentLevel()?.GetComponent<GeoLevelController>();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        private static int NormalizeOption(int option)
        {
            return option == 2 ? 2 : 1;
        }

        private static PassiveModifierAbilityDef GetAffinityAbilityForRank(
            LeaderSelection.AffinityApproach approach,
            int rank,
            out int normalizedRank)
        {
            normalizedRank = Math.Max(1, Math.Min(3, rank));
            PassiveModifierAbilityDef[] track = GetAffinityTrack(approach);

            if (track == null || track.Length < normalizedRank)
            {
                return null;
            }

            return track[normalizedRank - 1];
        }

        private static PassiveModifierAbilityDef[] GetAffinityTrack(LeaderSelection.AffinityApproach approach)
        {
            switch (approach)
            {
                case LeaderSelection.AffinityApproach.PsychoSociology:
                    return Affinities.PsychoSociology;
                case LeaderSelection.AffinityApproach.Exploration:
                    return Affinities.Exploration;
                case LeaderSelection.AffinityApproach.Occult:
                    return Affinities.Occult;
                case LeaderSelection.AffinityApproach.Biotech:
                    return Affinities.Biotech;
                case LeaderSelection.AffinityApproach.Machinery:
                    return Affinities.Machinery;
                case LeaderSelection.AffinityApproach.Compute:
                    return Affinities.Compute;
                default:
                    return null;
            }
        }

        private static int RemoveAllAffinityAbilities(GeoCharacter operative)
        {
            try
            {
                if (operative?.Progression == null)
                {
                    return 0;
                }

                List<TacticalAbilityDef> abilities = Traverse.Create(operative.Progression)
                    .Field("_abilities")
                    .GetValue<List<TacticalAbilityDef>>();

                if (abilities == null)
                {
                    return 0;
                }

                int removed = 0;

                foreach (LeaderSelection.AffinityApproach approach in AllAffinityApproaches)
                {
                    PassiveModifierAbilityDef[] track = GetAffinityTrack(approach);
                    if (track == null || track.Length == 0)
                    {
                        continue;
                    }

                    removed += abilities.RemoveAll(ability =>
                        ability != null && track.Any(def => def == ability));
                }

                return removed;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return 0;
            }
        }

        private static string GetOperativeName(GeoCharacter operative)
        {
            if (operative == null)
            {
                return "UNKNOWN";
            }

            if (!string.IsNullOrEmpty(operative.DisplayName))
            {
                return operative.DisplayName;
            }

            return operative.GetName();
        }

        public static readonly Dictionary<Type, Func<string, object>> TypeToConvertFunc = new Dictionary<Type, Func<string, object>>
        {
            {
                typeof(sbyte),
                (string v) => sbyte.Parse(v)
            },
            {
                typeof(short),
                (string v) => short.Parse(v)
            },
            {
                typeof(int),
                (string v) => int.Parse(v)
            },
            {
                typeof(long),
                (string v) => long.Parse(v)
            },
            {
                typeof(byte),
                (string v) => byte.Parse(v)
            },
            {
                typeof(ushort),
                (string v) => ushort.Parse(v)
            },
            {
                typeof(uint),
                (string v) => uint.Parse(v)
            },
            {
                typeof(ulong),
                (string v) => ulong.Parse(v)
            },
            {
                typeof(float),
                (string v) => float.Parse(v)
            },
            {
                typeof(double),
                (string v) => double.Parse(v)
            },
            {
                typeof(string),
                (string v) => v
            },
            {
                typeof(bool),
                delegate(string v)
                {
                    float num;
                    if (float.TryParse(v, out num))
                    {
                        return num != 0f;
                    }
                    return bool.Parse(v);
                }
            }
        };
    }
}
