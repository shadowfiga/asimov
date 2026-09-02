using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphite.Engine.Platform;

internal static class WindowIcon
{
    public static void Apply(GameWindow window, GraphicsDevice graphicsDevice, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("The game.icon file configured in settings.json was not found.", path);

        using var stream = File.OpenRead(path);
        using var texture = Texture2D.FromStream(graphicsDevice, stream);
        var colors = new Color[texture.Width * texture.Height];
        var rgba = new byte[colors.Length * 4];
        texture.GetData(colors);

        for (var index = 0; index < colors.Length; index++)
        {
            var offset = index * 4;
            rgba[offset] = colors[index].R;
            rgba[offset + 1] = colors[index].G;
            rgba[offset + 2] = colors[index].B;
            rgba[offset + 3] = colors[index].A;
        }

        var redMask = BitConverter.IsLittleEndian ? 0x000000ffu : 0xff000000u;
        var greenMask = BitConverter.IsLittleEndian ? 0x0000ff00u : 0x00ff0000u;
        var blueMask = BitConverter.IsLittleEndian ? 0x00ff0000u : 0x0000ff00u;
        var alphaMask = BitConverter.IsLittleEndian ? 0xff000000u : 0x000000ffu;
        var pinnedPixels = GCHandle.Alloc(rgba, GCHandleType.Pinned);

        try
        {
            var surface = SdlCreateRgbSurfaceFrom(
                pinnedPixels.AddrOfPinnedObject(),
                texture.Width,
                texture.Height,
                32,
                texture.Width * 4,
                redMask,
                greenMask,
                blueMask,
                alphaMask);

            if (surface == IntPtr.Zero)
                throw new InvalidOperationException($"SDL could not create the window icon surface: {GetSdlError()}");

            try
            {
                SdlSetWindowIcon(window.Handle, surface);
            }
            finally
            {
                SdlFreeSurface(surface);
            }
        }
        catch (DllNotFoundException exception)
        {
            throw new InvalidOperationException("Graphite could not load SDL2 to apply game.icon.", exception);
        }
        catch (EntryPointNotFoundException exception)
        {
            throw new InvalidOperationException("Graphite could not access the SDL2 window-icon API.", exception);
        }
        finally
        {
            pinnedPixels.Free();
        }
    }

    private static string GetSdlError()
        => Marshal.PtrToStringUTF8(SdlGetError()) ?? "unknown SDL error";

    [DllImport("SDL2", EntryPoint = "SDL_CreateRGBSurfaceFrom", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SdlCreateRgbSurfaceFrom(
        IntPtr pixels,
        int width,
        int height,
        int depth,
        int pitch,
        uint redMask,
        uint greenMask,
        uint blueMask,
        uint alphaMask);

    [DllImport("SDL2", EntryPoint = "SDL_SetWindowIcon", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlSetWindowIcon(IntPtr window, IntPtr icon);

    [DllImport("SDL2", EntryPoint = "SDL_FreeSurface", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlFreeSurface(IntPtr surface);

    [DllImport("SDL2", EntryPoint = "SDL_GetError", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SdlGetError();
}
