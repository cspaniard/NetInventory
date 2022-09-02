namespace Services.Network.Ip

open System.Net
open System.Threading.Tasks
open Motsoft.Util
open Services.Network.Ip.Exceptions

type IProcessBroker = Infrastructure.DI.ProcessesDI.IProcessBroker

type Service () =

    //----------------------------------------------------------------------------------------------------
    static member getIpV4NetworkClassesAsyncTry () =

        backgroundTask {
            let! allIpAddresses = Dns.GetHostAddressesAsync(Dns.GetHostName())

            let ipV4Addresses =
                allIpAddresses
                |> Array.filter (fun address -> address.GetAddressBytes().Length = 4)

            ipV4Addresses.Length = 0 |> failWithIfTrue IP_NO_NETWORKS_FOUND

            return ipV4Addresses
                   |> Array.map (fun address ->
                                     address.ToString()
                                     |> split "."
                                     |> Array.take 3
                                     |> Array.fold (fun st s -> st + s + ".") "")
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNameInfoForIpAsyncTry ip =

        backgroundTask {
            let! data = IProcessBroker.startProcessAndReadAllLinesAsyncTry "nslookup" ip

            if data[0].Contains "=" then
                let hostFullName = (data[0] |> split "=")[1] |> trim
                let hostName = (hostFullName |> split ".")[0]
                return (ip, hostName)
            else
                return (ip, "")
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNameInfoForIpsAsyncTry ipList =

        ipList
        |> List.map Service.getNameInfoForIpAsyncTry
        |> Task.WhenAll
    //----------------------------------------------------------------------------------------------------
