namespace Brokers.FileSystem.Path

open System
open System.IO
open System.Reflection

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
