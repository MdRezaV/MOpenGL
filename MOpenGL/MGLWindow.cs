#nullable disable

#define FPS         // FPS
#define CONSOLE     // Console Log

using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Timer = System.Timers.Timer;

namespace MGLCC;

public class MGLWindow : GameWindow
{
    public MGLWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
#if DEBUG
        var timer = new Timer()
        {
            Enabled = true,
            Interval = 1000, // 1 SEC
        };
        timer.Elapsed += (o, e) =>
        {
            Trace.WriteLine($"fps : {fps}");
            fps = 0;
        };
#endif
    }
#if DEBUG
    private int fps;
#endif

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        float[] vertices =
        {
            -0.5F, 0.5F,
            0.5F, 0.5F,
            0.5F, -0.5F,
            -0.5F, -0.5F,
        };

        // upload vertices to gpu memory
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // draw
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, vertices.Length);

        defaultShader.Use();

        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        SwapBuffers();
#if DEBUG
        fps++;
#endif
    }

    // ---------- Shaders ----------

    private IShader defaultShader;

    // -----------------------------

    // ---------- Providers ----------

    private IProvider defaultProvider;

    // -----------------------------

    protected override void OnLoad()
    {
        base.OnLoad();

        // ---------- Providers ----------

        defaultProvider = new MGL2D();

        // -----------------------------

        // ---------- Shaders ----------

        defaultShader = ShaderLoader.Load(
            new ShaderFile("default.vert", ShaderType.VertexShader),
            new ShaderFile("default.frag", ShaderType.FragmentShader));

        // -----------------------------
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        defaultProvider.Close();

        // ---------- Shaders ----------

        defaultShader.Close();

        // -----------------------------
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}

public interface IProvider
{
    public void Close();
}

public class MGL2D : IProvider, IDisposable
{
    public MGL2D()
    {
        VertexArrayObject = GL.GenVertexArray();
        VertexBufferObjects = GL.GenBuffer();
        GL.BindVertexArray(VertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObjects);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    public int VertexArrayObject { get; set; }
    public int VertexBufferObjects { get; set; }

    public void Close()
    {
        GL.DeleteBuffer(VertexBufferObjects);
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}


public class MGL3D : IProvider, IDisposable
{
    public MGL3D()
    {
        VertexArrayObject = GL.GenVertexArray();
        VertexBufferObjects = GL.GenBuffer();
        GL.BindVertexArray(VertexArrayObject);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObjects);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    public int VertexArrayObject { get; set; }
    public int VertexBufferObjects { get; set; }

    public void Close()
    {
        GL.DeleteVertexArray(VertexArrayObject);
        GL.DeleteBuffer(VertexBufferObjects);
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}

public interface IShader
{
    public int ShaderProgram { get; set; }
    public void Compile(bool close = true);
    public void Use();
    public void Close();
}
public class InlineShader : IShader
{
    private const string VERTEX_SHADER =
@"

#version 330 core
layout (location = 0) in vec3 aPos;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 1.0, 1.0);
}

";

    private const string FRAGMENT_SHADER =
@"

#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
}
";

    public InlineShader() => Compile(false);

    public int ShaderProgram { get; set; }
    public void Compile(bool close = true)
    {
        if (close) Close();

        var vertShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertShader, VERTEX_SHADER);
        GL.CompileShader(vertShader);

        var fragShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragShader, FRAGMENT_SHADER);
        GL.CompileShader(fragShader);
#if CONSOLE
        GL.GetShader(vertShader, ShaderParameter.CompileStatus, out var isOK0);
        GL.GetShader(fragShader, ShaderParameter.CompileStatus, out var isOK1);
        if (isOK0 == 0 || isOK1 == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Errors\n{");
            if (isOK0 == 0) Console.Write("\t(VERT)  " + GL.GetShaderInfoLog(vertShader));
            if (isOK1 == 0) Console.Write("\t(FRAG)  " + GL.GetShaderInfoLog(fragShader));
            Console.WriteLine("}");
            Console.ForegroundColor = ConsoleColor.White;
        }
#endif
        ShaderProgram = GL.CreateProgram();
        GL.AttachShader(ShaderProgram, vertShader);
        GL.AttachShader(ShaderProgram, fragShader);
        GL.LinkProgram(ShaderProgram);

        GL.DeleteShader(vertShader);
        GL.DeleteShader(fragShader);
    }
    public void Close() => GL.DeleteProgram(ShaderProgram);
    public void Use() => GL.UseProgram(ShaderProgram);
}

public class ManualShader : IShader
{
    public ManualShader() => Compile(false);

    public int ShaderProgram { get; set; }
    public void Compile(bool close = true)
    {
        if (close) Close();
        ShaderProgram = GL.CreateProgram();
    }
    public void Close() => GL.DeleteProgram(ShaderProgram);
    public void Use() => GL.UseProgram(ShaderProgram);
}

public static class ShaderLoader
{
    public static IShader Load(params ShaderFile[] shaders)
    {
        var output = new ManualShader();
        var errors = false;
        var shaderLocations = new int[shaders.Length];
        for (var i = 0; i < shaders.Length; i++)
        {
            try
            {
                var shader = shaders[i];
                var assembly = Assembly.GetExecutingAssembly();
                var resourceContent = "";
                var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(shader.FileName));
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream)) resourceContent = reader.ReadToEnd();

                shaderLocations[i] = GL.CreateShader(shader.Type);
                var currShader = shaderLocations[i];
                GL.ShaderSource(currShader, resourceContent);
                GL.CompileShader(currShader);
#if CONSOLE
                GL.GetShader(currShader, ShaderParameter.CompileStatus, out var isOK);
                if (isOK == 0)
                {
                    if (!errors)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Errors\n{");
                        errors = true;
                    }
                    Console.Write($"\t({shader.FileName})  " + GL.GetShaderInfoLog(currShader));
                }
#endif
                GL.AttachShader(output.ShaderProgram, currShader);
            }
            catch (Exception ex)
            {
#if CONSOLE
                if (!errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Errors\n{");
                    errors = true;
                }
                Console.WriteLine($"\t(Exception)  " + ex);
#endif
            }
        }

        GL.LinkProgram(output.ShaderProgram);
        foreach (var shader in shaderLocations) GL.DeleteShader(shader);
#if CONSOLE
        if (errors)
        {
            Console.WriteLine("}");
            Console.ForegroundColor = ConsoleColor.White;
        }
        GL.GetProgram(output.ShaderProgram, GetProgramParameterName.ActiveUniforms, out var num);
        for (int i = 0; i < num; i++)
        {
            if (i == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Uniforms\n{");
            }
            GL.GetActiveUniform(output.ShaderProgram, i, 20, out _, out var size, out var type, out var name);
            var addr = GL.GetUniformLocation(output.ShaderProgram, name);
            if (i % 2 == 0)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
            }
            Console.WriteLine($"\t{i,-20}{addr,-20}{name,-20}{type,-20}{size,-10:N0}");
        }
        if (num > 0)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

#endif
        return output;
    }
}

public class ShaderFile
{
    public ShaderFile() { }
    public ShaderFile(string fileName, ShaderType type)
    {
        FileName = fileName;
        Type = type;
    }

    public string FileName { get; set; }
    public ShaderType Type { get; set; }
}

