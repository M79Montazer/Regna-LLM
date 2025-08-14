// Program.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Usage: Launcher.exe [backend.exe] [game.exe] [portFileName]
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string backendExe = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : Path.Combine(exeDir, @"bin\temp.exe");
        string gameExe = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : Path.Combine(exeDir, @"Regna_RPG\game.exe");
        string portFile = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : Path.Combine(exeDir, @"Regna_RPG\backend_port.txt");

        Console.WriteLine("Launcher starting... (console may be hidden depending on build settings)");

        if (!File.Exists(backendExe))
        {
            Console.Error.WriteLine($"Backend not found: {backendExe}");
            return 2;
        }
        if (!File.Exists(gameExe))
        {
            Console.Error.WriteLine($"Game exe not found: {gameExe}");
            return 3;
        }

        int port = GetFreePort();
        Console.WriteLine($"Picked port {port} for backend.");

        var backendProc = StartBackend(backendExe, port);
        if (backendProc == null)
        {
            Console.Error.WriteLine("Failed to start backend process.");
            return 4;
        }

        bool up = await WaitForPortOpenAsync("127.0.0.1", port, TimeSpan.FromSeconds(15));
        if (!up)
        {
            Console.Error.WriteLine($"Backend did not start listening on port {port} in time.");
            try { backendProc.Kill(true); } catch { }
            return 5;
        }

        // Write port file for the game to read
        try
        {
            File.WriteAllText(portFile, port.ToString());
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to write port file: {ex}");
        }

        // Launch game (use shell execute so it behaves like a normal app)
        var gameInfo = new ProcessStartInfo(gameExe)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(gameExe) ?? exeDir
        };

        Console.WriteLine("Starting game...");
        var gameProc = Process.Start(gameInfo);
        if (gameProc == null)
        {
            Console.Error.WriteLine("Failed to start game exe.");
            try { backendProc.Kill(true); } catch { }
            return 6;
        }

        // Wait for the game to exit
        Console.WriteLine("Game started. Waiting for game process to exit...");
        await Task.Run(() => gameProc.WaitForExit());

        // Game exited — clean up
        Console.WriteLine("Game exited. Shutting down backend...");
        try
        {
            if (!backendProc.HasExited)
            {
                backendProc.Kill(true);
                backendProc.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error stopping backend: " + ex);
        }

        // Optionally remove port file
        try { File.Delete(portFile); } catch { }

        Console.WriteLine("Launcher finished.");
        return 0;
    }

    static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    static Process? StartBackend(string path, int port)
    {
        var workingDir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        var psi = new ProcessStartInfo(path)
        {
            UseShellExecute = false,   // required for CreateNoWindow
            CreateNoWindow = true,     // hide console window
            WorkingDirectory = workingDir
        };

        // Set environment variable so ASP.NET Core Kestrel binds to the chosen port:
        // ASPNETCORE_URLS=http://127.0.0.1:{port}
        psi.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";

        // Alternatively you could pass "--urls" argument if your backend reads it:
        // psi.Arguments = $"--urls http://127.0.0.1:{port}";

        try
        {
            return Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error starting backend: " + ex);
            return null;
        }
    }

    static async Task<bool> WaitForPortOpenAsync(string host, int port, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(host, port);
                var finished = await Task.WhenAny(connectTask, Task.Delay(500));
                if (finished == connectTask && tcp.Connected)
                {
                    return true;
                }
            }
            catch
            {
                // swallow
            }
            await Task.Delay(250);
        }
        return false;
    }
}
