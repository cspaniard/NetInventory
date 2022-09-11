namespace NetInventory

open Gtk
open System.Collections.Generic
open Motsoft.Binder.NotifyObject

type private IIpService = Infrastructure.DI.Services.NetworkDI.IIpService
type private INetworkDataService = Infrastructure.DI.Services.DataDI.INetworkDataService

open Model.Constants

type MainWindowVM(IpListStore : ListStore, NetworksListStore : ListStore) as this =
    inherit NotifyObject()

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
            // TODO: add try/with - ErrorMessage
            let! results = IIpService.getNameInfoInNetworkAsyncTry network

            let mutable treeIter = TreeIter()

            if IpListStore.GetIter(&treeIter, new TreePath("0")) then
                results
                |> Array.iter (fun (_, name) -> IpListStore.SetValue(treeIter, COL_NAME, name)
                                                IpListStore.IterNext(&treeIter) |> ignore)
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member val NetworksData = Unchecked.defaultof<Dictionary<string, seq<string[]>>> with get, set
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.Init() =

        // TODO: Evaluar lo bueno/malo de esta solución.
        backgroundTask {
            try
                let! data = INetworkDataService.getAllNetworksDataAsyncTry()
                this.NetworksData <- data
            with e -> printfn $"%A{e}"
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.InitNetworksAsync() =

        task {
            // TODO: Try With - ErrorMessage
            let! networks = IIpService.getNetworksAsyncTry()

            NetworksListStore.Clear()

            networks
            |> Array.iter (fun n -> NetworksListStore.AppendValues [| n |] |> ignore)
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
    member _.ScanNetworkAsyncTry network =

        backgroundTask {
            do! scanIpsInNetworkAsync network
            do! getDnsNamesInNetworkAsyncTry network

            this.UpdateNetworkData network
        }
    //----------------------------------------------------------------------------------------------------
