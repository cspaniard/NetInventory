namespace Services.Network.Ip

open System.Threading.Tasks
open Motsoft.Util

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker
type private IIpBroker = Infrastructure.DI.Brokers.NetworkDI.IIpBroker

type Service () =

    //----------------------------------------------------------------------------------------------------
    static member getNameInfoForIpsAsyncTry ipList =

        ipList
        |> List.map IIpBroker.getNameInfoForIpAsyncTry
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllIpStatusInNetworkAsync network =

        [ for i in 1..254 -> IIpBroker.pingIpAsync $"{network}{i}" ]
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworksAsyncTry () =
        IIpBroker.getIpV4NetworkClassesAsyncTry ()
    //----------------------------------------------------------------------------------------------------
