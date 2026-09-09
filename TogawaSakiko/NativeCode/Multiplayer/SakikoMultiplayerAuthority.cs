using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace TogawaSakiko.NativeCode.Multiplayer;

/// <summary>Authority for player input only. Deterministic card and hook effects must execute on every peer.</summary>
public static class SakikoMultiplayerAuthority
{
    public static bool CanSubmitPlayerAction(Player player)
    {
        var service = RunManager.Instance.NetService;
        return service.Type switch
        {
            NetGameType.Singleplayer => true,
            NetGameType.Host or NetGameType.Client => service.IsConnected && LocalContext.IsMe(player),
            _ => false
        };
    }
}
