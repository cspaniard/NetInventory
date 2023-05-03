namespace Brokers.Network.Ip

open System.Net
open System.Net.NetworkInformation
open Motsoft.Util

open Model.Types
open Brokers.Network.Ip.Exceptions

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker

type Broker () =

    //----------------------------------------------------------------------------------------------------
    static member getIpStatusAsync (ip: string) =

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

            return (ip, (resultStatus = IPStatus.Success)) |> IpStatus
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
                                     |> join "."
                                     |> fun s -> s + ".")
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getMacForIpAsync (ip : string) =

        backgroundTask {
            let! mac = IPAddress.Parse ip
                       |> ArpLookup.Arp.LookupAsync

            return mac.ToString()
        }
    //----------------------------------------------------------------------------------------------------
