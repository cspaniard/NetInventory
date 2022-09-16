namespace Brokers.Network.Ip

open System.Net
open System.Net.NetworkInformation
open Motsoft.Util

open Brokers.Network.Ip.Exceptions

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker

type Broker () =

    //----------------------------------------------------------------------------------------------------
    static member pingIpAsync (ip: string) =

        backgroundTask {
            let ping = new Ping()
            let mutable retryCount = 3
            let mutable resultStatus = IPStatus.Unknown

            while retryCount > 0 do
                let! pingReply = ping.SendPingAsync(ip, 500)

                if pingReply.Status = IPStatus.Success then
                    resultStatus <- pingReply.Status
                    retryCount <- 0
                else
                    retryCount <- retryCount - 1

            return ip, (resultStatus = IPStatus.Success)
        }
    //----------------------------------------------------------------------------------------------------

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
