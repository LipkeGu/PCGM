Imports System.Net.Sockets
Imports System.Net
Imports System.IO
Imports System.Text

Public Class GameResServer
#Region "Events"
    Public Event ServerState(ByVal e As String)
    Public Event NewConnection(ByVal e As String)
    Public Event Connection_closed(ByVal msg As String)
    Public Event byteDatareceived(ByVal Message() As Byte)
#End Region
#Region "Server"
    Private _ref As GameResClient

    Public ConnectedClients As New List(Of GameResClient)
    Private _Listener As TcpListener
    Private lock As New Object

    Public ReadOnly Property Num_Clients As String
        Get
            Return CStr(ConnectedClients.Count)
        End Get
    End Property

    Public Sub StartServer(ByVal port As Integer, ByVal srvname As String, ByVal irc_network As String)
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

    Private Sub _EndAccept(ar As IAsyncResult)
        Dim c As New GameResClient(_Listener.EndAcceptTcpClient(ar))

        If ar IsNot Nothing Then
            c._remaddr = shared_func.GetHostadress(c._socket.Client.RemoteEndPoint.ToString)

            AddHandler c.Packet_received, AddressOf GameResServer_byteDatareceived
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

    Private Sub Client_Connection_Closed(ByVal msg As String, ByVal ref As GameResClient)
        SyncLock lock
            ConnectedClients.Remove(ref)
            RaiseEvent Connection_closed(msg)
        End SyncLock
    End Sub

    Public Sub RemoveClient(ByVal socket As GameResClient)
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
    Private Sub Client_NewConnection(e As String)
        RaiseEvent NewConnection(e)
    End Sub

    Private Sub GameResServer_byteDatareceived(Message() As Byte)

        For i As Integer = 0 To Message.Length - 1
            RaiseEvent ServerState("Byte at Position " & i & " :" & Message(i).ToString)
        Next
    End Sub
End Class
