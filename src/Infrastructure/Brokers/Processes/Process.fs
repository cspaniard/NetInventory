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
                                 FileName = processName,
                                 Arguments = arguments)
                |> Process.Start

            let lines = ResizeArray<string>()

            let mutable tmpLine = ""
            let! line = proc.StandardOutput.ReadLineAsync()
            tmpLine <- line

            while tmpLine <> null do
                lines.Add tmpLine
                let! line = proc.StandardOutput.ReadLineAsync()
                tmpLine <- line

            return lines.ToArray()
        }
    //----------------------------------------------------------------------------------------------------
