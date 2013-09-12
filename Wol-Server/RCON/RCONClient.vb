Imports System.Net
Imports System.Net.Sockets
Imports System.Text

Public Class RCONClient
    Public Event Connected(ByVal socket As RCONClient)
    Public Event Disconnect(ByVal socket As RCONClient)
    Public Event Datareceived(ByVal socket As RCONClient, ByVal message As String)
    Public Event Exception(ByVal msg As String, ByVal socket As RCONClient)

    Public _reference As RCONClient
    Protected _remaddr As IPEndPoint
    Public _socket As TcpClient
    Protected _stream As NetworkStream
    Protected buffer(4096) As Byte
    Protected _user As String
    Protected _pass As String

    Sub New(socket As TcpClient)
        _socket = socket

        If socket.Connected AndAlso _reference IsNot Nothing Then
            _stream = _socket.GetStream()
            RaiseEvent Connected(_reference)
            _stream.BeginRead(buffer, 0, buffer.Length, AddressOf _endread, buffer)
        Else
            _socket = Nothing
        End If
    End Sub

    Private Sub _endread(ByVal ar As IAsyncResult)
        Try
            Dim length As Integer = _stream.EndRead(ar)
            Dim message As String = ""

            If Not length = 0 Then
                While _stream.DataAvailable
                    length = _stream.Read(buffer, 0, buffer.Length)

                End While

                message = Encoding.ASCII.GetString(buffer, 0, buffer.Length)

                If message.Length > 2 AndAlso message.StartsWith(":") Then
                    RaiseEvent Datareceived(_reference, message)
                End If

                If _stream IsNot Nothing Then
                    _stream.BeginRead(buffer, 0, buffer.Length, AddressOf _endread, buffer)
                Else
                    RaiseEvent Disconnect(_reference)
                End If
            Else
                RaiseEvent Disconnect(_reference)
            End If
        Catch ex As IO.IOException
            RaiseEvent Exception(ex.Message, _reference)
            RaiseEvent Disconnect(_reference)
        End Try
    End Sub

    Public Sub send(message As String)
        Try
            If message.Length > 2 Then
                If _stream IsNot Nothing Then

                    Dim _writebuffer() As Byte = Encoding.ASCII.GetBytes(message)
                    _stream.Write(_writebuffer, 0, _writebuffer.Length)
                    _stream.Flush()
                End If
            Else
                RaiseEvent Disconnect(_reference)
            End If
        Catch ex As IO.IOException
            RaiseEvent Disconnect(_reference)
        End Try

    End Sub

End Class
