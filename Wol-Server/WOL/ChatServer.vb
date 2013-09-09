Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Text

Public Class WolServer
    Dim vercheck As New Versioncheck
    Dim userprovider As New AccProvider
    Dim chanprovider As New chanprovider
    Private ReadOnly rplcodes As New REPLYCODES

    Private Gamenumber As Integer = 1
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
    Protected _motdfile As String

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

    Public WriteOnly Property set_MOTD_File As String
        Set(value As String)
            _motdfile = value
        End Set
    End Property

    Private ReadOnly Property get_MOTD_File As String
        Get
            If File.Exists(_motdfile) Then
                Return _motdfile
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public ReadOnly Property Num_Clients As String
        Get
            Return CStr(ConnectedClients.Count)
        End Get
    End Property

    Public Function GetunixTimeStamp() As String
        Dim t() As String = CStr((DateTime.Now - New DateTime(1970, 1, 1)).TotalMilliseconds).Split(CChar(","))
        Return t(0)
    End Function

    Public ReadOnly Property ServerName As String
        Get
            Return _srvname
        End Get
    End Property

    Public ReadOnly Property Getnetwork_adress As String
        Get
            Return _irc_network_fqdn
        End Get
    End Property

    Public Sub StartServer(ByVal port As Integer, ByVal srvname As String, ByVal irc_network As String)
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

        loadServers("605")
        loadServers("608")
        loadServers("609")
        loadServers("611")
        loadServers("612")
        loadServers("613")
        loadServers("615")

        Try
            _srvname = srvname
            _irc_network_fqdn = irc_network
        
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
                                    If socket.Iswolhost = False Then RaiseEvent handle_MOTD_command(socket)

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
                                socket.Send(":" & Getnetwork_adress & " " & ex.Message)
                                RaiseEvent Exception("[Command]: " & socket.GetNick & " : " & ex.Message)
                                Exit Sub
                            Catch ex As ArgumentException
                                socket.Send(":" & Getnetwork_adress & " " & ex.Message)
                                RaiseEvent Exception("[Command]: " & socket.GetNick & " : " & ex.Message)
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
            RaiseEvent Connection_closed("Verbindung beendet: " & socket.Gethostname)
        End SyncLock
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String)
        socket.Send(":" & Getnetwork_adress & " " & replycode)
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String, Message As String)
        socket.Send(":" & Getnetwork_adress & " " & replycode & " " & Message)
    End Sub

    Sub putPage(ByVal socket As WolClient, Message As String)
        socket.Send(":" & "ChanServ" & " PAGE " & socket.GetNick & " :" & Message)
    End Sub

    Sub putReply2(socket As WolClient, ByVal replycode As String, Message As String)
        socket.Send(":" & " " & replycode & " " & Message)
    End Sub

    Sub ParamReply(socket As WolClient, ByVal Command As String, parameter As String)
        socket.Send(":" & socket.hostmask & " " & Command & " " & parameter)
    End Sub

    Sub putReply(socket As WolClient, ByVal replycode As String, Nickname As String, ByVal Message As String)
        socket.Send(":" & Getnetwork_adress & " " & replycode & " " & Nickname & " " & Message)
    End Sub

    Private Sub WolServer_handle_ADVERTR_command(chan As String, socket As WolClient) Handles Me.handle_ADVERTR_command
<<<<<<< HEAD
        putCommand(socket, "ADVERTR", "5 " & chan & "\n\r")
=======
        socket.Send(":" & Getnetwork_adress & " ADVERTR 5 " & chan)
>>>>>>> Added basic WCHAT (WOL V1)-Support
    End Sub

    Private Sub WolServer_handle_apgar_command(APGAR As String, _unkint As String, socket As WolClient) Handles Me.handle_apgar_command
        If Not APGAR = "oeakaaaa" Then ' Password: "" (leer)
            If socket.isRegistered Then
                If APGAR = userprovider.GetPassword("WOL", socket.GetNick) Then
                    socket.SetApgar = APGAR
                Else
                    putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.GetNick & " :Bad Password/Nickname.")
                    RemoveClient(socket)
                    Exit Sub
                End If
            Else
                userprovider.AddUser("WOL", socket.GetNick, APGAR, socket)
                socket.set_isregistered = True
            End If
        Else
            putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.GetNick & " :Bad Password/Nickname.")
            RemoveClient(socket)
        End If
    End Sub

    Private Sub WolServer_handle_GETLOCALE_command(ByVal nicks As String, ByVal socket As WolClient) Handles Me.handle_GETLOCALE_command 'FIXME
        putReply(socket, rplcodes.RPL_LOCALE, socket.GetNick, socket.GetNick & "`" & socket.Getlocale)
    End Sub

    Private Sub WolServer_handle_MOTD_command(socket As WolClient) Handles Me.handle_MOTD_command
        SyncLock lock
            If get_MOTD_File <> Nothing Then
                Dim motd() As String

                If socket.Iswolhost = True Then
                    putReply(socket, rplcodes.RPL_MOTDSTART, ":- Willkommen, " & socket.GetNick & "!")
                Else
                    putReply(socket, rplcodes.RPL_MOTDSTART, ":- " & _irc_network_fqdn & " Message of the day -")
                End If

                If File.Exists(get_MOTD_File) Then
                    motd = File.ReadAllLines(get_MOTD_File)
                    For i As Integer = 0 To motd.Length - 1
                        putReply(socket, rplcodes.RPL_MOTD, socket.GetNick, ":- " & motd(i) & "")
                    Next
                End If
            Else
                RaiseEvent Exception("MOTD-File not found!")
            End If

            putReply(socket, rplcodes.RPL_ENDOFMOTD, " :End of MOTD command")
        End SyncLock
    End Sub

    Private Sub WolServer_handle_cvers_command(version As String, sku As String, socket As WolClient) Handles Me.handle_cvers_command
        socket.SetCver1 = version
        socket.SetCver2 = sku
        socket.SetWoltyp = True
        socket.SetGameID = vercheck.GetClient_GroupbySKU("WOL", sku)
    End Sub

    Private Sub WolServer_handle_GETCODEPAGE_command(nick As String, socket As WolClient) Handles Me.handle_GETCODEPAGE_command
        putReply(socket, rplcodes.RPL_CODEPAGE, nick, nick & "`" & socket.GetCodepage)

    End Sub

    Private Sub WolServer_handle_user_command(param As String, socket As WolClient) Handles Me.handle_user_command

        If Not socket.isRegistered Then
            socket.set_isregistered = True
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
                    putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.GetNick & " :Nickname not allowed!")
                    RemoveClient(socket)
                Else
                    Dim _unique As Boolean = True
                    SyncLock lock
                        For Each channel As Chatchannel In chanprovider.channels
                            For Each user As WolClient In channel.sockets
                                If channel.sockets.Count > 0 Then
                                    If nick = user.GetNick Then
                                        _unique = False
                                    End If
                                End If
                            Next
                        Next
                    End SyncLock

                    If Not _unique Then
                        putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.GetNick & " :Nickname already in Use!")
                        RemoveClient(socket)
                    Else
                        socket.SetNick = nick
                        If userprovider.DoesUserExist(nick) Then
                            socket.set_isregistered = True
                        Else
                            socket.set_isregistered = False
                        End If
                    End If
                End If
            Else
                putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.GetNick & " :Nickname not allowed!")
                RemoveClient(socket)
            End If
        End SyncLock
    End Sub

    Private Sub WolServer_handle_pass_command(password As String, socket As WolClient) Handles Me.handle_pass_command
        'GUIDO: Man sollte es rausnehmen... da das wirkliche Password mittels Apgar übertragen wird und nicht hier... =/
        If socket.Iswolhost Then
            If password <> "supersecret" Then
                putReply(socket, rplcodes.ERR_APGARMISSMATCH & " " & socket.GetNick & " :Bad Login!")
                RemoveClient(socket)
            End If
        End If
    End Sub

    Private Sub WolServer_handle_SETCODEPAGE_command(codepage As String, socket As WolClient) Handles Me.handle_SETCODEPAGE_command
        If codepage = "" Then codepage = "1252"

        If socket.GetCodepage = codepage Then
            putReply(socket, rplcodes.RPL_CODEPAGESET, socket.GetNick, codepage)
        Else
            socket.SetCodePage = CInt(codepage)
            putReply(socket, rplcodes.RPL_CODEPAGESET, socket.GetNick, codepage)
        End If
    End Sub

    Private Sub WolServer_handle_SQUADINFO_command(squad As String, socket As WolClient) Handles Me.handle_SQUADINFO_command
        If squad = "0" Then
            putReply(socket, rplcodes.RPL_SQUADINFO, socket.GetNick & " :ID does not Exist")
        End If
    End Sub

    Private Sub WolServer_handle_SETLOCALE_command(locale As String, socket As WolClient) Handles Me.handle_SETLOCALE_command
        Try
            If locale = "5" Or locale = "0" Then
                socket.SetLocale = CInt(locale)

                If socket.Getlocale = locale Then
                    putReply(socket, rplcodes.RPL_LOCALESET, socket.GetNick, locale)
                End If
            Else
                Throw New InvalidDataException("Your Location is not supported on this Server!")
            End If
        Catch ex As Exception
            RaiseEvent ServerState(socket.GetNick & " benutzt einen nicht erlaubten Standort!")
            putReply(socket, rplcodes.ERR_APGARMISSMATCH, socket.GetNick & " :" & ex.Message)
            RemoveClient(socket)
        End Try
    End Sub

    Private Sub WolServer_handle_setopt_command(opt1 As String, opt2 As String, socket As WolClient) Handles Me.handle_setopt_command
        socket.SetSetopt1 = CInt(opt1)
        socket.SetSetopt2 = CInt(opt2)
    End Sub

    Private Sub WolServer_handle_ping_command(socket As WolClient) Handles Me.handle_ping_command
        putReply(socket, Getnetwork_adress, "PONG : " & Getnetwork_adress)
    End Sub

    Private Sub WolServer_handle_LIST_command(type As String, gameid As String, socket As WolClient) Handles Me.handle_LIST_command

        putReply(socket, rplcodes.RPL_LISTSTART, socket.GetNick, " :Listing Channels...")
        If type = "0" Then
            GetChannelList(type, gameid, socket)

        ElseIf type = gameid Then
            GetGameList(type, gameid, socket)
        ElseIf type = "-1" Then
            putReply(socket, rplcodes.RPL_LIST, socket.GetNick & " #Chat 0 0 0 388:")
        End If
        putReply(socket, rplcodes.RPL_ENDOFLIST, socket.GetNick, " :End of LIST")
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
                        If tmpchan.key <> "" Then ' Chaannel passwort gesetzt ?
                            chan_has_key = True
                        Else
                            chan_has_key = False
                        End If

                        If tmpchan.key = chanpass Then
                            key_is_matching = True
                        Else
                            key_is_matching = False
                        End If

                        If CInt(tmpchan.sockets.Count) < CInt(tmpchan.Max_Users) Then
                            If Not tmpchan.sockets.Contains(socket) Then
                                tmpchan.sockets.Add(socket)
                            Else
                                already_in_chan = True
                                Throw New InvalidDataException(rplcodes.ERR_USERONCHANNEL & " " & chan & " :You are already on that Channel" & tmpchan.GetName)
                            End If
                            If socket.get_wolversion = "1" Then
                                ParamReply(socket, "JOIN", chan)
                            Else
                                ParamReply(socket, "JOIN :", "0," & socket.GetIPasLong & " " & chan)
                            End If

                            RaiseEvent handle_NAMES_command(tmpchan.GetName, socket)
                            RaiseEvent handle_LIST_command(socket.gameid, socket.gameid, socket)

                            If socket.GetSerial = "" AndAlso socket.GetSerialReport = False Then
                                putPage(socket, "Beim Login, wurde festgestellt, dass das Spiel fehlerhaft Installiert wurde...")
                                putPage(socket, "Stelle bitte sicher, dass das Spiel korrekt in der Registry eingetragen wurde!")
                                socket.SetSerialReportstate = True
                                socket.SetTournamentAllowedState = False
                            End If
                        Else
                            Throw New InvalidDataException(rplcodes.ERR_CHANNELISFULL & " " & tmpchan.GetName & " :Channel is Full")
                        End If
                    Else
                        chan_exists = False
                        Throw New InvalidDataException(rplcodes.ERR_NOSUCHCHANNEL & " " & tmpchan.GetName & " :No such Channel...")
                    End If
                Else
                    Throw New InvalidDataException(rplcodes.ERR_BADCHANMASK & " " & chan & " :Bad Channel Mask ")
                End If
            Catch ex As InvalidDataException
                RaiseEvent Exception("[JOIN]: " & socket.GetNick & ": -> " & ex.Message)
                socket.Send(":" & Getnetwork_adress & " " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Public Sub SendGamechannames(ByVal chan As Chatchannel, listtype As String, socket As WolClient)
        putReply(socket, rplcodes.RPL_NAMREPLY, socket.GetNick, "* " & chan.GetName & " :@" & chan.GetOwner.GetNick & ",0," & chan.GetOwner.GetIPasLong)

        For i As Integer = 0 To chan.sockets.Count - 1
            If chan.sockets.Item(i).GetNick <> chan.GetOwner.GetNick AndAlso socket.GetNick <> chan.sockets.Item(i).GetNick Then
                putReply(socket, rplcodes.RPL_NAMREPLY, socket.GetNick, "* " & chan.GetName & " :" & chan.sockets.Item(i).GetNick & ",0," & chan.sockets.Item(i).GetIPasLong)
            End If
        Next
        If socket.GetNick <> chan.GetOwner.GetNick Then
            putReply(socket, rplcodes.RPL_NAMREPLY, socket.GetNick, "* " & chan.GetName & " :" & socket.GetNick & ",0," & socket.GetIPasLong)
        End If

        putReply(socket, rplcodes.RPL_ENDOFNAMES, socket.GetNick & " " & chan.GetName & " :End of Names")
    End Sub

    Private Sub WolServer_handle_PART_command(channel As String, socket As WolClient) Handles Me.handle_PART_command
        Try
            If channel.StartsWith("#") Then
                Dim _tmp_chan As Chatchannel = GetChatchannelbyName(channel)

                If _tmp_chan IsNot Nothing Then
                    If _tmp_chan.sockets.Contains(socket) Then
                        _tmp_chan.sockets.Remove(socket)

                        If _tmp_chan.GetOwner IsNot Nothing Then
                            If _tmp_chan.GetOwner.Equals(socket) Then
                                _tmp_chan.Owner = Nothing
                            End If
                        End If

                        ParamReply(socket, "PART", channel)

                        If _tmp_chan.sockets.Count = 0 AndAlso _tmp_chan.LIstType <> "0" Then
                            If chanprovider.channels.Contains(_tmp_chan) Then
                                RaiseEvent ServerState("Removing Channel: " & _tmp_chan.GetName)
                                chanprovider.channels.Remove(_tmp_chan)
                            End If
                        Else
                            For Each player As WolClient In _tmp_chan.sockets
                                If socket.GetNick <> player.GetNick Then
                                    RaiseEvent handle_NAMES_command(_tmp_chan.GetName, player) ' Maybe we should rewrite this function >.<
                                End If
                            Next
                        End If

                    Else
                        Throw New InvalidDataException(rplcodes.ERR_NOSUCHNICK & " " & _tmp_chan.GetName & " :User is not on that Channel")
                    End If

                Else
                    Throw New InvalidDataException(rplcodes.ERR_NOSUCHCHANNEL & " " & channel & " :No such Channel...")
                End If
            Else
                Throw New InvalidDataException(rplcodes.ERR_BADCHANMASK & " " & channel & " :Bad Channel Mask")
            End If
        Catch ex As InvalidDataException
            socket.Send(":" & Getnetwork_adress & " " & ex.Message)
            RaiseEvent ServerState("[PART]: " & socket.GetNick & ": -> " & ex.Message)
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
                        .SetName = chan
                        .Owner = socket
                        .SetKey = optpass
                        .SetOwnerIPasLong = socket.GetIPasLong
                        .SetIP = socket.Gethostname
                        .SetGameID = type
                        .SetListType = type
                        .SetMinUsers = CInt(min_Plr)
                        .SetMasUsers = CInt(maxPls)
                        .SetTournament = istournament
                        .Setreserved = unk1
                        .SetGameEx = GameExtension
                        .sockets.Add(socket)
                        '.SetTopic = .GetName
                    End With

                    chanprovider.channels.Add(c)

                    If chanprovider.channels.Contains(c) Then
                        Dim _line As String = c.Min_Users & " " & c.Max_Users & " " & c.GetGameid & " " & c.GetReserved & " " & c.GetOwner.GetSquadid & " " & c.GetOwner.GetIPasLong & " " & c.ISTournament & " :" & c.GetName

                        RaiseEvent ServerState("JOINGAME_DEBUG: " & _line)
                        ParamReply(socket, "JOINGAME", _line)
                        putReply(socket, rplcodes.RPL_TOPIC, c.GetTopic)

                        If c.GetOwner.GetNick = socket.GetNick Then
                            putPage(c.GetOwner, "*** Du bist nun der Gamehost ***")
                        End If

                        For Each player As WolClient In c.sockets
                            SendGamechannames(c, c.LIstType, player)
                        Next
                    End If
                Else


                    Dim _tmp_chan As Chatchannel = GetChatchannelbyName(chan)

                    If _tmp_chan IsNot Nothing Then
                        If Not gethostbyuser(socket.Gethostname, _tmp_chan) Then

                            If Not _tmp_chan.sockets.Contains(socket) Then
                                _tmp_chan.sockets.Add(socket)

                                ParamReply(socket, "JOINGAME", min_Plr & " " & maxPls & " " & type & " " & unk1 & " " & socket.GetSquadid & " " & socket.GetIPasLong & " " & istournament & " :" & chan)



                                For Each player As WolClient In _tmp_chan.sockets
                                    SendGamechannames(_tmp_chan, _tmp_chan.LIstType, player)
                                Next

                                If _tmp_chan.GetOwner.GetNick <> socket.GetNick Then
                                    putPage(socket, "*** " & _tmp_chan.GetOwner.GetNick & " ist der Gamehost! ***")
                                End If

                            Else
                                Throw New InvalidDataException(rplcodes.ERR_USERONCHANNEL & " " & chan & " :User is already in that channel")
                            End If
                        Else
                            putPage(socket, "Fehler beim beitreten des Spiels: Jeder Spieler muss eine EIGENE routbare Externe IP-Adresse benutzen =(!")

                            For Each player As WolClient In _tmp_chan.sockets
                                putPage(player, socket.GetNick & " kann diesem spiel nicht beitreten, da bereits jemand in diesem Spiel die IP-Adresse von " & socket.GetNick & " benutzt!")
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
                socket.Send(":" & Getnetwork_adress & " " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Private Sub putCommand(Socket As WolClient, ByVal command As String, ByVal params As String)
        Socket.Send(":" & Getnetwork_adress & " " & command & " :" & params)
    End Sub

    Private Function gethostbyuser(host As String, channel As Chatchannel) As Boolean
        Dim _tmp As Boolean = False
        For Each user As WolClient In channel.sockets
            If user.Gethostname = host Then
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
                    putReply(player, rplcodes.RPL_NAMREPLY, player.GetNick, "* " & Chan.GetName & " :" & Chan.sockets.Item(i).GetNick & ",0," & Chan.sockets.Item(i).GetIPasLong)
                Next
                putReply(player, rplcodes.RPL_ENDOFNAMES, player.GetNick & " " & Chan.GetName & " :End of Names")

            Next
        End SyncLock
    End Sub

    Private Sub WolServer_handle_serial_command(SERIAL As String, socket As WolClient) Handles Me.handle_serial_command
        If SERIAL.Length > 24 AndAlso SERIAL.Length < 26 Then
            socket.SetSerial = SERIAL
            socket.SetSerialReportstate = True
            socket.SetTournamentAllowedState = True
        Else
            socket.SetSerialReportstate = False
            socket.SetTournamentAllowedState = True
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
                    putReply(socket, "379", socket.GetNick & " :none none none 1 " & sku & " NONREQ")

                    If CDbl(sku) = 1000 Then
                        socket.set_wolversion = "1"
                    End If
                End If
            End If

            If _needpatch = True Then
                Throw New NotImplementedException(rplcodes.RPL_UPDATE_EXIST & " :You must update before connecting!")
            Else
                If socket.get_wolversion = "1" Then
                    putReply(socket, rplcodes.RPL_UPDATE_NONEX, "")
                Else
                    putReply(socket, rplcodes.RPL_UPDATE_NONEX, "")
                End If

                socket.SetCver2 = sku
                socket.SetGameversion = CInt(version)
            End If

        Catch ex As NotImplementedException
            socket.Send(":" & Getnetwork_adress & " " & ex.Message)
            RaiseEvent ServerState(ex.Message)
            RemoveClient(socket)
        End Try
    End Sub

    Private Sub WolServer_handle_TOPIC_command(channel As String, Topic As String, ByVal socket As WolClient) Handles Me.handle_TOPIC_command
        SyncLock lock
            For Each c As Chatchannel In chanprovider.channels
                If c.GetName = channel Then
                    c.SetTopic = Replace(Topic, channel, "")
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
                        send_Servers(socket)
                    Else
                        putReply(socket, rplcodes.RPL_UPDATE_FTP, "u :" & Get_patch_informations(socket.GetCver2, socket.GetGameVersion, CStr(new_patchversion)))
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
                If chan.key <> "" Then
                    flag = " 384" ' password needed
                ElseIf chan.GetName.StartsWith("#Lob") Or chan.GetName.StartsWith("#Cha") Then
                    flag = " 388"
                Else
                    flag = " 128" ' No Password needed...
                End If

                If chan.GetName.Contains("#Lob_" & chan.GetGameid) Or chan.GetName = "#Chat" Then
                    putReply(socket, rplcodes.RPL_LIST, socket.GetNick & " " & chan.GetName & " " & chan.Min_Users & " " & CStr(chan.sockets.Count) & flag)
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
                If chan.key <> "" Then
                    flag = " 384" ' password needed
                Else
                    flag = " 128" ' No Password needed...
                End If

                If chan.LIstType = GameID Then

                    _line = chan.GetName & " " & CStr(chan.sockets.Count) & " " & chan.Max_Users & " " & chan.LIstType & " " & chan.ISTournament & " " & chan.GetGameEx & " " & chan.GetOwnerIPaslong & flag & ":" & Trim(chan.GetTopic)

                    _line = Replace(_line, "  ", " ")
                    If _line <> "" Then
                        putReply(socket, rplcodes.RPL_LISTGAME, socket.GetNick & " " & _line)
                    End If
                End If
            Next
        End SyncLock
    End Sub

    Public Function GetChatchannelbyName(ByVal channel As String) As Chatchannel
        SyncLock lock
            For Each chan As Chatchannel In chanprovider.channels
                If chan.GetName = channel Then
                    Return chan
                    Exit For
                End If
            Next
        End SyncLock
    End Function

    Private Sub ReplyMessage(_from As WolClient, ByVal Message As String, ByVal skipsource As Boolean)
        SyncLock lock
            If Message.StartsWith("/") Then
                putPage(_from, "Ja?! xD")
            Else
                If skipsource = True Then
                    For Each client As WolClient In ConnectedClients

                        If client.GetNick <> _from.GetNick Then
                            client.Send(":" & _from.hostmask & " privmsg " & Message)
                        End If
                    Next

                Else
                    For Each client As WolClient In ConnectedClients
                        client.Send(":" & _from.hostmask & " privmsg " & Message)
                    Next
                End If
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
                If client.GetNick = usernmae Then
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
                    message = player.GetNick & " " & player.Gethostname & " "
                Else
                    message = message & player.GetNick & " " & player.Gethostname & " "
                End If

            Next

            message = message & ":" & Gamenumber & " " & GetunixTimeStamp()

            For Each player As WolClient In GetGamehostChannel(socket).sockets
                player.Send(":" & socket.hostmask & " STARTG " & _chan.GetOwner.GetNick & " :" & message)
            Next
        End SyncLock
    End Sub

    Private Sub send_Servers(ByVal socket As WolClient)
        SyncLock lock
            Try
                Dim max As Integer = 0
                Dim _line As String = ""

                'TODO: Add YR and others :D
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
                socket.Send(":" & Getnetwork_adress & " " & ex.Message)
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
    End Sub

    Private Function get_lobbycount() As String
        Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\WOL_Serverlist.ini")
        Dim max As Integer = 0

        max = CInt(ini_func.WertLesen("INFO", "605_count"))

        If CStr(max) IsNot Nothing Then
            Return CStr(max)
        Else
            Return "1"
        End If
    End Function

End Class
