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
open Helpers

type MainWindow(WindowIdName : string) as this =
    inherit BaseWindow(WindowIdName)

    [<Literal>]
    let VERSION = "0.1.0"

    //----------------------------------------------------------------------------------------------------
    // Referencias a controles
    //----------------------------------------------------------------------------------------------------
    let MainLabel = this.Gui.GetObject("MainLabel") :?> Label
    let ScanButton = this.Gui.GetObject("ScanButton") :?> Button
    let IpsListStore = this.Gui.GetObject("IpsListStore") :?> ListStore
    let NetworksListStore = this.Gui.GetObject("NetworksListStore") :?> ListStore
    let NetworksComboBox = this.Gui.GetObject("NetworksComboBox") :?> ComboBox
    //----------------------------------------------------------------------------------------------------

    let VM = MainWindowVM(IpsListStore, NetworksListStore)
    let binder = Binder(VM)

    do
        let negateBool (value : Object) _ = value :?> Boolean |> not :> Object

        binder
            .AddBinding(MainLabel, "label", nameof VM.MainMessage, OneWay)
            .AddBinding(ScanButton, "sensitive", nameof VM.IsScanning, OneWay, negateBool)
            .AddBinding(NetworksComboBox, "active", nameof VM.NetworksActiveIdx, OneWayToSource)
            .AddVmPropertyCallBack(nameof VM.NetworksActiveIdx, this.NetworksActiveIdxCallBack)
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
    let refreshIpColumnColors () =

        let setIpColumnColor treeIter =
            let ipColor =
                if Boolean.Parse(IpsListStore.GetValue(treeIter, COL_IP_IS_ACTIVE) |> string)
                then "white"
                else "gray"

            IpsListStore.SetValue(treeIter, COL_IP_COLOR_NAME, ipColor)

        IpsListStore.Foreach (fun _ _ treeIter -> setIpColumnColor treeIter ; false)
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

    //----------------------------------------------------------------------------------------------------
    member _.ScanButtonClicked (_ : Object) (_ : EventArgs) =

        task {
            do! VM.ScanNetworkAsync ()
            refreshIpColumnColors ()
        }
        |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.DescriptionEdited (_ : Object) (args : EditedArgs) =

        task {
            do! VM.UpdateIpDescription args.Path args.NewText
        }
        |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.NetworksActiveIdxCallBack (_ : PropertyChangedEventArgs) =

        if NetworksComboBox.Active >= 0 then
            VM.LoadNetworkData ()
            refreshIpColumnColors ()
    //----------------------------------------------------------------------------------------------------
