using Godot;
using ImGuiNET;
using System;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace ImGuiGodot;

public static class ImGuiGD
{
    /// <summary>
    /// Deadzone for all axes
    /// </summary>
    public static float JoyAxisDeadZone { get; set; } = 0.15f;

    /// <summary>
    /// Swap the functionality of the activate (face down) and cancel (face right) buttons
    /// </summary>
    public static bool JoyButtonSwapAB { get; set; } = false;

    /// <summary>
    /// Try to calculate how many pixels squared per point. Should be 1 or 2 on non-mobile displays
    /// </summary>
    public static int DpiFactor => Math.Max(1, DisplayServer.ScreenGetDpi() / 96);

    /// <summary>
    /// Adjust the scale based on <see cref="DpiFactor"/>
    /// </summary>
    public static bool ScaleToDpi { get; set; } = true;

    /// <summary>
    /// Setting this property will reload fonts and modify the ImGuiStyle
    /// </summary>
    public static float Scale
    {
        get => _scale;
        set
        {
            if (_scale != value && value >= 0.25f)
            {
                _scale = value;
                RebuildFontAtlas();
            }
        }
    }
    private static float _scale = 1.0f;

    public static IntPtr BindTexture(Texture2D tex)
    {
        return (IntPtr)tex.GetRid().Id;
    }

    [Obsolete("UnbindTexture is no longer necessary")]
    public static void UnbindTexture(IntPtr texid)
    {
    }

    [Obsolete("UnbindTexture is no longer necessary")]
    public static void UnbindTexture(Texture2D tex)
    {
    }

    public static void Init(float? scale = null, RendererType renderer = RendererType.RenderingDevice)
    {
        if (IntPtr.Size != sizeof(ulong))
        {
            throw new PlatformNotSupportedException("imgui-godot requires 64-bit pointers");
        }

        if (scale != null)
        {
            _scale = scale.Value;
        }

        // fall back to Canvas in OpenGL compatibility mode
        if (renderer == RendererType.RenderingDevice && RenderingServer.GetRenderingDevice() == null)
        {
            renderer = RendererType.Canvas;
        }

        Internal.State.Init(renderer switch
        {
            RendererType.Dummy => new Internal.DummyRenderer(),
            RendererType.Canvas => new Internal.CanvasRenderer(),
            RendererType.RenderingDevice => new Internal.RdRenderer(),
            _ => throw new ArgumentException("Invalid renderer", nameof(renderer))
        });
    }

    public static void ResetFonts()
    {
        Internal.Fonts.ResetFonts();
    }

    public static void AddFont(FontFile fontData, int fontSize, bool merge = false)
    {
        Internal.Fonts.AddFont(fontData, fontSize, merge);
    }

    public static void AddFontDefault()
    {
        Internal.Fonts.AddFont(null, 13, false);
    }

    public static void RebuildFontAtlas()
    {
        Internal.Fonts.RebuildFontAtlas(ScaleToDpi ? Scale * DpiFactor : Scale);
    }

    public static void Update(double delta, Viewport vp)
    {
        var io = ImGui.GetIO();
        var vpSize = vp.GetVisibleRect().Size;
        io.DisplaySize = new(vpSize.x, vpSize.y);
        io.DeltaTime = (float)delta;

        Internal.Input.Update(io);

        ImGui.NewFrame();
    }

    public static void Render(Viewport vp)
    {
        Internal.State.Render(vp);
    }

    public static void Shutdown()
    {
        Internal.State.Renderer.Shutdown();
        if (ImGui.GetCurrentContext() != IntPtr.Zero)
            ImGui.DestroyContext();
    }

    /// <summary>
    /// EXPERIMENTAL! Please report bugs, with steps to reproduce.
    /// </summary>
    public static void ExperimentalEnableViewports()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        if (OS.GetName() != "Windows")
        {
            GD.PushWarning("ImGui Viewports have issues on macOS and Linux https://github.com/ocornut/imgui/wiki/Multi-Viewports#issues");
        }

        var mainvp = ImGuiLayer.Instance.GetViewport();
        if (mainvp.GuiEmbedSubwindows)
        {
            GD.PushWarning("ImGui Viewports: 'display/window/subwindows/embed_subwindows' needs to be disabled");
            mainvp.GuiEmbedSubwindows = false;
        }
    }

    /// <returns>
    /// True if the InputEvent was consumed
    /// </returns>
    public static bool ProcessInput(InputEvent evt, Window window)
    {
        return Internal.Input.ProcessInput(evt, window);
    }

    /// <summary>
    /// Extension method to translate between <see cref="Key"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this Key key)
    {
        return Internal.Input.ConvertKey(key);
    }

    /// <summary>
    /// Extension method to translate between <see cref="JoyButton"/> and <see cref="ImGuiKey"/>
    /// </summary>
    public static ImGuiKey ToImGuiKey(this JoyButton button)
    {
        return Internal.Input.ConvertJoyButton(button);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGBA
    /// </summary>
    public static Vector4 ToVector4(this Color color)
    {
        return new Vector4(color.r, color.g, color.b, color.a);
    }

    /// <summary>
    /// Convert <see cref="Color"/> to ImGui color RGB
    /// </summary>
    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }

    /// <summary>
    /// Convert RGB <see cref="Vector3"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector3 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z);
    }

    /// <summary>
    /// Convert RGBA <see cref="Vector4"/> to <see cref="Color"/>
    /// </summary>
    public static Color ToColor(this Vector4 vec)
    {
        return new Color(vec.X, vec.Y, vec.Z, vec.W);
    }

    /// <summary>
    /// Set IniFilename, converting Godot path to native
    /// </summary>
    public static void SetIniFilename(this ImGuiIOPtr io, string fileName)
    {
        Internal.State.SetIniFilename(io, fileName);
    }
}

public enum RendererType
{
    Dummy,
    Canvas,
    RenderingDevice
}
