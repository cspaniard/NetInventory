namespace Brokers.Processes.Process

open System.Diagnostics
open Microsoft.FSharp.Core.CompilerServices

type Broker () =

    //----------------------------------------------------------------------------------------------------
    static member startProcessTry processName arguments =

        Process.Start((processName : string), (arguments : string))
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member startAndWaitForProcessAsyncTry processName arguments =

        task {
            let proc = Broker.startProcessTry processName arguments
            do! proc.WaitForExitAsync()

            return proc
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member startProcessAndReadAllLinesAsyncTry processName arguments =

        backgroundTask {
            let proc =
                ProcessStartInfo(RedirectStandardOutput = true,
                                 RedirectStandardError = true,
                                 FileName = processName,
                                 WindowStyle = ProcessWindowStyle.Hidden,
                                 CreateNoWindow = true,
                                 UseShellExecute = false,
                                 Arguments = arguments)
                |> Process.Start

            let lines = ArrayCollector<string>()
            let mutable tmpLine = ""

            // Lectura del StdOut
            let! line = proc.StandardOutput.ReadLineAsync()
            tmpLine <- line

            while tmpLine <> null do
                lines.Add tmpLine
                let! line = proc.StandardOutput.ReadLineAsync()
                tmpLine <- line

            // Lectura del StdErr
            let! line = proc.StandardError.ReadLineAsync()
            tmpLine <- line

            while tmpLine <> null do
                lines.Add tmpLine
                let! line = proc.StandardError.ReadLineAsync()
                tmpLine <- line

            return lines.Close()            // Devolvemos StdOut y StdErr juntos.
        }
    //----------------------------------------------------------------------------------------------------
