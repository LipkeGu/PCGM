Imports System.IO

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
    Private _path_to_motd As String = My.Application.Info.DirectoryPath & "\conf\motd.txt"
    Private _woltimezone As String = "5"
    Private _multiserver As String = "0"


    Public Sub INIT()
        If Not Directory.Exists(My.Application.Info.DirectoryPath & "\conf") Then
            Directory.CreateDirectory(My.Application.Info.DirectoryPath & "\conf")
        End If

        If Not File.Exists(My.Application.Info.DirectoryPath & "\conf\config.ini") Then
            Dim cfg_lines As String = "[MAIN]" & vbCrLf & _
                "Network-Address = " & NetworkAddress & vbCrLf & _
                "Network-Name = " & NetworkAddress & vbCrLf & _
                "GameServer-Port = " & GameServer_port & vbCrLf & _
                "ManglerServer-Port = " & ManglerServer_port & vbCrLf & _
                "LadderServer-Port = " & LadderServer_port & vbCrLf & _
                "TicketServer-Port = " & TicketServer_port & vbCrLf & _
                "WOL-Timezone = " & wol_timezone & vbCrLf & _
                "WOL-Multiserver = " & multiserver & vbCrLf

            File.WriteAllText(My.Application.Info.DirectoryPath & "\conf\config.ini", cfg_lines)
        End If

        If File.Exists(My.Application.Info.DirectoryPath & "\conf\config.ini") Then
            Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\config.ini")

            NetworkAddress = ini_func.WertLesen("MAIN", "Network-Address")
            GameServer_port = ini_func.WertLesen("MAIN", "GameServer-Port")
            ManglerServer_port = ini_func.WertLesen("MAIN", "ManglerServer-Port")
            LadderServer_port = ini_func.WertLesen("MAIN", "LadderServer-Port")
            Networkname = ini_func.WertLesen("MAIN", "Network-Name")
            TicketServer_port = ini_func.WertLesen("MAIN", "TicketServer-Port")
            wol_timezone = ini_func.WertLesen("MAIN", "WOL-Timezone")
            multiserver = ini_func.WertLesen("MAIN", "WOL-Multiserver")
        Else
            NetworkAddress = _networkaddr
            Networkname = _networkname
            GameServer_port = _Gamesrv_port
            GameResServer_port = _Gameres_port
            ManglerServer_port = _Mangler_port
            TicketServer_port = _Ticketsrv_port
            wol_timezone = _woltimezone
            multiserver = _multiserver


        End If

        If Not File.Exists(_path_to_motd) Then
            Dim motd_lines As String = "Welcome to " & Networkname & " " & vbCrLf
            File.WriteAllText(_path_to_motd, motd_lines)
        End If

        chatserv.network_adress = NetworkAddress
        chatserv.StartServer(CInt(GameServer_port))
        chatserv.StartServer(4000) 'WOLV1 must run on Port 4000!
        chatserv.ServerName = Networkname
        chatserv._motdfile = MOTDFILE
        chatserv._timezone = wol_timezone

        ladderserv.StartServer(CInt(LadderServer_port))
        GameResserv.StartServer(CInt(GameResServer_port))
    End Sub

#Region "Propertys"

    Public Property multiserver As String
        Get
            Return _multiserver
        End Get
        Set(value As String)
            _multiserver = value
        End Set
    End Property

    Public Property wol_timezone As String
        Get
            Return _woltimezone
        End Get
        Set(value As String)
            _woltimezone = value
        End Set
    End Property

    Public Property TicketServer_port As String
        Get
            Return _Ticketsrv_port
        End Get
        Set(value As String)
            _Ticketsrv_port = value
        End Set
    End Property

    Public Property ManglerServer_port As String
        Get
            Return _Mangler_port
        End Get
        Set(value As String)
            _Mangler_port = value
        End Set
    End Property

    Public Property GameResServer_port As String
        Get
            Return _Gameres_port
        End Get
        Set(value As String)
            _Gameres_port = value
        End Set
    End Property

    Public Property MOTDFILE As String
        Get
            Return _path_to_motd
        End Get
        Set(value As String)
            _path_to_motd = value
        End Set
    End Property

    Public Property Networkname As String
        Get
            Return _networkname
        End Get
        Set(value As String)
            _networkname = value
        End Set
    End Property

    Public Property GameServer_port As String
        Get
            Return _Gamesrv_port
        End Get
        Set(value As String)
            _Gamesrv_port = value
        End Set
    End Property

    Public Property LadderServer_port As String
        Get
            Return _Ladderserv_port
        End Get
        Set(value As String)
            _Ladderserv_port = value
        End Set
    End Property

    Public Property NetworkAddress As String
        Get
            Return _networkaddr
        End Get
        Set(value As String)
            _networkaddr = value
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
#End Region

End Class
