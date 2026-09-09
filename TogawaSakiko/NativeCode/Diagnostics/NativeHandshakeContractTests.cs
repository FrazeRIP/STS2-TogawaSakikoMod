using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using TogawaSakiko.NativeCode.Bootstrap;
using TogawaSakiko.NativeCode.Content;

namespace TogawaSakiko.NativeCode.Diagnostics;

/// <summary>Negative native packet contracts, separate from the real ENet transport scenarios.</summary>
internal static class NativeHandshakeContractTests
{
    internal static int Run()
    {
        PeerVersionInfo local = PeerVersionInfo.LocalDefault();
        List<string> mods = local.gameplayAffectingMods
            ?? throw new InvalidOperationException("The test package was not loaded as gameplay-affecting.");
        string manifestVersion = MegaCrit.Sts2.Core.Modding.ModManager.GetLoadedMods()
            .Single(mod => mod.manifest?.id == ModEntryPoint.ModId).manifest!.version
            ?? throw new InvalidOperationException("The loaded test package has no manifest version.");
        string ownModEntry = ModEntryPoint.ModId + "-" + manifestVersion;
        if (!MegaCrit.Sts2.Core.Debug.SemanticVersion.TryFromString(manifestVersion, out _) ||
            !mods.Contains(ownModEntry))
        {
            throw new InvalidOperationException("The native handshake requires the actual, valid loaded manifest version.");
        }
        if (!mods.Contains(NativePackageIdentity.HandshakeEntry))
        {
            throw new InvalidOperationException("The exact package identity was omitted from the native handshake.");
        }

        int assertions = 1;
        void Check(PeerVersionInfo remote, NetError? expected)
        {
            foreach (NetGameType role in new[] { NetGameType.Host, NetGameType.Client })
            {
                ProbeHandler handler = new(role);
                PacketWriter writer = new();
                HandshakeManager manager = new(handler, local, new PacketWriter());
                manager.BeginHandshakeFor(9981);
                try
                {
                    HandshakeManager.WriteHandshakeMessage(9981, remote, writer);
                    PacketReader reader = new();
                    reader.Reset(writer.Buffer.AsSpan(0, writer.BytePosition).ToArray());
                    manager.HandshakeMessageReceived(9981, reader);
                    assertions++;
                    if (handler.Success != (expected is null) || handler.Error != expected || manager.IsHandshaking(9981))
                    {
                        throw new InvalidOperationException($"Native {role} handshake expected {expected}, got {handler.Error}.");
                    }
                }
                finally
                {
                    manager.AbortHandshake(9981);
                }
            }
        }

        Check(local, null);
        PeerVersionInfo absent = local;
        absent.gameplayAffectingMods = mods.Where(mod => !mod.StartsWith(ModEntryPoint.ModId + "-", StringComparison.Ordinal)).ToList();
        Check(absent, NetError.ModMismatch);
        PeerVersionInfo otherVersion = local;
        otherVersion.gameplayAffectingMods = mods.Select(mod => mod == ownModEntry
            ? ModEntryPoint.ModId + "-v0.1.0" : mod).ToList();
        Check(otherVersion, NetError.ModMismatch);
        PeerVersionInfo otherPackage = local;
        otherPackage.gameplayAffectingMods = mods.Select(mod => mod == NativePackageIdentity.HandshakeEntry
            ? mod + "-different-content" : mod).ToList();
        Check(otherPackage, NetError.ModMismatch);
        PeerVersionInfo otherModels = local;
        otherModels.idDatabaseHash ^= 1;
        Check(otherModels, NetError.VersionMismatch);
        return assertions;
    }

    private sealed class ProbeHandler(NetGameType role) : IHandshakeHandler
    {
        public NetGameType Type => role;
        internal bool Success { get; private set; }
        internal NetError? Error { get; private set; }

        public void SendHandshakeMessage(ulong peerId, PacketWriter writer) { }
        public void HandshakeFailed(ulong peerId, NetErrorInfo info) => Error = info.GetReason();
        public void HandshakeSucceeded(ulong peerId, PeerVersionInfo versionInfo) => Success = true;
    }
}
