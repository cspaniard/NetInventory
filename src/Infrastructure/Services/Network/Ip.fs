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
        let processDataLinux (stdOutData : string[]) =

            let hostFullName = (stdOutData[0] |> split "=")[1] |> trim
            let hostName = (hostFullName |> split ".")[0]
            (ip, hostName)
        //------------------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------------------
        let processDataWindows (stdOutData : string[]) =

            let hostFullName = (stdOutData[3] |> split ":")[1] |> trim
            let hostName = (hostFullName |> split ".")[0]
            (ip, hostName)
        //------------------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------------------
        let processData processDataFun condForEmptyVal emptyRetVal data =

            if condForEmptyVal
            then emptyRetVal
            else processDataFun data
        //------------------------------------------------------------------------------------------------

        backgroundTask {
            let! stdOutdata, stdErrData, exitCode =
                IProcessBroker.startProcessAndReadAllLinesAsyncTry "nslookup" ip

            let emptyRetVal = (ip, "")
            let processDataLinux = processData processDataLinux (exitCode <> 0) emptyRetVal
            let processDataWindows = processData processDataWindows (stdErrData.Length > 0) emptyRetVal

            return
                match RuntimeInformation.OSDescription with
                | LinuxOS -> processDataLinux stdOutdata
                | WindowsOS -> processDataWindows stdOutdata
                | OtherOS -> emptyRetVal
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllNameInfosAsyncTry network =

        [ for i in 1..254 -> Service.getNameInfoForIpAsyncTry $"%s{network}{i}" ]
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllIpStatusAsyncTry network =

        [ for i in 1..254 -> IIpBroker.getIpStatusAsync $"%s{network}{i}" ]
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getMacsForActiveIps (ipStatuses : IpStatus[])  =

        ipStatuses
        |> Array.map (fun (ip, active) -> if active
                                          then IIpBroker.getMacForIpAsync ip
                                          else Task.FromResult "")
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworksListAsyncTry () =
        IIpBroker.getIpV4NetworkClassesAsyncTry ()
    //----------------------------------------------------------------------------------------------------
