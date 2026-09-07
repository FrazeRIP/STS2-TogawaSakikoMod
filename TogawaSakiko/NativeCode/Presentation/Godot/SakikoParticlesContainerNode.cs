using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace TogawaSakiko.NativeCode.Presentation.Godot;

public partial class SakikoParticlesContainerNode : NParticlesContainer
{
    private static readonly FieldInfo ParticlesField = typeof(NParticlesContainer).GetField(
        "_particles",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(NParticlesContainer).FullName, "_particles");

    public override void _Ready()
    {
        ParticlesField.SetValue(this, new global::Godot.Collections.Array<GpuParticles2D>());
    }
}
