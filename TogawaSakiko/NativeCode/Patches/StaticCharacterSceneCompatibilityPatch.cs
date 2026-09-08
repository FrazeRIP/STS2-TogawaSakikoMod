using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace TogawaSakiko.NativeCode.Patches;

[HarmonyPatch]
internal static class SakikoStaticMerchantCompatibilityPatch
{
    private const string SakikoMerchantNodeName = "TogawaSakikoMerchant";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))
            ?? throw new MissingMethodException(typeof(NMerchantCharacter).FullName, nameof(NMerchantCharacter._Ready));
        yield return AccessTools.Method(
                typeof(NMerchantCharacter),
                nameof(NMerchantCharacter.PlayAnimation),
                new[] { typeof(string), typeof(bool) })
            ?? throw new MissingMethodException(typeof(NMerchantCharacter).FullName, nameof(NMerchantCharacter.PlayAnimation));
    }

    private static bool Prefix(NMerchantCharacter __instance)
    {
        return !string.Equals(__instance.Name.ToString(), SakikoMerchantNodeName, StringComparison.Ordinal);
    }
}

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.FlipX))]
internal static class SakikoStaticRestSiteFlipPatch
{
    private const string SakikoRestSiteNodeName = "TogawaSakikoRestSite";

    private static bool Prefix(NRestSiteCharacter __instance)
    {
        if (!string.Equals(__instance.Name.ToString(), SakikoRestSiteNodeName, StringComparison.Ordinal))
        {
            return true;
        }

        Node2D visuals = __instance.GetNode<Node2D>("%Visuals");
        visuals.Scale = new Vector2(-visuals.Scale.X, visuals.Scale.Y);
        visuals.Position = new Vector2(-visuals.Position.X, visuals.Position.Y);

        Control controlRoot = __instance.GetNode<Control>("ControlRoot");
        controlRoot.Scale = new Vector2(-controlRoot.Scale.X, controlRoot.Scale.Y);
        return false;
    }
}
