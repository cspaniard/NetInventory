namespace NetInventory

open Gtk
open System.Collections.Generic
open Motsoft.Binder.NotifyObject

type private IIpService = Infrastructure.DI.Services.NetworkDI.IIpService
type private INetworkDataService = Infrastructure.DI.Services.DataDI.INetworkDataService

open Model.Constants

type MainWindowVM(IpListStore : ListStore, NetworksListStore : ListStore) as this =
    inherit NotifyObject()


    //----------------------------------------------------------------------------------------------------
    // Property Holders
    //----------------------------------------------------------------------------------------------------
    let mutable mainMessage = "Listo"
    let mutable isScanning = false
    let mutable errorMessage = ""
    //----------------------------------------------------------------------------------------------------


    // let getIpSuffix ipString = (ipString |> split ".")[3]

    //----------------------------------------------------------------------------------------------------
    let getRowValues treeIter =

        [| for i in 0..COL_MAX_VAL -> IpListStore.GetValue(treeIter, i) |> string |]
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getNetworkDataFromIpList () =

        let mutable myIter = TreeIter()
        let mutable loop = IpListStore.GetIter(&myIter, new TreePath("0"))

        [|
            while loop do
                getRowValues myIter
                loop <- IpListStore.IterNext(&myIter)
        |]
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let scanIpsInNetworkAsync network =

        task {
            let! results = IIpService.getAllIpStatusInNetworkAsync network

            let mutable treeIter = TreeIter()

            if IpListStore.GetIter(&treeIter, new TreePath("0")) then
                results
                |> Array.iter (fun (ip, isActive) ->
                                   IpListStore.SetValue(treeIter, COL_IP, ip)
                                   IpListStore.SetValue(treeIter, COL_IP_IS_ACTIVE, isActive)
                                   IpListStore.IterNext(&treeIter) |> ignore)
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getDnsNamesInNetworkAsyncTry network =

        task {
            let! results = IIpService.getNameInfoInNetworkAsyncTry network

            let mutable treeIter = TreeIter()

            if IpListStore.GetIter(&treeIter, new TreePath("0")) then
                results
                |> Array.iter (fun (_, name) -> IpListStore.SetValue(treeIter, COL_NAME, name)
                                                IpListStore.IterNext(&treeIter) |> ignore)
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Properties
    //----------------------------------------------------------------------------------------------------
    member _.MainMessage
        with get() = mainMessage
        and set value  = if mainMessage <> value then mainMessage <- value ; this.NotifyPropertyChanged()

    member _.IsScanning
        with get() = isScanning
        and set value  = if isScanning <> value then isScanning <- value ; this.NotifyPropertyChanged()

    member _.ErrorMessage
        with get() = errorMessage
        and set value  = if errorMessage <> value then errorMessage <- value ; this.NotifyPropertyChanged()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member val NetworksData = Unchecked.defaultof<Dictionary<string, seq<string[]>>> with get, set
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.InitAsync () =

        // TODO: Evaluar lo bueno/malo de esta solución.
        task {
            try
                let! data = INetworkDataService.getAllNetworksDataAsyncTry()
                this.NetworksData <- data
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.InitNetworksAsync () =

        task {
            try
                let! networks = IIpService.getNetworksAsyncTry()

                NetworksListStore.Clear()

                networks
                |> Array.iter (fun n -> NetworksListStore.AppendValues [| n |] |> ignore)
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateNetworkData network =

        this.NetworksData[network] <- getNetworkDataFromIpList ()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.LoadNetworkData network =

        IpListStore.Clear()

        this.NetworksData[network]
        |> Seq.iter (fun row -> IpListStore.AppendValues row |> ignore)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateRowDescription (row : string) (newText : string) =

        let mutable myIter = TreeIter()

        if IpListStore.GetIter(&myIter, new TreePath(row)) then
            IpListStore.SetValue(myIter, COL_DESCRIPTION, newText)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ScanNetworkAsync network =

        task {
            try
                this.IsScanning <- true
                this.MainMessage <- "Escaneando red..."
                do! scanIpsInNetworkAsync network
                do! getDnsNamesInNetworkAsyncTry network

                this.UpdateNetworkData network
                this.MainMessage <- "Listo"
                this.IsScanning <- false
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------
