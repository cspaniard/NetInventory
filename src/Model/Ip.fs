namespace Model

module IpInfo =

    open System
    open Model.Constants

    //----------------------------------------------------------------------------------------------------
    type IpInfo = {
        Ip : string
        Name : string
        Description : string
        IpIsActive : bool
    }
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let ipInfoToArray ipInfo =

        let ipInfoArray = Array.create<string> COL_MAX_VAL ""

        ipInfoArray[COL_IP] <- ipInfo.Ip
        ipInfoArray[COL_NAME] <- ipInfo.Name
        ipInfoArray[COL_DESCRIPTION] <- ipInfo.Description
        ipInfoArray[COL_IP_IS_ACTIVE] <- ipInfo.IpIsActive.ToString()

        ipInfoArray
    //----------------------------------------------------------------------------------------------------

    //----------------------------------------------------------------------------------------------------
    let ipInfoFromArray (ipInfoArray : string[]) =

        {
            Ip = ipInfoArray[COL_IP]
            Name = ipInfoArray[COL_NAME]
            Description = ipInfoArray[COL_DESCRIPTION]
            IpIsActive = ipInfoArray[COL_IP_IS_ACTIVE] |> Boolean.Parse
        }
    //----------------------------------------------------------------------------------------------------
