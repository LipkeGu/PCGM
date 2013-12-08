Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Text

Public Class WolServer
    Dim vercheck As New Versioncheck
    Dim userprovider As New AccProvider
    Dim chanprovider As New chanprovider
    Private ReadOnly rplcodes As New REPLYCODES

    Private Gamenumber As Integer = 0
    Dim ini_func As INIDatei
    Private _Listener As TcpListener
    Private lock As New Object
    Private ConnectedClients As New List(Of WolClient)
    Public Servers As New List(Of server)

    Structure server
        Dim type As String
        Dim name As String
        Dim addrr As String
        Dim port As String

        Dim lon As String
        Dim lat As String

        Dim tzone As String
        Dim unk As String
    End Structure

    Protected _srvname As String
    Protected _irc_network_fqdn As String
    Friend _motdfile As String
    Friend _timezone As String
    Friend _multiserver As String = "0"

#Region "Allgemeine Events und Handler-Events"
    Public Event handle_verchk_command(ByVal sku As String, ByVal version As String, ByVal socket As WolClient)
    Public Event handle_lobcount_command(ByVal sku As String, ByVal socket As WolClient)
    Public Event handle_whereto_command(ByVal username As String, ByVal password As String, ByVal version As String, ByVal sku As String, ByVal serial As String, ByVal socket As WolClient)
    Public Event handle_quit_command(ByVal socket As WolClient)
    Public Event handle_cvers_command(ByVal version As String, ByVal sku As String, ByVal socket As WolClient)
    Public Event handle_nick_command(ByVal nick As String, ByVal socket As WolClient)
    Public Event handle_pass_command(ByVal password As String, ByVal socket As WolClient)
    Public Event handle_serial_command(ByVal SERIAL As String, ByVal socket As WolClient)
    Public Event handle_ping_command(ByVal socket As WolClient)
    Public Event handle_pong_command(ByVal socket As WolClient)
    Public Event handle_startg_command(ByVal chan As String, ByVal names As String, ByVal socket As WolClient)
    Public Event handle_gameopt_command(ByVal chan As String, ByVal options As String, ByVal socket As WolClient)
    Public Event handle_apgar_command(ByVal APGAR As String, ByVal _unkint As String, ByVal socket As WolClient)
    Public Event handle_user_command(ByVal param As String, ByVal socket As WolClient)
    Public Event handle_setopt_command(ByVal opt1 As String, opt2 As String, ByVal socket As WolClient)
    Public Event handle_SETCODEPAGE_command(ByVal codepage As String, ByVal socket As WolClient)
    Public Event handle_GETCODEPAGE_command(ByVal codepage As String, ByVal socket As WolClient)
    Public Event handle_SETLOCALE_command(ByVal locale As String, ByVal socket As WolClient)
    Public Event handle_GETLOCALE_command(ByVal nicks As String, ByVal socket As WolClient)
    Public Event handle_SQUADINFO_command(ByVal squad As String, ByVal socket As WolClient)
    Public Event handle_JOIN_command(ByVal chan As String, ByVal pass As String, ByVal socket As WolClient)
    Public Event handle_ADVERTR_command(ByVal chan As String, ByVal socket As WolClient)
    Public Event handle_JOINGAME__WOL2_command(ByVal chan As String, ByVal min_Plr As String, ByVal maxPls As String, ByVal type As String, ByVal unk1 As String, ByVal unk2 As String, ByVal istournament As String, GameExtension As String, ByVal optpass As String, ByVal owner As Boolean, socket As WolClient)
    Public Event handle_TOPIC_command(ByVal channel As String, ByVal Topic As String, ByVal socket As WolClient)
    Public Event handle_LIST_command(ByVal type As String, gameid As String, ByVal socket As WolClient)
    Public Event handle_MOTD_command(ByVal socket As WolClient)
    Public Event handle_PART_command(ByVal channel As String, ByVal socket As WolClient)
    Public Event handle_PRIVMSG_command(ByVal from As String, ByVal target As String, ByVal socket As WolClient)
    Public Event handle_NAMES_command(ByVal channel As String, ByVal socket As WolClient)

    Public Event Exception(ByVal ex As String)
    Public Event ServerState(ByVal e As String)
    Public Event NewConnection(ByVal e As String)
    Public Event Connection_closed(ByVal msg As String)
    Public Event byteDatareceived(ByVal Message() As Byte)
    Public Event StringDatareceived(ByVal Message As String)
#End Region

    Public ReadOnly Property Num_Clients As String
        Get
            Return CStr(ConnectedClients.Count)
        End Get
    End Property

    Public Function GetunixTimeStamp() As String
        Dim t() As String = CStr((DateTime.Now - New DateTime(1970, 1, 1)).TotalMilliseconds).Split(CChar(","))
        Return t(0)
    End Function

    Public Property ServerName As String
        Get
            Return _srvname
        End Get
        Set(value As String)
            _srvname = value
        End Set
    End Property

    Public Property network_adress As String
        Get
            Return _irc_network_fqdn
        End Get

        Set(value As String)
            _irc_network_fqdn = value
        End Set
    End Property

    Public Sub StartServer(ByVal port As Integer)
        chanprovider.AddChannels(0)
        chanprovider.AddChannels(1)
        chanprovider.AddChannels(2)
        chanprovider.AddChannels(3)
        chanprovider.AddChannels(4)
        chanprovider.AddChannels(5)
        chanprovider.AddChannels(12)
        chanprovider.AddChannels(14)
        chanprovider.AddChannels(16)
        chanprovider.AddChannels(18)
        chanprovider.AddChannels(21)
        chanprovider.AddChannels(31)
        chanprovider.AddChannels(33)
        chanprovider.AddChannels(37)
        chanprovider.AddChannels(41)

        If _multiserver = "1" Then
            loadServers("605")
            loadServers("608")
            loadServers("609")
            loadServers("611")
            loadServers("612")
            loadServers("613")
            loadServers("615")
        End If

        Try
            _Listener = New TcpListener(IPAddress.Any, port)
            _Listener.ExclusiveAddressUse = False
            _Listener.Start()
            _Listener.BeginAcceptTcpClient(AddressOf _EndAccept, Nothing)

            RaiseEvent ServerState("Listening on:" & _Listener.LocalEndpoint.ToString)
        Catch ex As Exception
            RaiseEvent Exception("[Socket]: " & ex.Message)
        End Try
    End Sub

    Private Sub _EndAccept(ar As IAsyncResult)
        Dim c As New WolClient(_Listener.EndAcceptTcpClient(ar))

        With c
            AddHandler c.String_MessageReceived, AddressOf Client_String_Messagereceived
            AddHandler c.ConnectionClosed, AddressOf Client_Connection_Closed
            AddHandler c.ClientState, AddressOf Client_Message_Send
            AddHandler c.GetException, AddressOf Client_Exception
        End With

        c._remaddr = GetHostadress(c._socket.Client.RemoteEndPoint.ToString)
        c._tc = c

        If c._socket IsNot Nothing And c._stream IsNot Nothing Then
            ConnectedClients.Add(c)
            RaiseEvent NewConnection("Neue Verbindung von: " & c._remaddr)
        Else
            c = Nothing
        End If
        _Listener.BeginAcceptTcpClient(AddressOf _EndAccept, Nothing)
    End Sub

    Private Sub Client_String_Messagereceived(ByVal message As String, ByVal socket As WolClient)
        SyncLock lock
            Dim params() As String
            message = Mid(message, 1)
            Dim lines() As String = message.Split(CChar(vbCrLf))
            If message IsNot Nothing Then
                For i As Integer = 0 To lines.Length - 1 Step 1
                    If lines(i) Is Nothing Then
                        Exit Sub
                    Else
                        If lines(i).Length > 2 Then
                            params = lines(i).Split(CChar(" "))
                            RaiseEvent ServerState("Empfangen: " & lines(i))
                            Try
                                If params(0).Contains("verchk") Then
                                    If params.Length < 3 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_verchk_command(params(1), params(2), socket)
                                    End If
                                ElseIf params(0).Contains("QUIT") Then
                                    RaiseEvent handle_quit_command(socket)
                                ElseIf params(0).Contains("CVERS") Then

                                    If params.Length < 3 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_cvers_command(params(1), params(2), socket)
                                    End If
                                ElseIf params(0).Contains("PASS") Then
                                    If params.Length < 2 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_pass_command(params(1), socket)
                                    End If

                                ElseIf params(0).Contains("NICK") Then
                                    If params.Length < 2 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_nick_command(params(1), socket)
                                    End If
                                ElseIf params(0).Contains("apgar") Then
                                    If params.Length < 3 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_apgar_command(params(1), params(2), socket)
                                    End If
                                ElseIf params(0).Contains("ADVERTR") Then
                                    If params.Length < 2 Then
                                        Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                    Else
                                        RaiseEvent handle_ADVERTR_command(params(1), socket)
                                    End If
                                ElseIf params(0).Contains("MOTD") Then
                                    If socket.Is_wolhost = False Then RaiseEvent handle_MOTD_command(socket)

                                ElseIf params(0).Contains("SERIAL") Then
                                        If params.Length < 1 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_serial_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("USER") And params(0).Length < 6 Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_user_command(params(1), socket)
                                            RaiseEvent handle_MOTD_command(socket)
                                        End If
                                ElseIf params(0).Contains("lobcount") Then
                                        If params.Length < 1 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_lobcount_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("whereto") Then
                                        If params.Length > 4 Then
                                            RaiseEvent handle_whereto_command(params(1), params(2), params(3), params(4), params(5), socket)

                                        Else
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        End If
                                ElseIf params(0).Contains("SETOPT") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            Dim options() As String = params(1).Split(CChar(","))
                                            RaiseEvent handle_setopt_command(options(0), options(1), socket)
                                            options = Nothing
                                        End If
                                ElseIf params(0).Contains("SETCODEPAGE") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_SETCODEPAGE_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("GETCODEPAGE") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_GETCODEPAGE_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("PAGE") And params(0).Length < 6 Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            Throw New NotImplementedException(params(0))
                                        End If
                                ElseIf params(0).Contains("SETLOCALE") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_SETLOCALE_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("GETLOCALE") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            Dim _param As String = Replace(lines(i), params(0), "")
                                            RaiseEvent handle_GETLOCALE_command(_param, socket)
                                        End If
                                ElseIf params(0).Contains("JOIN") And params(0).Length < 6 Then
                                        If params.Length < 2 Then
                                            RaiseEvent handle_JOIN_command(params(1), "", socket)
                                        ElseIf params.Length = 3 Then
                                            RaiseEvent handle_JOIN_command(params(1), params(2), socket)
                                        End If
                                ElseIf params(0).Contains("SQUADINFO") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_SQUADINFO_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("LIST") And params(0).Length < 6 Then
                                        If params.Length = 1 Then
                                            RaiseEvent handle_LIST_command("-1", "0", socket)
                                        Else
                                            RaiseEvent handle_LIST_command(params(1), params(2), socket)
                                        End If
                                ElseIf params(0).Contains("PART") Then
                                        If params.Length < 2 Then
                                            Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                        Else
                                            RaiseEvent handle_PART_command(params(1), socket)
                                        End If
                                ElseIf params(0).Contains("JOINGAME") Then
                                        If params.Length > 6 Then
                                            RaiseEvent handle_JOINGAME__WOL2_command(params(1), params(2), params(3), params(4), params(5), params(6), params(7), params(8), params(9), True, socket)
                                        Else
                                            RaiseEvent handle_JOINGAME__WOL2_command(params(1), params(2), "", "", "", "", "", "", "", False, socket)
                                        End If

                                ElseIf params(0).Contains("GAMEOPT") Or params(0).Contains("PRIVMSG") Or params(0).Contains("TOPIC") Or params(0).Contains("STARTG") Then
                                        If params(0).Contains("GAMEOPT") Then
                                            If params.Length < 1 Then
                                                Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                            Else
                                                params(2) = lines(i).Replace(params(0), "")
                                                RaiseEvent handle_gameopt_command(params(1), params(2), socket)
                                            End If
                                        ElseIf params(0).Contains("PRIVMSG") Then
                                            If params.Length < 3 Then
                                                Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                            Else
                                                params(2) = lines(i).Replace(params(0), "")
                                                RaiseEvent handle_PRIVMSG_command(params(1), params(2), socket)
                                            End If
                                        ElseIf params(0).Contains("STARTG") Then
                                            If params.Length < 3 Then
                                                Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                            Else
                                                RaiseEvent handle_startg_command(params(1), params(2), socket)

                                            End If
                                        ElseIf params(0).Contains("TOPIC") Then
                                            If params.Length < 3 Then
                                                Throw New ArgumentException(rplcodes.ERR_NEEDMOREPARAMS & " " & params(0) & " :Not enough parameters")
                                            Else
                                                params(2) = lines(i).Replace(params(0), "")
                                                RaiseEvent handle_TOPIC_command(params(1), params(2), socket)
                                            End If
                                        End If
                                Else
                                        Throw New NotImplementedException(rplcodes.ERR_UNKNOWNCOMMAND & " " & params(0) & " :Unknown Command")
                                    End If
                            Catch ex As NotImplementedException
                                socket.Send(":" & network_adress & " " & ex.Message)
                                RaiseEvent Exception("[Command]: " & socket.Nick & " : " & ex.Message)
                                Exit Sub
                            Catch ex As ArgumentException
                                socket.Send(":" & network_adress & " " & ex.Message)
                                RaiseEvent Exception("[Command]: " & socket.Nick & " : " & ex.Message)
                                Exit Sub
                            End Try
                        End If
                    End If
                Next
            End If
        End SyncLock
    End Sub

    Private Sub Client_Connection_Closed(ByVal msg As String, ByVal ref As WolClient)
        SyncLock lock
            ConnectedClients.Remove(ref)
            RaiseEvent Connection_closed("Verbindung beendet: " & msg)
        End SyncLock
    End Sub

    Public Sub RemoveClient(ByVal socket As WolClient)
        SyncLock lock
            For Each chan As Chatchannel In chanprovider.channels
                If chan.sockets.Contains(socket) Then
                    chan.sockets.Remove(socket)
                End If
            Next

            If socket._socket IsNot Nothing Then
                socket._socket.Client.Disconnect(False)
                socket._socket.Close()
                socket._socket = Nothing
            End If

            If socket._stream IsNot Nothing Then
                socket._stream.Close()
                socket._stream.Dispose()
                socket._stream = Nothing
            End If
            ConnectedClients.Remove(socket)
            RaiseEvent Connection_closed("Verbindung beendet: " & socket.Hostname)
        End SyncLock
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String)
        socket.Send(":" & network_adress & " " & replycode)
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String, Message As String)
        socket.Send(":" & network_adress & " " & replycode & " " & Message)
    End Sub

    Sub putPage(ByVal socket As WolClient, Message As String)
        socket.Send(":" & "ChanServ" & " PAGE " & socket.Nick & " :" & Message)
    End Sub

    Sub putReply2(socket As WolClient, ByVal replycode As String, Message As String)
        socket.Send(":" & " " & replycode & " " & Message)
    End Sub

    Sub ParamReply(socket As WolClient, ByVal Command As String, parameter As String)
        socket.Send(":" & socket.hostmask & " " & Command & " " & parameter)
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String, Nickname As String, ByVal Message As String)
        socket.Send(":" & network_adress & " " & replycode & " " & Nickname & " " & Message)
    End Sub

    Private Sub WolServer_handle_ADVERTR_command(chan As String, socket As WolClient) Handles Me.handle_ADVERTR_command
        putCommand(socket, "ADVERTR", "5 " & chan & "\n\r")
        socket.Send(":" & network_adress & " ADVERTR 5 " & chan)
    End Sub

    Private Sub WolServer_handle_apgar_command(APGAR As String, _unkint As String, socket As WolClient) Handles Me.handle_apgar_command
        If Not APGAR = "oeakaaaa" Then ' Password: "" (leer)
            If socket.Registered Then
                If APGAR = userprovider.GetPassword("WOL", socket.Nick) Then
                    socket.password = APGAR
                Else
                    putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.Nick & " :Bad Password/Nickname.")
                    RemoveClient(socket)
                    Exit Sub
                End If
            Else
                userprovider.AddUser("WOL", socket.Nick, APGAR, socket)
                socket.Registered = True
            End If
        Else
            putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.Nick & " :Bad Password/Nickname.")
            RemoveClient(socket)
        End If
    End Sub

    Private Sub WolServer_handle_GETLOCALE_command(ByVal nicks As String, ByVal socket As WolClient) Handles Me.handle_GETLOCALE_command 'FIXME
        putReply(socket, rplcodes.RPL_LOCALE, socket.Nick, socket.Nick & "`" & socket.locale)
    End Sub

    Private Sub WolServer_handle_MOTD_command(socket As WolClient) Handles Me.handle_MOTD_command
        SyncLock lock
            If _motdfile <> Nothing Then
                Dim motd() As String

                If socket.Is_wolhost = True Then
                    putReply(socket, rplcodes.RPL_MOTDSTART, ":- Willkommen, " & socket.Nick & "!")
                Else
                    putReply(socket, rplcodes.RPL_MOTDSTART, ":- " & _irc_network_fqdn & " Message of the day -")
                End If

                If File.Exists(_motdfile) Then
                    motd = File.ReadAllLines(_motdfile)
                    For i As Integer = 0 To motd.Length - 1
                        putReply(socket, rplcodes.RPL_MOTD, socket.Nick, ":- " & motd(i) & "")
                    Next
                End If
            Else
                RaiseEvent Exception("MOTD-File not found!")
            End If

            putReply(socket, rplcodes.RPL_ENDOFMOTD, " :End of MOTD command")
        End SyncLock
    End Sub

    Private Sub WolServer_handle_cvers_command(version As String, sku As String, socket As WolClient) Handles Me.handle_cvers_command
        socket.Cver1 = version
        socket.Cver2 = sku
        socket.Is_wolhost = True
        socket.GameID = vercheck.GetClient_GroupbySKU("WOL", sku)
    End Sub

    Private Sub WolServer_handle_GETCODEPAGE_command(nick As String, socket As WolClient) Handles Me.handle_GETCODEPAGE_command
        putReply(socket, rplcodes.RPL_CODEPAGE, nick, nick & "`" & socket.codepage)

    End Sub

    Private Sub WolServer_handle_user_command(param As String, socket As WolClient) Handles Me.handle_user_command

        If Not socket.Registered Then
            socket.Registered = True
        End If

        RaiseEvent handle_ping_command(socket)
    End Sub

    Private Sub WolServer_handle_nick_command(nick As String, socket As WolClient) Handles Me.handle_nick_command
        SyncLock lock
            Dim suspicious_char As Boolean = False
            If nick.Length > 3 Then
                If nick.Contains(" ") Then
                    suspicious_char = True
                ElseIf nick.Contains(".") Then
                    suspicious_char = True
                ElseIf nick.Contains(",") Then
                    suspicious_char = True
                ElseIf nick.Contains("#") Then
                    suspicious_char = True
                ElseIf nick.Contains("~") Then
                    suspicious_char = True
                ElseIf nick.Contains("*") Then
                    suspicious_char = True
                ElseIf nick.Contains("+") Then
                    suspicious_char = True
                ElseIf nick.Contains("\\") Then
                    suspicious_char = True
                ElseIf nick.Contains("/") Then
                    suspicious_char = True
                ElseIf nick.Contains("!") Then
                    suspicious_char = True
                ElseIf nick.Contains("§") Then
                    suspicious_char = True
                ElseIf nick.Contains("$") Then
                    suspicious_char = True
                ElseIf nick.Contains("%") Then
                    suspicious_char = True
                ElseIf nick.Contains("&") Then
                    suspicious_char = True
                ElseIf nick.Contains("(") Then
                    suspicious_char = True
                ElseIf nick.Contains(")") Then
                    suspicious_char = True
                ElseIf nick.Contains("=") Then
                    suspicious_char = True
                ElseIf nick.Contains("?") Then
                    suspicious_char = True
                ElseIf nick.Contains("`") Then
                    suspicious_char = True
                ElseIf nick.Contains("´") Then
                    suspicious_char = True
                ElseIf nick.Contains("^") Then
                    suspicious_char = True
                ElseIf nick.Contains("<") Then
                    suspicious_char = True
                ElseIf nick.Contains(">") Then
                    suspicious_char = True
                ElseIf nick.Contains("@") Then
                    suspicious_char = True
                ElseIf nick.Contains("€") Then
                    suspicious_char = True
                ElseIf nick.Contains("[") Then
                    suspicious_char = True
                ElseIf nick.Contains("]") Then
                    suspicious_char = True
                ElseIf nick.Contains("then") Then
                    suspicious_char = True
                ElseIf nick.Contains(";") Then
                    suspicious_char = True
                ElseIf nick.Contains(":") Then
                    suspicious_char = True
                ElseIf nick.Contains("|") Then
                    suspicious_char = True
                ElseIf nick.Contains("ß") Then
                    suspicious_char = True
                ElseIf nick.Contains("facebook") Then
                    suspicious_char = True
                ElseIf nick.Contains("fuck") Then
                    suspicious_char = True
                ElseIf nick.Contains("adult") Then
                    suspicious_char = True
                ElseIf nick.Contains("westwood") Then
                    suspicious_char = True
                ElseIf nick.Contains("porn") Then
                    suspicious_char = True
                ElseIf nick.Contains("adolf") Then
                    suspicious_char = True
                ElseIf nick.Contains("4crazy") Then
                    suspicious_char = True
                ElseIf nick.Contains("redtube") Then
                    suspicious_char = True
                ElseIf nick.Contains("femal") Then
                    suspicious_char = True
                ElseIf nick.Contains("suck") Then
                    suspicious_char = True
                ElseIf nick.Contains("youporn") Then
                    suspicious_char = True
                ElseIf nick.Contains("admin") Then
                    suspicious_char = True
                ElseIf nick.Contains("support") Then
                    suspicious_char = True
                ElseIf nick.Contains("xbox") Then
                    suspicious_char = True
                ElseIf nick.Contains("asshole") Then
                    suspicious_char = True
                ElseIf nick.Contains("username") Then
                    suspicious_char = True
                ElseIf nick.Equals("jappy") Then
                    suspicious_char = True
                ElseIf nick.Equals("hitler") Then
                    suspicious_char = True
                ElseIf nick.Equals("guest") Then
                    suspicious_char = True
                ElseIf nick.Contains("XWIS") Then
                    suspicious_char = True
                ElseIf nick.Equals("linux") Then
                    suspicious_char = True
                ElseIf nick.Equals("wolbot") Then
                    suspicious_char = True
                ElseIf nick.Equals("chanserv") Then
                    suspicious_char = True
                ElseIf nick.Equals("nickserv") Then
                    suspicious_char = True
                ElseIf nick.Equals("spamserv") Then
                    suspicious_char = True
                ElseIf nick.Equals("nickname") Then
                    suspicious_char = True
                ElseIf nick.Equals("operserv") Then
                    suspicious_char = True
                ElseIf nick.Equals("server") Then
                    suspicious_char = True
                ElseIf nick.Contains("serv") Then
                    suspicious_char = True
                ElseIf nick.Contains("fake") Then
                    suspicious_char = True
                Else
                    suspicious_char = False
                End If

                If suspicious_char = True Then
                    putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.Nick & " :Nickname not allowed!")
                    RemoveClient(socket)
                Else
                    Dim _unique As Boolean = True
                    SyncLock lock
                        For Each channel As Chatchannel In chanprovider.channels
                            For Each user As WolClient In channel.sockets
                                If channel.sockets.Count > 0 Then
                                    If nick = user.Nick Then
                                        _unique = False
                                    End If
                                End If
                            Next
                        Next
                    End SyncLock

                    If Not _unique Then
                        putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.Nick & " :Nickname already in Use!")
                        RemoveClient(socket)
                    Else
                        socket.Nick = nick
                        If userprovider.DoesUserExist(nick) Then
                            socket.Registered = True
                        Else
                            socket.Registered = False
                        End If
                    End If
                End If
            Else
                putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.Nick & " :Nickname not allowed!")
                RemoveClient(socket)
            End If
        End SyncLock
    End Sub

    Private Sub WolServer_handle_pass_command(password As String, socket As WolClient) Handles Me.handle_pass_command
        'GUIDO: Man sollte es rausnehmen... da das wirkliche Password mittels Apgar übertragen wird und nicht hier... =/
        If socket.Is_wolhost Then
            If password <> "supersecret" Then
                putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.Nick & " :Bad Login!")
                RemoveClient(socket)
            End If
        End If
    End Sub

    Private Sub WolServer_handle_SETCODEPAGE_command(codepage As String, socket As WolClient) Handles Me.handle_SETCODEPAGE_command
        If codepage = "" Then codepage = "1252"

        If socket.codepage = codepage Then
            putReply(socket, rplcodes.RPL_CODEPAGESET, socket.Nick, codepage)
        Else
            socket.codepage = codepage
            putReply(socket, rplcodes.RPL_CODEPAGESET, socket.Nick, codepage)
        End If
    End Sub

    Private Sub WolServer_handle_SQUADINFO_command(squad As String, socket As WolClient) Handles Me.handle_SQUADINFO_command
        If squad = "0" Then
            putReply(socket, rplcodes.RPL_SQUADINFO, socket.Nick & " :ID does not Exist")
        End If
    End Sub

    Private Sub WolServer_handle_SETLOCALE_command(locale As String, socket As WolClient) Handles Me.handle_SETLOCALE_command
        Try
            If locale = "5" Or locale = "0" Then
                socket.locale = locale

                If socket.locale = locale Then
                    putReply(socket, rplcodes.RPL_LOCALESET, socket.Nick, locale)
                End If
            Else
                Throw New InvalidDataException("Your Location is not supported on this Server!")
            End If
        Catch ex As Exception
            RaiseEvent ServerState(socket.Nick & " benutzt einen nicht erlaubten Standort!")
            putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.Nick & " :" & ex.Message)
            RemoveClient(socket)
        End Try
    End Sub

    Private Sub WolServer_handle_setopt_command(opt1 As String, opt2 As String, socket As WolClient) Handles Me.handle_setopt_command
        socket.opt1 = opt1
        socket.opt2 = CInt(opt2)
    End Sub

    Private Sub WolServer_handle_ping_command(socket As WolClient) Handles Me.handle_ping_command
        putReply(socket, network_adress, "PONG : " & network_adress)
    End Sub

    Private Sub WolServer_handle_LIST_command(type As String, gameid As String, socket As WolClient) Handles Me.handle_LIST_command

        putReply(socket, rplcodes.RPL_LISTSTART, socket.Nick, " :Listing Channels...")
        If type = "0" Then
            GetChannelList(type, gameid, socket)

        ElseIf type = gameid Then
            GetGameList(type, gameid, socket)
        ElseIf type = "-1" Then
            putReply(socket, rplcodes.RPL_LIST, socket.Nick & " #Chat 0 0 0 388:")
        End If
        putReply(socket, rplcodes.RPL_ENDOFLIST, socket.Nick, " :End of LIST")
    End Sub

    Private Sub WolServer_handle_JOIN_command(chan As String, chanpass As String, socket As WolClient) Handles Me.handle_JOIN_command
        SyncLock lock
            Try
                Dim chan_exists As Boolean = False
                Dim chan_has_key As Boolean = False
                Dim completed As Boolean = False
                Dim already_in_chan As Boolean = False
                Dim key_is_matching As Boolean = False
                If chan.StartsWith("#") Then
                    Dim tmpchan As Chatchannel = GetChatchannelbyName(chan)

                    If tmpchan IsNot Nothing Then
                        If tmpchan.Chan_Key <> "" Then ' Chaannel passwort gesetzt ?
                            chan_has_key = True
                        Else
                            chan_has_key = False
                        End If

                        If tmpchan.Chan_Key = chanpass Then
                            key_is_matching = True
                        Else
                            key_is_matching = False
                        End If

                        If CInt(tmpchan.sockets.Count) < CInt(tmpchan.Chan_Max_Users) Then
                            If Not tmpchan.sockets.Contains(socket) Then
                                tmpchan.sockets.Add(socket)
                            Else
                                already_in_chan = True
                                Throw New InvalidDataException(rplcodes.ERR_USERONCHANNEL & " " & chan & " :You are already on that Channel" & tmpchan.Chan_Name)
                            End If
                            If socket.wolversion = "1" Then
                                ParamReply(socket, "JOIN", chan)
                            Else
                                ParamReply(socket, "JOIN :", "0," & socket.IPasLong & " " & chan)
                            End If

                            RaiseEvent handle_NAMES_command(tmpchan.Chan_Name, socket)
                            RaiseEvent handle_LIST_command(socket.gameid, socket.gameid, socket)

                            If socket.Serial = "" AndAlso socket.Serial_Report = False Then
                                putPage(socket, "Beim Login, wurde festgestellt, dass das Spiel fehlerhaft Installiert wurde...")
                                putPage(socket, "Stelle bitte sicher, dass das Spiel korrekt in der Registry eingetragen wurde!")
                                socket.Serial_Report = True
                                socket.TournamentAllowed_State = False
                            End If
                        Else
                            Throw New InvalidDataException(rplcodes.ERR_CHANNELISFULL & " " & tmpchan.Chan_Name & " :Channel is Full")
                        End If
                    Else
                        chan_exists = False
                        Throw New InvalidDataException(rplcodes.ERR_NOSUCHCHANNEL & " " & tmpchan.Chan_Name & " :No such Channel...")
                    End If
                Else
                    Throw New InvalidDataException(rplcodes.ERR_BADCHANMASK & " " & chan & " :Bad Channel Mask ")
                End If
            Catch ex As InvalidDataException
                RaiseEvent Exception("[JOIN]: " & socket.Nick & ": -> " & ex.Message)
                socket.Send(":" & network_adress & " " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Public Sub SendGamechannames(ByVal chan As Chatchannel, listtype As String, socket As WolClient)
        putReply(socket, rplcodes.RPL_NAMREPLY, socket.Nick, "* " & chan.Chan_Name & " :@" & chan.Chan_Owner.Nick & ",0," & chan.Chan_Owner.IPasLong)

        For i As Integer = 0 To chan.sockets.Count - 1
            If chan.sockets.Item(i).Nick <> chan.Chan_Owner.Nick AndAlso socket.Nick <> chan.sockets.Item(i).Nick Then
                putReply(socket, rplcodes.RPL_NAMREPLY, socket.Nick, "* " & chan.Chan_Name & " :" & chan.sockets.Item(i).Nick & ",0," & chan.sockets.Item(i).IPasLong)
            End If
        Next
        If socket.Nick <> chan.Chan_Owner.Nick Then
            putReply(socket, rplcodes.RPL_NAMREPLY, socket.Nick, "* " & chan.Chan_Name & " :" & socket.Nick & ",0," & socket.IPasLong)
        End If

        putReply(socket, rplcodes.RPL_ENDOFNAMES, socket.Nick & " " & chan.Chan_Name & " :End of Names")
    End Sub

    Private Sub WolServer_handle_PART_command(channel As String, socket As WolClient) Handles Me.handle_PART_command
        Try
            If channel.StartsWith("#") Then
                Dim _tmp_chan As Chatchannel = GetChatchannelbyName(channel)

                If _tmp_chan IsNot Nothing Then
                    If _tmp_chan.sockets.Contains(socket) Then
                        _tmp_chan.sockets.Remove(socket)

                        If _tmp_chan.Chan_Owner IsNot Nothing Then
                            If _tmp_chan.Chan_Owner.Equals(socket) Then
                                _tmp_chan.Chan_Owner = Nothing
                            End If
                        End If

                        ParamReply(socket, "PART", channel)

                        If _tmp_chan.sockets.Count = 0 AndAlso _tmp_chan.Chan_ListType <> "0" Then
                            If chanprovider.channels.Contains(_tmp_chan) Then
                                RaiseEvent ServerState("Removing Channel: " & _tmp_chan.Chan_Name)
                                chanprovider.channels.Remove(_tmp_chan)
                            End If
                        Else
                            For Each player As WolClient In _tmp_chan.sockets
                                If socket.Nick <> player.Nick Then
                                    RaiseEvent handle_NAMES_command(_tmp_chan.Chan_Name, player) ' Maybe we should rewrite this function >.<
                                End If
                            Next
                        End If

                    Else
                        Throw New InvalidDataException(rplcodes.ERR_NOSUCHNICK & " " & _tmp_chan.Chan_Name & " :User is not on that Channel")
                    End If

                Else
                    Throw New InvalidDataException(rplcodes.ERR_NOSUCHCHANNEL & " " & channel & " :No such Channel...")
                End If
            Else
                Throw New InvalidDataException(rplcodes.ERR_BADCHANMASK & " " & channel & " :Bad Channel Mask")
            End If
        Catch ex As InvalidDataException
            socket.Send(":" & network_adress & " " & ex.Message)
            RaiseEvent ServerState("[PART]: " & socket.Nick & ": -> " & ex.Message)
        End Try
    End Sub

    Private Sub WolServer_handle_quit_command(socket As WolClient) Handles Me.handle_quit_command
        putReply(socket, rplcodes.RPL_QUIT, ":QUIT")
        RemoveClient(socket)
    End Sub

    Private Sub WolServer_handle_JOINGAME__WOL2_command(chan As String, min_Plr As String, maxPls As String, type As String, unk1 As String, unk2 As String, istournament As String, GameExtension As String, optpass As String, owner As Boolean, socket As WolClient) Handles Me.handle_JOINGAME__WOL2_command
        SyncLock lock
            Try

                If owner = True AndAlso Not chanprovider.channels.Contains(GetChatchannelbyName(chan)) Then
                    Dim c As New Chatchannel

                    With c
                        .Chan_Name = chan
                        .Chan_Owner = socket
                        .Chan_Key = optpass
                        .OwnerIPasLong = socket.IPasLong
                        .Chan_IPAdress = socket.Hostname
                        .Chan_Gameid = type
                        .Chan_ListType = type
                        .Chan_Min_Users = CInt(min_Plr)
                        .Chan_Max_Users = CInt(maxPls)
                        .Is_Tournament = istournament
                        .Chan_Reserved = unk1
                        .Chan_GameEx = GameExtension
                        .sockets.Add(socket)
                    End With

                    chanprovider.channels.Add(c)

                    If chanprovider.channels.Contains(c) Then
                        Dim _line As String = c.Chan_Min_Users & " " & c.Chan_Max_Users & " " & c.Chan_Gameid & " " & c.Chan_Reserved & " " & c.Chan_Owner.Squadid & " " & c.Chan_Owner.IPasLong & " " & c.Is_Tournament & " :" & c.Chan_Name

                        ParamReply(socket, "JOINGAME", _line)
                        putReply(socket, rplcodes.RPL_TOPIC, c.Chan_Topic)

                        If c.Chan_Owner.Nick = socket.Nick Then
                            putPage(c.Chan_Owner, "*** Du bist nun der Gamehost ***")
                        End If

                        For Each player As WolClient In c.sockets
                            SendGamechannames(c, c.Chan_ListType, player)
                        Next
                    End If
                Else
                    Dim _tmp_chan As Chatchannel = GetChatchannelbyName(chan)

                    If _tmp_chan IsNot Nothing Then
                        If Not gethostbyuser(socket.Hostname, _tmp_chan) Then

                            If Not _tmp_chan.sockets.Contains(socket) Then
                                _tmp_chan.sockets.Add(socket)

                                ParamReply(socket, "JOINGAME", min_Plr & " " & maxPls & " " & type & " " & unk1 & " " & socket.Squadid & " " & socket.IPasLong & " " & istournament & " :" & chan)

                                For Each player As WolClient In _tmp_chan.sockets
                                    SendGamechannames(_tmp_chan, _tmp_chan.Chan_ListType, player)
                                Next

                                If _tmp_chan.Chan_Owner.Nick <> socket.Nick Then
                                    putPage(socket, "*** " & _tmp_chan.Chan_Owner.Nick & " ist der Gamehost! ***")
                                End If

                            Else
                                Throw New InvalidDataException(rplcodes.ERR_USERONCHANNEL & " " & chan & " :User is already in that channel")
                            End If
                        Else
                            putPage(socket, "Fehler beim beitreten des Spiels: Jeder Spieler muss eine EIGENE routbare Externe IP-Adresse benutzen =(!")

                            For Each player As WolClient In _tmp_chan.sockets
                                putPage(player, socket.Nick & " kann diesem spiel nicht beitreten, da bereits jemand in diesem Spiel die IP-Adresse von " & socket.Nick & " benutzt!")
                            Next
                            Threading.Thread.Sleep(300)
                            RemoveClient(socket)
                        End If
                    Else
                        Throw New InvalidDataException(rplcodes.ERR_NOSUCHCHANNEL & " " & chan & " :No Such Channel")
                    End If
                End If
            Catch ex As InvalidDataException
                RaiseEvent Exception("[JOINGAME]: " & ex.Message)
                socket.Send(":" & network_adress & " " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Private Sub putCommand(Socket As WolClient, ByVal command As String, ByVal params As String)
        Socket.Send(":" & network_adress & " " & command & " :" & params)
    End Sub

    Private Function gethostbyuser(host As String, channel As Chatchannel) As Boolean
        Dim _tmp As Boolean = False
        For Each user As WolClient In channel.sockets
            If user.Hostname = host Then
                _tmp = True
            End If
            Exit For
        Next
        Return _tmp
    End Function

    Private Sub WolServer_handle_PRIVMSG_command(chan As String, Message As String, socket As WolClient) Handles Me.handle_PRIVMSG_command
        SyncLock lock
            If chan.StartsWith("#") Then
                Dim tmpchan As Chatchannel = GetChatchannelbyName(chan)

                For Each player As WolClient In tmpchan.sockets
                    ReplyMessage(player, Message, False)
                Next
            Else
                ReplyMessage(socket, Message, True)
            End If
        End SyncLock
    End Sub

    Private Sub WolServer_handle_NAMES_command(channel As String, socket As WolClient) Handles Me.handle_NAMES_command
        SyncLock lock
            Dim Chan As Chatchannel = GetChatchannelbyName(channel)

            For Each player As WolClient In Chan.sockets

                For i As Integer = 0 To Chan.sockets.Count - 1
                    putReply(player, rplcodes.RPL_NAMREPLY, player.Nick, "* " & Chan.Chan_Name & " :" & Chan.sockets.Item(i).Nick & ",0," & Chan.sockets.Item(i).IPasLong)
                Next
                putReply(player, rplcodes.RPL_ENDOFNAMES, player.Nick & " " & Chan.Chan_Name & " :End of Names")

            Next
        End SyncLock
    End Sub

    Private Sub WolServer_handle_serial_command(SERIAL As String, socket As WolClient) Handles Me.handle_serial_command
        If SERIAL.Length > 24 AndAlso SERIAL.Length < 26 Then
            socket.Serial = SERIAL
            socket.Serial_Report = True
            socket.TournamentAllowed_State = True
        Else
            socket.Serial_Report = False
            socket.TournamentAllowed_State = True
        End If
    End Sub

    Private Sub WolServer_handle_verchk_command(sku As String, version As String, socket As WolClient) Handles Me.handle_verchk_command
        Dim _needpatch As Boolean = False
        Try
            If CDbl(version) <> CDbl(vercheck.GetAllowedVersionbySKU("WOL", sku)) And CDbl(sku) <> 32512 And CDbl(sku) <> 1000 Then
                _needpatch = True
            End If

            If CDbl(sku) = vercheck.GetAllowedVersionbySKU("WOL", "32512") Then
                If CDbl(sku) = 32512 Or CDbl(sku) = 1000 Then
                    putReply(socket, "379", socket.Nick & " :none none none 1 " & sku & " NONREQ")

                    If CDbl(sku) = 1000 Then
                        socket.wolversion = "1"
                    End If
                End If
            End If

            If _needpatch = True Then
                Throw New NotImplementedException(rplcodes.RPL_UPDATE_EXIST & " :You must update before connecting!")
            Else
                If socket.wolversion = "1" Then
                    putReply(socket, rplcodes.RPL_UPDATE_NONEX, "")
                Else
                    putReply(socket, rplcodes.RPL_UPDATE_NONEX, "")
                End If

                socket.Cver2 = sku
                socket.GameVersion = version
            End If

        Catch ex As NotImplementedException
            socket.Send(":" & network_adress & " " & ex.Message)
            RaiseEvent ServerState(ex.Message)
            RemoveClient(socket)
        End Try
    End Sub

    Private Sub WolServer_handle_TOPIC_command(channel As String, Topic As String, ByVal socket As WolClient) Handles Me.handle_TOPIC_command
        SyncLock lock
            For Each c As Chatchannel In chanprovider.channels
                If c.Chan_Name = channel Then
                    c.Chan_Topic = Replace(Topic, channel, "")
                    putReply(socket, rplcodes.RPL_TOPIC, ":" & Topic)
                End If
            Next
        End SyncLock
    End Sub

    Private Sub WolServer_handle_gameopt_command(chan As String, options As String, socket As WolClient) Handles Me.handle_gameopt_command 'FIXME: =(
        SyncLock lock
            Dim _tmpchan As Chatchannel = GetChatchannelbyName(chan)
            If chan.StartsWith("#") Then
                For Each player As WolClient In GetGamehostChannel(socket).sockets
                    player.Send(":" & socket.hostmask & " GAMEOPT " & LTrim(options))

                Next
            End If
            'TODO: WHISPERMODE adden...
        End SyncLock
    End Sub

    Private Sub servserv_handle_lobcount_command(sku As String, socket As WolClient) Handles Me.handle_lobcount_command
        putReply(socket, rplcodes.RPL_LOBBYCOUNT, "u " & get_lobbycount())
    End Sub

    Private Sub servserv_handle_whereto_command(username As String, password As String, sku As String, version As String, ByVal serial As String, socket As WolClient) Handles Me.handle_whereto_command
        SyncLock lock
            Try
                'If serial.Length < 25 Or serial Is Nothing Then
                'Throw New NotImplementedException("Serial not given or invalid!")
                'End If

                Dim _temp As Double = CDbl(vercheck.GetAllowedVersionbySKU("WOL", sku))
                Dim new_patchversion As Double = 0

                If CDbl(version) <> _temp Then
                    new_patchversion = _temp
                Else
                    new_patchversion = 0
                End If

                If username = "TibSun" AndAlso password = "TibPass99" AndAlso CDbl(version) = CDbl(vercheck.GetAllowedVersionbySKU("WOL", sku)) Then
                    If new_patchversion = 0 Then

                        'Announce My-Server...

                        putReply(socket, rplcodes.RPL_WOLSERV, network_adress & " 4005 '0:(" & network_adress & ") " & _srvname & "' -" & _timezone & " 36.1083 -115.0582")
                        putReply(socket, rplcodes.RPL_GAMERES_SERV, network_adress & " 4807 '(" & network_adress & ") " & _srvname & "' -" & _timezone & " 36.1083 -115.0582")
                        putReply(socket, rplcodes.RPL_LADDERSERV, network_adress & " 4007 '(" & network_adress & ") " & _srvname & "' -" & _timezone & " 36.1083 -115.0582")
                        putReply(socket, rplcodes.RPL_PINGERVER, network_adress & " 0 '(" & network_adress & ") " & _srvname & "' -" & _timezone & " 36.1083 -115.0582")

                        putReply(socket, rplcodes.RPL_WDTSERVER, network_adress & " 4005 '(" & network_adress & ") WDT-Server' -" & _timezone & " 36.1083 -115.0582")
                        putReply(socket, rplcodes.RPL_MANGLERSERV, network_adress & " 48321 '(" & network_adress & ") Port-Mangler' -" & _timezone & " 36.1083 -115.0582")
                        putReply(socket, rplcodes.RPL_TICKETSERV, network_adress & " 48018 '(" & network_adress & ") Ticket-Server ' -" & _timezone & " 36.1083 -115.0582")

                        If _multiserver = "1" Then
                            send_Servers(socket)
                        End If
                    Else
                        putReply(socket, rplcodes.RPL_UPDATE_FTP, "u :" & Get_patch_informations(socket.Cver2, socket.GameVersion, CStr(new_patchversion)))
                    End If
                Else
                    Throw New NotImplementedException("GameLogin for SKU " & sku & " with Password failed: -> Username: " & username & " ; Password: " & password & " ; Version: " & version)
                End If

            Catch ex As NotImplementedException
                RaiseEvent Exception("[WHERETO]: " & ex.Message)
                putReply(socket, rplcodes.ERR_APGARMISSMATCH, "u :" & ex.Message)
                RemoveClient(socket)
            End Try
        End SyncLock
    End Sub

    Private Sub GetChannelList(ByVal ListType As String, ByRef GameID As String, socket As WolClient)
        SyncLock lock
            Dim flag As String = " 128"
            Dim _locked As String = " LOCK"
            For Each chan As Chatchannel In chanprovider.channels
                If chan.Chan_Key <> "" Then
                    flag = " 384" ' password needed
                ElseIf chan.Chan_Name.StartsWith("#Lob") Or chan.Chan_Name.StartsWith("#Cha") Then
                    flag = " 388"
                Else
                    flag = " 128" ' No Password needed...
                End If

                If chan.Chan_Name.Contains("#Lob_" & chan.Chan_Gameid) Or chan.Chan_Name = "#Chat" Then
                    putReply(socket, rplcodes.RPL_LIST, socket.Nick & " " & chan.Chan_Name & " " & chan.Chan_Min_Users & " " & CStr(chan.sockets.Count) & flag)
                End If
            Next
        End SyncLock

    End Sub

    Private Sub GetGameList(ByVal ListType As String, ByRef GameID As String, socket As WolClient)
        SyncLock lock
            Dim flag As String = " 128"
            Dim _locked As String = " LOCK"
            Dim _line As String
            For Each chan As Chatchannel In chanprovider.channels
                If chan.Chan_Key <> "" Then
                    flag = " 384" ' password needed
                Else
                    flag = " 128" ' No Password needed...
                End If

                If chan.Chan_ListType = GameID Then

                    _line = chan.Chan_Name & " " & CStr(chan.sockets.Count) & " " & chan.Chan_Max_Users & " " & chan.Chan_ListType & " " & chan.Is_Tournament & " " & chan.Chan_GameEx & " " & chan.OwnerIPasLong & flag & ":" & Trim(chan.Chan_Topic)

                    _line = Replace(_line, "  ", " ")
                    If _line <> "" Then
                        putReply(socket, rplcodes.RPL_LISTGAME, socket.Nick & " " & _line)
                    End If
                End If
            Next
        End SyncLock
    End Sub

    Public Function GetChatchannelbyName(ByVal channel As String) As Chatchannel
        SyncLock lock
            For Each chan As Chatchannel In chanprovider.channels
                If chan.Chan_Name = channel Then
                    Return chan
                    Exit For
                End If
            Next
        End SyncLock
    End Function

    Private Sub ReplyMessage(_from As WolClient, ByVal Message As String, ByVal skipsource As Boolean)
        SyncLock lock
            If skipsource = True Then
                For Each client As WolClient In ConnectedClients

                    If client.Nick <> _from.Nick Then
                        client.Send(":" & _from.hostmask & " privmsg " & Message)
                    End If
                Next

            Else
                For Each client As WolClient In ConnectedClients
                    client.Send(":" & _from.hostmask & " privmsg " & Message)
                Next
            End If
        End SyncLock
    End Sub

    Private Function GetGamehostChannel(ByVal player As WolClient) As Chatchannel
        SyncLock lock
            For Each chan As Chatchannel In chanprovider.channels
                If chan.sockets.Contains(player) Then
                    Return chan
                    Exit For
                End If
            Next
        End SyncLock
    End Function

    Private Function GetClientbyUSername(ByVal usernmae As String) As WolClient
        SyncLock lock
            For Each client As WolClient In ConnectedClients
                If client.Nick = usernmae Then
                    Return client
                    Exit For
                End If
            Next
        End SyncLock

    End Function

    Private Sub Sendgameoptions(target As WolClient, ByVal options As String, ByVal source As WolClient) ' Guido: Später sollte ich das Verallgemeinern (Funktion besteht bereits) 
        target.Send(":" & source.hostmask & " GAMEOPT " & options)
    End Sub

    Private Sub WolServer_handle_startg_command(chan As String, names As String, ByVal socket As WolClient) Handles Me.handle_startg_command
        SyncLock lock
            Dim _chan As Chatchannel = GetChatchannelbyName(chan)
            Dim message As String = ""

            For Each player As WolClient In _chan.sockets
                If message.Length < 1 Then
                    message = player.Nick & " " & player.Hostname & " "
                Else
                    message = message & player.Nick & " " & player.Hostname & " "
                End If

            Next

            message = message & ":" & Gamenumber & " " & GetunixTimeStamp()

            For Each player As WolClient In GetGamehostChannel(socket).sockets
                player.Send(":" & socket.hostmask & " STARTG " & _chan.Chan_Owner.Nick & " :" & message)
            Next
        End SyncLock
    End Sub

    Private Sub send_Servers(ByVal socket As WolClient)
        SyncLock lock
            Try
                Dim max As Integer = 0
                Dim _line As String = ""

                Dim entry As String = Nothing

                For Each s As server In Servers
                    If s.type = "605" Then
                        _line = s.addrr & " " & s.port & " '" & s.unk & ":(" & s.addrr & ") " & s.name & "' -" & s.tzone & " " & s.lon & " " & s.lat
                    Else
                        _line = s.addrr & " " & s.port & " '(" & s.addrr & ") " & s.name & "' -" & s.tzone & " " & s.lon & " " & s.lat
                    End If
                    putReply2(socket, s.type, "u :" & _line)
                Next
            Catch ex As NotImplementedException
                socket.Send(":" & network_adress & " " & ex.Message)
                RaiseEvent handle_quit_command(socket)
            End Try
        End SyncLock
    End Sub

    Private Sub Client_Message_Send(msg As String)
        RaiseEvent ServerState(msg)
    End Sub

    Private Sub Client_Exception(ex As String)
        RaiseEvent Exception("[CLIENT]: " & ex)
    End Sub

    Public Sub loadServers(rplcode As String)
        If File.Exists(My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini") Then
            Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini")
            Dim entry As String = Nothing
            Dim parts() As String
            Dim max As Integer = 0

            max = CInt(ini_func.WertLesen("INFO", rplcode & "_count"))


            For i As Integer = 0 To max - 1
                entry = ini_func.WertLesen(rplcode, CStr(i))
                parts = entry.Split(CChar(";"))

                Dim c As New server
                c.addrr = parts(0)
                c.port = parts(1)
                c.type = rplcode

                If rplcode = "605" Then
                    c.unk = CStr(i)
                End If

                If parts(5) <> "" Then

                    c.name = parts(5)
                Else
                    RaiseEvent Exception("WARN: Load-Servers: no Servername specified for entry " & i.ToString & " in Section " & c.type & ", ... using " & c.addrr & " as ServerName!")
                    c.name = c.addrr
                End If
                c.tzone = parts(2)
                c.lon = parts(3)
                c.lat = parts(4)

                Servers.Add(c)
            Next
        Else
            RaiseEvent Exception("Cant find File : " & My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini")
        End If
    End Sub

    Private Function get_lobbycount() As String
        If File.Exists(My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini") Then
            Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini")
            Dim max As Integer = 0

            max = CInt(ini_func.WertLesen("INFO", "605_count"))

            If CStr(max) IsNot Nothing Then
                Return CStr(max)
            Else
                Return "1"
            End If
        Else
            Return "1"
        End If
    End Function

End Class
