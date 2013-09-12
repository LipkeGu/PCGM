Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Public Class RCON_Server
    Public Clients As List(Of RCONClient)
    Dim _listener As New TcpListener(IPAddress.Any, 2380)

    Public Event NewConnection(ByVal client As RCONClient)
    Public Event Datareceived(ByVal client As RCONClient, Message As String)
    Public Event ConnectionClosed(ByVal message As String)
    Public Event Server_exception(ByVal msg As String)

    Public Event handle_LISTCLCIENTS_command(ByVal socket As RCONClient)
    Public Event handle_AUTH_command(username As String, password As String, ByVal socket As RCONClient)
    Public Event handle_ANNOUNCE_command(message As String, ByVal socket As RCONClient)
    Public Event handle_QUIT_command(ByVal socket As RCONClient)

    
    Sub start_Server()
        Try
            _listener.ExclusiveAddressUse = True
            _listener.Start()

            'UPNP-Code comes here =)

            If _listener IsNot Nothing Then
                _listener.BeginAcceptTcpClient(AddressOf _endaccept, Nothing)
            End If

        Catch ex As Exception
            RaiseEvent Server_exception(ex.Message)
        End Try
    End Sub

    Private Sub _endaccept(ar As IAsyncResult)
        Dim _c As TcpClient = _listener.EndAcceptTcpClient(ar)
        Dim rc As New RCONClient(_c)

        If _c IsNot Nothing Then
            rc._reference = rc

            AddHandler rc.Connected, AddressOf Client_Connected
            AddHandler rc.Datareceived, AddressOf client_Datareceived
            AddHandler rc.Disconnect, AddressOf client_Disconnect
            AddHandler rc.Exception, AddressOf client_exception
        Else
            rc = Nothing
            _c = Nothing
        End If

        If _listener IsNot Nothing Then
            _listener.BeginAcceptTcpClient(AddressOf _endaccept, Nothing)
        End If
    End Sub

    Public Sub RemoveClient(socket As RCONClient)
        RaiseEvent ConnectionClosed("Die verbindung mit " & socket._socket.Client.RemoteEndPoint.ToString & " wurde beendet!")

        If socket IsNot Nothing Then
            If socket._socket IsNot Nothing Then
                If socket._socket.Connected Then
                    socket._socket.Client.Disconnect(False)
                    socket._socket.Client.Close()
                End If

                If socket._socket.GetStream IsNot Nothing Then
                    socket._socket.GetStream.Close()
                End If
            End If
        End If

        If Clients.Contains(socket) Then
            Clients.Remove(socket)
        End If
    End Sub



    Private Sub Client_Connected(socket As RCONClient)
        If Not Clients.Contains(socket) Then
            Clients.Add(socket)
            RaiseEvent NewConnection(socket)
        End If
    End Sub

    Private Sub client_Datareceived(socket As RCONClient, message As String)
        RaiseEvent Datareceived(socket, message)
    End Sub

    Private Sub client_Disconnect(socket As RCONClient)
        RemoveClient(socket)
    End Sub

    Private Sub client_exception(msg As String, socket As RCONClient)
        RaiseEvent Server_exception(msg)
    End Sub

    Private Sub RCON_Server_Datareceived(client As RCONClient, Message As String) Handles Me.Datareceived
        Dim params() As String = Message.Split(CChar("|"))

        For i As Integer = 0 To params.Length - 1
            If params(0).Contains(":AUTH") Then
                RaiseEvent handle_AUTH_command(params(1), params(2), client)
            ElseIf params(0).Contains(":QUIT") Then
                RaiseEvent handle_QUIT_command(client)
            ElseIf params(0).Contains(":ANNOUNCE") Then
                RaiseEvent handle_ANNOUNCE_command(params(1), client)
            Else
                Throw New NotImplementedException(params(0) & ": Unknown Command")
            End If
        Next

    End Sub
End Class
