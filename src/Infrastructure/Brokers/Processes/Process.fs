namespace Brokers.Processes.Process

open System.Diagnostics

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

            let stdOutLines = ResizeArray<string>()
            let mutable tmpLine = ""

            // Lectura del StdOut
            let! line = proc.StandardOutput.ReadLineAsync()
            tmpLine <- line

            while tmpLine <> null do
                stdOutLines.Add tmpLine
                let! line = proc.StandardOutput.ReadLineAsync()
                tmpLine <- line

            // Lectura del StdErr
            let stdErrLines = ResizeArray<string>()
            let! line = proc.StandardError.ReadLineAsync()
            tmpLine <- line

            while tmpLine <> null do
                stdErrLines.Add tmpLine
                let! line = proc.StandardError.ReadLineAsync()
                tmpLine <- line

            do! proc.WaitForExitAsync()

            return (stdOutLines.ToArray(), stdErrLines.ToArray(), proc.ExitCode)
        }
    //----------------------------------------------------------------------------------------------------
