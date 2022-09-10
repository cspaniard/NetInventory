namespace Services.Data.NetworkData

open System
open System.Collections.Generic
open System.IO
open System.Text.Encodings.Web
open System.Text.Json
open Motsoft.Util
open Model.Constants

type private IDataBroker = Infrastructure.DI.Brokers.FileSystemDI.IDataBroker
type private IIpService = Infrastructure.DI.Services.NetworkDI.IIpService

//----------------------------------------------------------------------------------------------------
type private IpInfo = {
    Ip : string
    Name : string
    Description : string
    IpIsActive : bool
}

//----------------------------------------------------------------------------------------------------
type Service () =

    //----------------------------------------------------------------------------------------------------
    static let getJsonSerializerOptions() =
        JsonSerializerOptions(WriteIndented = true, IgnoreReadOnlyProperties = false,
                              Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static let ipInfoToArray ipInfo =

        let ipInfoArray = Array.create<string> (COL_MAX_VAL + 1) ""

        ipInfoArray[COL_IP] <- ipInfo.Ip
        ipInfoArray[COL_NAME] <- ipInfo.Name
        ipInfoArray[COL_DESCRIPTION] <- ipInfo.Description
        ipInfoArray[COL_IP_IS_ACTIVE] <- ipInfo.IpIsActive.ToString()

        ipInfoArray
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static let ipInfoFromArray (ipInfoArray : string[]) =

        {
            Ip = ipInfoArray[COL_IP]
            Name = ipInfoArray[COL_NAME]
            Description = ipInfoArray[COL_DESCRIPTION]
            IpIsActive = ipInfoArray[COL_IP_IS_ACTIVE] |> Boolean.Parse
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworkDataAsyncTry network =

        task {
            let! rawData = IDataBroker.loadNetworkFileAsync network

            return
                JsonSerializer.Deserialize<ResizeArray<IpInfo>>(rawData)
                |> Seq.map ipInfoToArray
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member storeNetworkDataAsyncTry network (data : #seq<string[]>) =

        task {
            let ipInfos = data |> Seq.map ipInfoFromArray
            let jsonData = JsonSerializer.Serialize(ipInfos, getJsonSerializerOptions())

            do! IDataBroker.saveNetworkFileAsync network jsonData
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member createBlankNetworkData network =

        seq {
            for i in 1..254 do
                { Ip = $"%s{network}{i}" ; Name = "" ; Description = "" ; IpIsActive = false }
                |> ipInfoToArray
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworkFromFileName (fileName : string) =

        (Path.GetFileNameWithoutExtension fileName) + "."
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getAllNetworksDataAsyncTry () =

        backgroundTask {
            let allNetworksData = Dictionary<string, seq<string[]>>()

            let storedNetworks =
                IDataBroker.getDataFullFileNamesTry()
                |> Array.map Service.getNetworkFromFileName

            for network in storedNetworks do
                let! networkData = Service.getNetworkDataAsyncTry network
                allNetworksData.Add(network, networkData)

            let! adapterNetworks = IIpService.getNetworksAsyncTry()

            adapterNetworks
            |> Array.filter (allNetworksData.ContainsKey >> not)
            |> Array.iter (fun network -> allNetworksData.Add(network, Service.createBlankNetworkData network))

            return allNetworksData
        }
    //----------------------------------------------------------------------------------------------------
