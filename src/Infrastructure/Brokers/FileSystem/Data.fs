namespace Brokers.FileSystem.Data

open System.IO
open Brokers.FileSystem.Data.Exceptions

type IPathBroker = Brokers.FileSystem.Path.Broker

type Broker () =

    static do
        Directory.CreateDirectory IPathBroker.dataFolder |> ignore

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
