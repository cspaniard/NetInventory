namespace Brokers.FileSystem.Data

open System.IO
open Motsoft.Util
open Brokers.FileSystem.Data.Exceptions

type IPathBroker = Brokers.FileSystem.Path.Broker

type Broker () =

    //----------------------------------------------------------------------------------------------------
    static member loadNetworkFileAsync network =

        Path.Combine(IPathBroker.dataFolder, network + "json")
        |> File.ReadAllTextAsync
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member saveNetworkFileAsync network data =

        let fullPathName = Path.Combine(IPathBroker.dataFolder, network + "json")
        File.WriteAllTextAsync(fullPathName, data)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getDataFullFileNamesTry () =

        try
            Directory.GetFiles(IPathBroker.dataFolder, "*.json")
        with _ -> failwith DATA_FILES_ERROR
    //----------------------------------------------------------------------------------------------------