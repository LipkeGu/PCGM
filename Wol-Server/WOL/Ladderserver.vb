Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Text

Public Class LadderServer
#Region "Events"
    Public Event ServerState(ByVal e As String)
    Public Event NewConnection(ByVal e As String)
    Public Event Connection_closed(ByVal msg As String)
    Public Event byteDatareceived(ByVal Message() As Byte)
    Public Event StringDatareceived(ByVal Message As String)
    Public Event handle_LISTSEARCH_Command(ByVal SKU As String, ByVal arg2 As String, arg4 As String, ByVal arg5 As String, ByVal names As String, ByVal socket As LadderClient)
    Public Event handle_RUNGSEARCH_Command(ByVal arg1 As String, ByVal arg2 As String, arg3 As String, ByVal arg4 As String, ByVal arg5 As String, ByVal arg6 As String, ByVal arg7 As String, ByVal arg8 As String, socket As LadderClient)
#End Region
    Dim rplcodes As New REPLYCODES

#Region "Server"
    Private _ref As LadderClient
    Public ConnectedClients As New List(Of LadderClient)
    Private _Listener As TcpListener
    Private lock As New Object

    Public ReadOnly Property Num_Clients As String
        Get
            Return CStr(ConnectedClients.Count)
        End Get
    End Property

    Public Sub StartServer(ByVal port As Integer)
        Try
            _Listener = New TcpListener(IPAddress.Any, port)
            _Listener.ExclusiveAddressUse = False
            _Listener.Start()
            _Listener.BeginAcceptTcpClient(AddressOf _EndAccept, Nothing)
            RaiseEvent ServerState("Listening on:" & _Listener.LocalEndpoint.ToString)
        Catch ex As Exception
            RaiseEvent ServerState("FATAL ERROR: " & ex.Message)
        End Try
    End Sub

    Private Function GetHostadress(ByVal str As String) As String
        Dim _t() As String = str.Split(CChar(":"))
        Return _t(0)

    End Function

    Private Sub _EndAccept(ar As IAsyncResult)
        Dim c As New LadderClient(_Listener.EndAcceptTcpClient(ar))

        If ar IsNot Nothing Then
            c._remaddr = c.GetHostadress(c._socket.Client.RemoteEndPoint.ToString)
            AddHandler c.String_MessageReceived, AddressOf Client_String_Messagereceived
            AddHandler c.ConnectionClosed, AddressOf Client_Connection_Closed
            AddHandler c.send_Messagesend, AddressOf Client_String_Messagesend

            If c._socket IsNot Nothing And c._stream IsNot Nothing Then
                c._tc = c
                ConnectedClients.Add(c)

            Else
                Throw New SocketException
                c = Nothing
            End If
        Else
            Throw New SocketException
        End If
        RaiseEvent NewConnection("Neue Verbindung von: " & c._remaddr)
        _Listener.BeginAcceptTcpClient(AddressOf _EndAccept, Nothing)
    End Sub

    Public Sub SendStringtoAllClients(ByVal message As String)
        For Each c As LadderClient In ConnectedClients
            c.SendString(message)
        Next
    End Sub

    Public Sub SendStringToClient(tc As LadderClient, Message As String)
        ' MsgBox(Message)

        tc.SendString(Message)
    End Sub

    Private Sub Client_String_Messagereceived(ByVal message As String, ByVal socket As LadderClient)
        If message.Length > 1 Then
            Dim lines() As String = message.Split(CChar(vbCrLf))
            Dim params() As String

            For i As Integer = 0 To lines.Length - 1
                RaiseEvent StringDatareceived(lines(i))
                params = lines(i).Split(CChar(" "))

                If params(0).Contains("RUNGSEARCH") Then
                    RaiseEvent handle_RUNGSEARCH_Command(params(1), params(2), params(3), params(4), params(5), params(6), params(7), params(8), socket)
                ElseIf params(0).Contains("LISTSEARCH") Then
                    RaiseEvent handle_LISTSEARCH_Command(params(1), params(2), params(3), params(4), params(5), socket)
                Else
                    Throw New ArgumentException("Unbekannter Befehl: " & params(0) & ": " & lines(i))
                End If
            Next
        End If
    End Sub



    Private Sub Client_Connection_Closed(ByVal msg As String, ByVal ref As LadderClient)
        SyncLock lock
            ConnectedClients.Remove(ref)
            RaiseEvent Connection_closed(msg)
        End SyncLock
    End Sub

    Public Sub RemoveClient(ByVal socket As LadderClient)
        SyncLock lock
            If socket._socket IsNot Nothing Then
                socket._socket.Client.Disconnect(False)
                socket._socket.Close()
                socket._socket = Nothing
            End If

            If socket._stream IsNot Nothing Then
                socket._stream.Close()
                socket._stream = Nothing
            End If
            ConnectedClients.Remove(socket)
        End SyncLock
    End Sub
#End Region
    Private Sub Client_String_Messagesend(ref As LadderClient)

        'RemoveClient(ref)
    End Sub

    Private Sub Client_NewConnection(e As String)
        RaiseEvent NewConnection(e)
    End Sub


#Region "LadderServer-handles"

    Private Sub LadderServer_handle_LISTSEARCH_Command(SKU As String, arg2 As String, arg4 As String, arg5 As String, names As String, ByVal socket As LadderClient) Handles Me.handle_LISTSEARCH_Command
        'LISTSEARCH 4608 -1 0 0 0 :lolssss:Medice:Schnuffi xD:

        If names.Contains("") Then
            names = names.Replace("::", "")
        End If

        Dim Players() As String = names.Split(CChar(":"))

        'For i As Integer = 0 To Players.Length - 1
        '  If Players(i).Length > 0 Then
        'send fake-Scores for the moment ... so we can (for developing) join RA2 Lobbys xD 
        'All WOLv2 expects as OUTPUT: [rank] [nick] [points] [wins] [losses] 0 [disconnects]

        ' ParamReply(socket, i & " " & Players(i) & " " & i + (i * 6) & " " & i & " 0 0 0")
        '   End If
        'Next
        socket.SendString("NOTFOUND")
        RemoveClient(socket)

        ' LISTSEARCH 4608 -1 0 0 0 :Nick1:Nick2:NickN: 
    End Sub
    'Alle Clients erwarten einen Disconnet, nachdem alle Daten vom und zum Server übertragen worden sind, also tun wir das hier.

    Private Sub LadderServer_handle_RUNGSEARCH_Command(arg1 As String, arg2 As String, ag3 As String, arg4 As String, arg5 As String, arg6 As String, arg7 As String, arg8 As String, ByVal socket As LadderClient) Handles Me.handle_RUNGSEARCH_Command

        'WOL2 sendet als INPUT  -> ohne Battleclan...:

        'für alle: RUNGSEARCH 1 25 0 [SKU] -1 0 0 [Location]
        'Spieler: RUNGSEARCH [Nick] 25 10 [SKU] -1 0 0 [Location]

        'GWOL2 sendet als INPUT  -> für BattleClan... _
        'für alle: RUNGSEARCH 1 25 0 [BC-SKU] 0 0 0 X
        'Spieler: RUNGSEARCH [Nick] 25 0 [BC-SKU] -1 0 0 X'

        'All WOLv2 expects as OUTPUT: [rank] [nick] [points] [wins] [losses] 0 [disconnects]
        socket.SendString("NOTFOUND")

        RemoveClient(socket)
    End Sub

#End Region

End Class
