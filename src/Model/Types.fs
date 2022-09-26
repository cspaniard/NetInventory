namespace Model

open System.Runtime.InteropServices

module Types =

    //------------------------------------------------------------------------------------------------
    let (| LinuxOS | WindowsOS | OtherOS |) _ =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            LinuxOS
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            WindowsOS
        else OtherOS
    //------------------------------------------------------------------------------------------------
