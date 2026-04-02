using System;
using System.Diagnostics;
using System.IO;

class Program {
    static void Main(string[] args) {
        if (args.Length == 0) return;
        var psi = new ProcessStartInfo {
            FileName = "git",
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        File.WriteAllText("git_output.txt", "Exit Code: " + process.ExitCode + "\n\nSTDOUT:\n" + output + "\n\nSTDERR:\n" + error);
    }
}
