namespace Services.Network.Ip

open System.Threading.Tasks
open Motsoft.Util

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker
type private IIpBroker = Infrastructure.DI.Brokers.NetworkDI.IIpBroker

type Service () =

    //----------------------------------------------------------------------------------------------------
    static member getNameInfoInNetworkAsyncTry network =

        [ for i in 1..254 -> $"%s{network}{i}" ]
        |> List.map IIpBroker.getNameInfoForIpAsyncTry
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
