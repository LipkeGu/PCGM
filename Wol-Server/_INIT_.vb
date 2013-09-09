Public Class wol_main

    Dim WithEvents chatserv As New ClassLibrary1.WolServer
    Dim WithEvents ladderserv As New ClassLibrary1.LadderServer
    Dim WithEvents GameResserv As New ClassLibrary1.GameResServer

    Private _networkname As String = _networkaddr
    Private _networkaddr As String = "localhost"
    Private _Gameres_port As String = "4807"
    Private _Gamesrv_port As String = "4005"
    Private _Ladderserv_port As String = "4007"
    Private _Mangler_port As String = "4321"
    Private _Ticketsrv_port As String = "4018"
    Private _path_to_motd As String = ""

    Public Sub INIT()
        Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\config.ini")
        Set_NetworkAddress = ini_func.WertLesen("MAIN", "Network-Address")
        Set_GameServer_port = ini_func.WertLesen("MAIN", "GameServer-Port")
        Set_Ladderserver_port = ini_func.WertLesen("MAIN", "LadderServer-port")
        Set_Network_Name = ini_func.WertLesen("MAIN", "Network-Name")
        set_modfile = ini_func.WertLesen("MAIN", "motdfile-path")

        If _path_to_motd = "-" Then
            set_modfile = My.Application.Info.DirectoryPath & "\conf\motd.txt"
        End If

        chatserv.StartServer(CInt(_Gamesrv_port), Networkname, Network_Address)
        chatserv.StartServer(4000, Networkname, Network_Address)
        ladderserv.StartServer(CInt(_Ladderserv_port), Networkname, Network_Address)
        GameResserv.StartServer(CInt(_Gameres_port), Networkname, Network_Address)
    End Sub

    Public ReadOnly Property Networkname As String
        Get
            Return _networkname
        End Get
    End Property

    Public WriteOnly Property Set_Network_Name As String
        Set(value As String)
            _networkname = value
        End Set
    End Property

    Public ReadOnly Property GameServer_port As String
        Get
            Return _Gamesrv_port
        End Get
    End Property

    Private WriteOnly Property Set_GameServer_port As String
        Set(value As String)
            _Gamesrv_port = value
        End Set
    End Property

    Public ReadOnly Property LadderServer_port As String
        Get
            Return _Ladderserv_port
        End Get
    End Property

    Private WriteOnly Property Set_Ladderserver_port As String
        Set(value As String)
            _Ladderserv_port = value
        End Set
    End Property

    Public ReadOnly Property Network_Address As String
        Get
            Return _networkaddr
        End Get
    End Property

    Private WriteOnly Property Set_NetworkAddress As String
        Set(value As String)
            _networkaddr = value
        End Set
    End Property

    Public WriteOnly Property set_modfile As String
        Set(value As String)
            chatserv.set_MOTD_File = value
        End Set
    End Property

    Public Event Report_info(ByVal message As String)
    Public Event Report_error(ByVal message As String)
    Public Event Report_debug(ByVal message As String)

    Private Sub chatserv_Connection_closed(e As String) Handles chatserv.Connection_closed
        RaiseEvent Report_info("[Chat-Server]: " & e)
    End Sub

    Private Sub chatserv_Exception(ex As String) Handles chatserv.Exception
        RaiseEvent Report_error("[Chat-Server]: " & ex)
    End Sub

    Private Sub chatserv_NewConnection(e As String) Handles chatserv.NewConnection
        RaiseEvent Report_info("[Chat-Server]: " & e)
    End Sub

    Private Sub chatserv_ServerState(e As String) Handles chatserv.ServerState
        RaiseEvent Report_info("[Chat-Server]: " & e)
    End Sub

    Private Sub GameResserv_Connection_closed(e As String) Handles GameResserv.Connection_closed
        RaiseEvent Report_info("[GameRes-Server]: " & e)
    End Sub

    Private Sub GameResserv_NewConnection(e As String) Handles GameResserv.NewConnection
        RaiseEvent Report_info("[GameRes-Server]: " & e)
    End Sub

    Private Sub GameResserv_ServerState(e As String) Handles GameResserv.ServerState
        RaiseEvent Report_info("[GameRes-Server]: " & e)
    End Sub

    Private Sub ladderserv_Connection_closed(e As String) Handles ladderserv.Connection_closed
        RaiseEvent Report_info("[Ladder-Server]: " & e)
    End Sub

    Private Sub ladderserv_NewConnection(e As String) Handles ladderserv.NewConnection
        RaiseEvent Report_info("[Ladder-Server]: " & e)
    End Sub

    Private Sub ladderserv_ServerState(e As String) Handles ladderserv.ServerState
        RaiseEvent Report_info("[Ladder-Server]: " & e)
    End Sub
End Class
