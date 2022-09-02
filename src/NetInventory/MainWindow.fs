namespace NetInventory

open System
// open GLib        // Necesario. Ambiguedad entre System.Object y GLib.Object
// open Gdk
open Gtk

open Motsoft.Util

type IProcessBroker = Infrastructure.DI.ProcessesDI.IProcessBroker
type IIpService = Infrastructure.DI.NetworkDI.IIpService


type MainWindow(WindowIdName : string) as this =
    inherit BaseWindow(WindowIdName)

    [<Literal>]
    let VERSION = "0.0.1"
    //----------------------------------------------------------------------------------------------------

    // Referencias a controles

    // let MainLabel = this.Gui.GetObject("MainLabel") :?> Label
    // let FileToSearchEntry = this.Gui.GetObject("FileToSearchEntry") :?> SearchEntry
    // let SearchButton = this.Gui.GetObject("SearchButton") :?> Button
    // let NetListTree = this.Gui.GetObject("NetListTree") :?> TreeView
    let NetListStore = this.Gui.GetObject("NetListStore") :?> ListStore

    // let VM = MainWindowVM()

    do
        //------------------------------------------------------------------------------------------------
        // Prepara las columnas del FileListTree.
        //------------------------------------------------------------------------------------------------
        // let a = new CellRendererText()
        // a.Editable <- true
        // NetListTree.AppendColumn("IP", new CellRendererText(), "text", 0) |> ignore
        // NetListTree.AppendColumn("Descripción", new CellRendererText(), "text", 1) |> ignore
        //------------------------------------------------------------------------------------------------

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
            NetListStore.Clear()

            let mostrarRedes() =
                task {
                    let! ipClasses = IIpService.getIpV4NetworkClassesAsyncTry()
                    printfn $"%A{ipClasses}"
                }

            let mostrarIpsRegistradas () =
                task {
                    let! namesInfo =
                        [ for i in 1..254 -> $"192.168.1.{i}" ]
                        |> IIpService.getNameInfoForIpsAsyncTry

                    namesInfo
                    |> Seq.filter (snd >> String.IsNullOrWhiteSpace >> not)
                    |> Seq.iter (fun (ip, name) -> NetListStore.AppendValues [| ip ; name |] |> ignore)
                }

            do! mostrarRedes()
            do! mostrarIpsRegistradas()
            Console.WriteLine "Done."

        }
        |> ignore
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    member _.DescriptionEdited (_ : Object) (a : EditedArgs) =

        let mutable myIter = TreeIter()

        if NetListStore.GetIter(&myIter, new TreePath(a.Path)) then
            NetListStore.SetValue(myIter, 2, a.NewText)
    //----------------------------------------------------------------------------------------------------
