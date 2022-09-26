namespace NetInventory

open System
// open GLib        // Necesario. Ambiguedad entre System.Object y GLib.Object
// open Gdk
open System.ComponentModel
open Gtk

open Motsoft.Binder
open Motsoft.Binder.BindingProperties
open Motsoft.Util

type MainWindow(WindowIdName : string) as this =
    inherit BaseWindow(WindowIdName)

    [<Literal>]
    let VERSION = "1.4.0"

    //----------------------------------------------------------------------------------------------------
    // Referencias a controles
    //----------------------------------------------------------------------------------------------------
    let MainLabel = this.Gui.GetObject("MainLabel") :?> Label
    let ScanButton = this.Gui.GetObject("ScanButton") :?> Button
    let IpsListStore = this.Gui.GetObject("IpsListStore") :?> ListStore
    let NetworksListStore = this.Gui.GetObject("NetworksListStore") :?> ListStore
    let NetworksComboBox = this.Gui.GetObject("NetworksComboBox") :?> ComboBox
    let IpsWithDataCheckButton = this.Gui.GetObject("IpsWithDataCheckButton") :?> CheckButton
    //----------------------------------------------------------------------------------------------------

    let VM = MainWindowVM(IpsListStore, NetworksListStore)
    let binder = Binder(VM)

    do
        let negateBool (value : Object) _ = value :?> Boolean |> not :> Object

        binder
            .AddBinding(MainLabel, "label", nameof VM.MainMessage, OneWay)
            .AddBinding(ScanButton, "sensitive", nameof VM.IsScanning, OneWay, negateBool)
            .AddBinding(NetworksComboBox, "sensitive", nameof VM.IsScanning, OneWay, negateBool)
            .AddBinding(NetworksComboBox, "active", nameof VM.NetworksActiveIdx, OneWayToSource)
            .AddBinding(IpsWithDataCheckButton, "active", nameof VM.IpsWithDataOnly)
            .AddVmPropertyCallBack(nameof VM.ErrorMessage, this.ErrorMessageCallBack)
        |> ignore

        task {
            do! VM.InitAsync ()

            if NetworksListStore.IterNChildren () > 0 then
                NetworksComboBox.Active <- 0
        }
        |> ignore


        //------------------------------------------------------------------------------------------------
        // Prepara y muestra la ventana.
        //------------------------------------------------------------------------------------------------
        this.ThisWindow.Title <- $"{this.ThisWindow.Title} - {VERSION}"
        // this.ThisWindow.Maximize ()
        this.EnableCtrlQ ()

        this.ThisWindow.WidthRequest <- 1000
        this.ThisWindow.HeightRequest <- 700
        this.ThisWindow.Show ()

    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    // Funcionalidad General
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
        Application.Quit ()
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.ScanButtonClicked (_ : Object) (_ : EventArgs) =

        task {
            do! VM.ScanNetworkAsync ()
        }
        |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.DescriptionEdited (_ : Object) (args : EditedArgs) =

        task {
            do! VM.UpdateIpDescriptionAsync args.Path args.NewText
        }
        |> ignore
    //----------------------------------------------------------------------------------------------------
