namespace NetInventory

open System
open System.Collections.Generic
open Gtk
open Motsoft.Binder.NotifyObject

type private IIpService = Infrastructure.DI.Services.NetworkDI.IIpService
type private IPathBroker = Infrastructure.DI.Brokers.FileSystemDI.IPathBroker


open Model.IpInfo
open Model.Constants

type MainWindowVM(IpListStore : ListStore, NetworksListStore : ListStore) as this =
    inherit NotifyObject()

    // let getIpSuffix ipString = (ipString |> split ".")[3]

    let mutable fullList = Array.empty<string[]>

    let getRowValues treeIter =

        [| for i in 0..COL_MAX_VAL -> IpListStore.GetValue(treeIter, i) |> string |]

    let rebuildFullList () =

        let mutable myIter = TreeIter()
        let mutable loop =  IpListStore.GetIter(&myIter, new TreePath("0"))

        fullList <-
            [|
                while loop do
                    getRowValues myIter
                    loop <- IpListStore.IterNext(&myIter)
            |]

    let networksDict = Dictionary<string, IpInfo list>()

    do
        let a = { Ip = "123" ; Name = "asdasd" ; Description = "xcvxvxcv" ; IpIsActive = true  }
        printfn "%A" a

        // TODO: Pruebas
        IPathBroker.getDataFullFileNamesTry()
        |> Array.map IPathBroker.getNetworkFromFileName
        |> Array.iter (fun nf -> networksDict.Add(nf, List.empty<IpInfo>))

        networksDict
        |> Seq.iter (fun kvp -> printfn $"%s{kvp.Key} - %A{kvp.Value}")

    //----------------------------------------------------------------------------------------------------
    member _.InitNetworksAsync() =

        task {
            // TODO: Try With
            let! networks = IIpService.getNetworksAsyncTry()

            NetworksListStore.Clear()

            networks
            |> Array.iter (fun n -> NetworksListStore.AppendValues [| n |] |> ignore)
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.AddRows() =

        // TODO: Eliminar esta prueba
        [1..10]
        |> List.iter (fun i -> IpListStore.AppendValues [| $"192.168.1.{i}" ; "Test" ; "Comentario." |] |> ignore)

        this.Filter()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.Filter() =

        rebuildFullList()

        IpListStore.Clear()

        fullList
        |> Array.filter (fun r -> r[COL_IP_IS_ACTIVE] |> Boolean.Parse)
        |> Array.iter (fun r -> IpListStore.AppendValues r |> ignore)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.UpdateRowDescription (row : string) (newText : string) =

        let mutable myIter = TreeIter()

        if IpListStore.GetIter(&myIter, new TreePath(row)) then
            IpListStore.SetValue(myIter, COL_DESCRIPTION, newText)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ScanAllIpsAsync (network : string) =

        task {
            let! results = IIpService.getAllIpStatusInNetworkAsync network

            results
            |> Array.iter (fun (ip, isActive) ->
                               let treeIter = IpListStore.Append()
                               IpListStore.SetValue(treeIter, COL_IP, ip)
                               IpListStore.SetValue(treeIter, COL_IP_IS_ACTIVE, isActive))
        }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.GetAllDnsNamesAsync (network : string) =

        task {
            let! results = IIpService.getNameInfoForIpsAsyncTry [ for i in 1..254 -> $"{network}{i}" ]

            let mutable treeIter = TreeIter()

            if IpListStore.GetIter(&treeIter, new TreePath("0")) then
                results
                |> Array.iter (fun (_, name) -> IpListStore.SetValue(treeIter, COL_NAME, name)
                                                IpListStore.IterNext(&treeIter) |> ignore)
        }
    //----------------------------------------------------------------------------------------------------
