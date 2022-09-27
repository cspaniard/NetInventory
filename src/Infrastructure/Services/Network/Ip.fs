namespace Services.Network.Ip

open System.Runtime.InteropServices
open System.Threading.Tasks
open Motsoft.Util

open Model.Types

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker
type private IIpBroker = Infrastructure.DI.Brokers.NetworkDI.IIpBroker

type Service () =

    //----------------------------------------------------------------------------------------------------
    static member getNameInfoForIpAsyncTry ip =

        //------------------------------------------------------------------------------------------------
        let processDataLinux (stdOutData : string[]) (exitCode : int) =

            if exitCode <> 0 then
                (ip, "")
            else
                let hostFullName = (stdOutData[0] |> split "=")[1] |> trim
                let hostName = (hostFullName |> split ".")[0]
                (ip, hostName)
        //------------------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------------------
        let processDataWindows (stdOutData : string[]) (stdErrData : string[]) =

            if stdErrData.Length > 0 then
                (ip, "")
            else
                let hostFullName = (stdOutData[3] |> split ":")[1] |> trim
                let hostName = (hostFullName |> split ".")[0]
                (ip, hostName)
        //------------------------------------------------------------------------------------------------

        backgroundTask {
            let! stdOutdata, stdErrData, exitCode =
                IProcessBroker.startProcessAndReadAllLinesAsyncTry "nslookup" ip

            return
                match RuntimeInformation.OSArchitecture with
                | LinuxOS -> processDataLinux stdOutdata exitCode
                | WindowsOS -> processDataWindows stdOutdata stdErrData
                | OtherOS -> (ip, "")
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllNameInfosInNetworkAsyncTry network =

        [ for i in 1..254 -> $"%s{network}{i}" ]
        |> List.map Service.getNameInfoForIpAsyncTry
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllIpStatusInNetworkAsyncTry network =

        [ for i in 1..254 -> IIpBroker.pingIpAsync $"%s{network}{i}" ]
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworksListAsyncTry () =
        IIpBroker.getIpV4NetworkClassesAsyncTry ()
    //----------------------------------------------------------------------------------------------------
