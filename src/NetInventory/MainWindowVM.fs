namespace NetInventory

open System
open System.Diagnostics
open Gtk
open System.Collections.Generic
open Motsoft.Util
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
    let mutable ipsWithDataOnly = true
    let mutable errorMessage = ""

    let mutable networksData = Unchecked.defaultof<Dictionary<string, seq<string[]>>>
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getIpSuffix ipString = (ipString |> split ".")[3] |> int
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getListStoreIter (index : int) (listStore : ListStore) =

        let mutable treeIter = TreeIter()
        let result = listStore.GetIter(&treeIter, new TreePath(index |> string))
        (result, treeIter)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let scanIpsInNetworkAsync network =

        let fillIpData (ipData : (string * bool)[]) =

            let currentData = networksData[network] |> Array.ofSeq

            ipData
            |> Array.iteri (fun row (ip, isActive) ->
                                currentData[row][COL_IP] <- ip
                                currentData[row][COL_IP_IS_ACTIVE] <- isActive.ToString())

            networksData[network] <- currentData

        task {
            let! ipData = IIpService.getAllIpStatusInNetworkAsyncTry network
            fillIpData ipData
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getDnsNamesInNetworkAsyncTry network =

        let fillNameInfoData (namesInfo : (string * string)[]) =

            let currentData = networksData[network] |> Array.ofSeq

            namesInfo
            |> Array.iteri (fun row (_, name) -> currentData[row][COL_NAME] <- name)

        task {
            let! nameInfoData = IIpService.getNameInfoInNetworkAsyncTry network
            fillNameInfoData nameInfoData
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let storeNetworkDataAsyncTry network =

        task {
            do! INetworkDataService.storeNetworkDataAsyncTry network networksData[network]
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

    member _.IpsWithDataOnly
        with get() = ipsWithDataOnly
        and set value = if ipsWithDataOnly <> value then
                            ipsWithDataOnly <- value
                            this.LoadNetworkData ()
                            this.NotifyPropertyChanged()

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
    member _.SelectedNetwork
        with get() = let _, treeIter = getListStoreIter this.NetworksActiveIdx NetworksListStore
                     NetworksListStore.GetValue(treeIter, 0) :?> string
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.InitAsync () =

        let getAllNetworksDataAsyncTry () =
            task {
                let! data = INetworkDataService.getAllNetworksDataAsyncTry ()
                this.NetworksData <- data
            }

        let fillNetworsListAsyncTry () =
            task {
                let! networks = IIpService.getNetworksAsyncTry ()
                networks
                |> Array.iter (fun n -> NetworksListStore.AppendValues [| n |] |> ignore)
            }

        task {
            try
                NetworksListStore.Clear()                      // Aquí por si hay problemas, que se vacíe.
                do! getAllNetworksDataAsyncTry ()
                do! fillNetworsListAsyncTry ()
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.RecalcIpListStoreColumns () =

        let setIpColumnColor treeIter =
            let ipColor =
                if Boolean.Parse(IpListStore.GetValue(treeIter, COL_IP_IS_ACTIVE) |> string)
                then "white"
                else "gray"

            IpListStore.SetValue(treeIter, COL_IP_COLOR_NAME, ipColor)

        IpListStore.Foreach (fun _ _ treeIter -> setIpColumnColor treeIter ; false)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.LoadNetworkData () =

        let fillIpListStore () =

            let filterFun = Seq.filter (fun (row : string[]) ->
                                            row[COL_IP_IS_ACTIVE] |> bool.Parse
                                            || row[COL_NAME] |> String.IsNullOrWhiteSpace |> not
                                            || row[COL_DESCRIPTION] |> String.IsNullOrWhiteSpace |> not)

            this.NetworksData[this.SelectedNetwork]
            |> (if this.IpsWithDataOnly then filterFun else id)
            |> Seq.iter (fun row -> IpListStore.AppendValues row |> ignore)

        IpListStore.Clear ()
        fillIpListStore ()

        this.RecalcIpListStoreColumns ()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateIpDescription (row : string) (newDescription : string) =

        let selectedNetwork = this.SelectedNetwork

        let fillNetworksDataDescription treeIter newDescription =
            let networkData = this.NetworksData[selectedNetwork] |> Array.ofSeq

            let ip = IpListStore.GetValue(treeIter, COL_IP).ToString()
            let idx = (ip |> getIpSuffix) - 1

            networkData[idx][COL_DESCRIPTION] <- newDescription
            this.NetworksData[selectedNetwork] <- networkData

        task {
            try
                match getListStoreIter (row |> int) IpListStore with
                | true, treeIter ->
                      IpListStore.SetValue(treeIter, COL_DESCRIPTION, newDescription)
                      fillNetworksDataDescription treeIter newDescription

                      do! storeNetworkDataAsyncTry selectedNetwork
                | false, _ ->
                      failwith "No se ha podido determinar el registro a actualizar."
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ScanNetworkAsync () =

        task {
            try
                let network = this.SelectedNetwork
                this.IsScanning <- true
                let stopWatch = Stopwatch.StartNew()

                this.MainMessage <- "Escaneando Red..."
                do! scanIpsInNetworkAsync network

                this.MainMessage <- "Resolviendo Nombres..."
                do! getDnsNamesInNetworkAsyncTry network
                stopWatch.Stop ()

                do! storeNetworkDataAsyncTry network

                this.IsScanning <- false
                this.MainMessage <- $"Listo: {stopWatch.ElapsedMilliseconds} ms"
                this.LoadNetworkData ()
            with e -> this.ErrorMessage <- e.Message
        }
    //----------------------------------------------------------------------------------------------------
