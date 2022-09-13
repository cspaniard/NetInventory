namespace NetInventory

open System.Diagnostics
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
    let mutable networksActiveIdx = -1
    let mutable errorMessage = ""

    // TODO: Comprobar si instanciando el diccionario vacio, no necesitamos el método Init.
    let mutable networksData = Unchecked.defaultof<Dictionary<string, seq<string[]>>>
    //----------------------------------------------------------------------------------------------------


    // let getIpSuffix ipString = (ipString |> split ".")[3]

    //----------------------------------------------------------------------------------------------------
    let getListStoreIter (index : int) (listStore : ListStore) =

        let mutable treeIter = TreeIter()
        let result = listStore.GetIter(&treeIter, new TreePath(index |> string))
        (result, treeIter)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getSelectedNetwork () =

        let _, treeIter = getListStoreIter this.NetworksActiveIdx NetworksListStore
        NetworksListStore.GetValue(treeIter, 0) :?> string
    //----------------------------------------------------------------------------------------------------

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

        let fillIpData treeIter (ipData : (string * bool)[]) =
            let mutable treeIter = treeIter

            ipData
            |> Array.iter (fun (ip, isActive) ->
                               IpListStore.SetValue(treeIter, COL_IP, ip)
                               IpListStore.SetValue(treeIter, COL_IP_IS_ACTIVE, isActive)
                               IpListStore.IterNext(&treeIter) |> ignore)

        task {
            let! ipData = IIpService.getAllIpStatusInNetworkAsync network

            match getListStoreIter 0 IpListStore with
            | true, treeIter -> ipData |> fillIpData treeIter
            | false, _ -> this.ErrorMessage <- "Error rellenando la información de las IPs."
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getDnsNamesInNetworkAsyncTry network =

        let fillNameInfoData treeIter (namesInfo : (string * string)[]) =
            let mutable treeIter = treeIter

            namesInfo
            |> Array.iter (fun (_, name) -> IpListStore.SetValue(treeIter, COL_NAME, name)
                                            IpListStore.IterNext(&treeIter) |> ignore)

        task {
            let! nameInfoData = IIpService.getNameInfoInNetworkAsyncTry network

            match getListStoreIter 0 IpListStore with
            | true, treeIter -> nameInfoData |> fillNameInfoData treeIter
            | false, _ -> this.ErrorMessage <- "Error rellenando los nombres de los dispositivos."
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Properties
    //----------------------------------------------------------------------------------------------------
    member _.MainMessage
        with get() = mainMessage
        and set value = if mainMessage <> value then mainMessage <- value ; this.NotifyPropertyChanged()

    member _.IsScanning
        with get() = isScanning
        and set value = if isScanning <> value then isScanning <- value ; this.NotifyPropertyChanged()

    member _.NetworksActiveIdx
        with get() = networksActiveIdx
        and set value = if networksActiveIdx <> value then networksActiveIdx <- value ; this.NotifyPropertyChanged()

    member _.ErrorMessage
        with get() = errorMessage
        and set value = if errorMessage <> value then errorMessage <- value ; this.NotifyPropertyChanged()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.NetworksData
        with get() = networksData
        and private set value = networksData <- value
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
                NetworksListStore.Clear()

                let! networks = IIpService.getNetworksAsyncTry()

                networks
                |> Array.iter (fun n -> NetworksListStore.AppendValues [| n |] |> ignore)
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateNetworkData network =

        task {
            this.NetworksData[network] <- getNetworkDataFromIpList ()
            do! INetworkDataService.storeNetworkDataAsyncTry network this.NetworksData[network]
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.LoadNetworkData () =

        let fillIpListStore () =
            let network = getSelectedNetwork ()

            this.NetworksData[network]
            |> Seq.iter (fun row -> IpListStore.AppendValues row |> ignore)

        IpListStore.Clear ()
        fillIpListStore ()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateIpDescription (row : string) (newText : string) =

        task {
            match getListStoreIter (row |> int) IpListStore with
            | true, treeIter ->
                  IpListStore.SetValue(treeIter, COL_DESCRIPTION, newText)
                  do! this.UpdateNetworkData (getSelectedNetwork ())
            | false, _ ->
                  this.ErrorMessage <- "No se ha podido determinar el registro a actualilzar."
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ScanNetworkAsync () =

        task {
            try
                let network = getSelectedNetwork ()
                this.IsScanning <- true
                let stopWatch = Stopwatch.StartNew()

                this.MainMessage <- "Escaneando Red..."
                do! scanIpsInNetworkAsync network

                this.MainMessage <- "Resolviendo Nombres..."
                do! getDnsNamesInNetworkAsyncTry network

                do! this.UpdateNetworkData network

                stopWatch.Stop()
                this.IsScanning <- false
                this.MainMessage <- $"Listo: {stopWatch.ElapsedMilliseconds} ms"
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------
