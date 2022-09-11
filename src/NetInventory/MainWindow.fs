namespace NetInventory

open System
// open GLib        // Necesario. Ambiguedad entre System.Object y GLib.Object
// open Gdk
open System.ComponentModel
open Gtk

open Motsoft.Binder
open Motsoft.Binder.BindingProperties
open Motsoft.Util
// open MainWindowConstants
open Model.Constants

type MainWindow(WindowIdName : string) as this =
    inherit BaseWindow(WindowIdName)

    [<Literal>]
    let VERSION = "0.0.1"
    //----------------------------------------------------------------------------------------------------

    // Referencias a controles

    let MainLabel = this.Gui.GetObject("MainLabel") :?> Label
    // let FileToSearchEntry = this.Gui.GetObject("FileToSearchEntry") :?> SearchEntry
    let SearchButton = this.Gui.GetObject("SearchButton") :?> Button
    // let IpsListTree = this.Gui.GetObject("IpsListTree") :?> TreeView
    let IpsListStore = this.Gui.GetObject("IpsListStore") :?> ListStore
    let NetworksListStore = this.Gui.GetObject("NetworksListStore") :?> ListStore
    let NetworksComboBox = this.Gui.GetObject("NetworksComboBox") :?> ComboBox

    let VM = MainWindowVM(IpsListStore, NetworksListStore)
    let binder = Binder(VM)

    do
        let negateBool (value : Object) _ = value :?> Boolean |> not :> Object

        binder
            .AddBinding(MainLabel, "label", nameof VM.MainMessage, OneWay)
            .AddBinding(SearchButton, "sensitive", nameof VM.IsScanning, OneWay, negateBool)
            .AddVmPropertyCallBack(nameof VM.ErrorMessage, this.ErrorMessageCallBack)
        |> ignore

        task {
            do! VM.InitAsync()
            do! VM.InitNetworksAsync()

            NetworksComboBox.Active <- 0
        }
        |> ignore


        //------------------------------------------------------------------------------------------------
        // Prepara y muestra la ventana.
        //------------------------------------------------------------------------------------------------
        this.ThisWindow.Title <- $"{this.ThisWindow.Title} - {VERSION}"
        // this.ThisWindow.Maximize()
        this.EnableCtrlQ()

        this.ThisWindow.Show()

    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Funcionalidad General
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getListStoreIter (index : int) (listStore : ListStore) =

        let mutable treeIter = TreeIter()
        let result = listStore.GetIter(&treeIter, new TreePath(index |> string))
        (result, treeIter)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let getNetworksSelectedValue () =

        // TODO: Eliminar y poner en el VM con un binding NetworksComboBox.Active
        let _, treeIter = getListStoreIter NetworksComboBox.Active NetworksListStore
        NetworksListStore.GetValue(treeIter, 0) :?> string
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let refreshIpColumnColors () =

        let setIpColumnColor treeIter =
            let ipColor =
                if Boolean.Parse(IpsListStore.GetValue(treeIter, COL_IP_IS_ACTIVE) |> string)
                then "white"
                else "gray"

            IpsListStore.SetValue(treeIter, COL_IP_COLOR_NAME, ipColor)

        let mutable loop, treeIter = getListStoreIter 0 IpsListStore

        while loop do
            setIpColumnColor treeIter
            loop <- IpsListStore.IterNext(&treeIter)
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ErrorMessageCallBack (_ : PropertyChangedEventArgs) =

        if VM.ErrorMessage |> (not << String.IsNullOrWhiteSpace) then
            this.ErrorDialogBox VM.ErrorMessage
            VM.ErrorMessage <- ""
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Eventos.
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Responde al cierre de la ventana.
    // Como es la ventana principal, también cierra la aplicación.
    //----------------------------------------------------------------------------------------------------
    member _.OnMainWindowDelete (_ : Object) (args : DeleteEventArgs) =

        args.RetVal <- true
        Application.Quit()

    //----------------------------------------------------------------------------------------------------
    // Click en botón SearchButton.
    //----------------------------------------------------------------------------------------------------
    member _.SearchButtonClicked (_ : Object) (_ : EventArgs) =

        task {
            do! VM.ScanNetworkAsync (getNetworksSelectedValue())

            refreshIpColumnColors()
        }
        |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.DescriptionEdited (_ : Object) (args : EditedArgs) =

        VM.UpdateRowDescription args.Path args.NewText

        // TODO: Quitar de aquí. para que se haga en VM.
        VM.UpdateNetworkData (getNetworksSelectedValue())
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.NetworksComboBoxChanged (_ : Object) (_ : EventArgs) =

        if NetworksComboBox.Active >= 0 then
            getNetworksSelectedValue()
            |> VM.LoadNetworkData

            refreshIpColumnColors()
    //----------------------------------------------------------------------------------------------------
