namespace Brokers.FileSystem.Path

open System
open System.IO
open Brokers.FileSystem.Path.Exceptions

type private IProcessBroker = Infrastructure.DI.ProcessesDI.IProcessBroker

type Broker () =
    //----------------------------------------------------------------------------------------------------
    static let _homeFolder = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
    static let _personalLaunchersFolder = Path.Combine(_homeFolder, ".local/share/applications")

    static let mutable _fileManager = ""

    static let getFileManagerAsyncTry() =

        backgroundTask {
            if _fileManager = "" then
                try
                    let! lines = IProcessBroker.startProcessAndReadAllLinesAsyncTry
                                     "xdg-mime" "query default inode/directory"

                    _fileManager <- lines[0].Split(".", StringSplitOptions.RemoveEmptyEntries)[0]
                with _ -> failwith PATH_MIME_ERROR

            return _fileManager
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    static member homeFolder = _homeFolder
    static member personalLaunchersFolder = _personalLaunchersFolder

    static member DESTINATION_ROOT = "/opt/"

    static member openFolderTry folderName =
        IProcessBroker.startProcessTry "xdg-open" folderName |> ignore

    static member openFolderAsRootAsyncTry folderName =

        backgroundTask {
            let! fileManager = getFileManagerAsyncTry()
            IProcessBroker.startProcessTry "pkexec" $"%s{fileManager} %s{folderName}" |> ignore
        }
    //----------------------------------------------------------------------------------------------------
