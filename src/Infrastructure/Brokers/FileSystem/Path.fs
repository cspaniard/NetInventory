namespace Brokers.FileSystem.Path

open System
open System.IO
open System.Reflection
open Brokers.FileSystem.Path.Exceptions
open Motsoft.Util

type private IProcessBroker = Infrastructure.DI.Brokers.ProcessesDI.IProcessBroker

type Broker () =
    //----------------------------------------------------------------------------------------------------
    static let _homeFolder = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
    static let _dataFolder = Path.Combine(_homeFolder, ".local/share", Assembly.GetEntryAssembly().GetName().Name)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member homeFolder with get() = _homeFolder
    static member dataFolder with get() = _dataFolder
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member createDataFolder () =
        Directory.CreateDirectory Broker.dataFolder |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getDataFullFileNamesTry () =
        try
            Directory.GetFiles(Broker.dataFolder, "*.json")
        with _ -> failwith PATH_DATA_FILES_ERROR
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member getNetworkFromFileName (fileName : string) =
        (Path.GetFileName fileName |> String.split ".")[0] + "."
    //----------------------------------------------------------------------------------------------------
