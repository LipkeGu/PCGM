Imports System.Net.Sockets
Imports System.IO
Imports System.Text
Imports System.Linq

Public Class GameResClient
    Inherits GameResServer


    Public Event Packet_received(packet() As Byte)
    Public _tc As GameResClient = Nothing
    Public _socket As TcpClient
    Public _stream As NetworkStream = Nothing
    Public _remaddr As String
    Public _length As Integer
    Public Event GetException(ex As String)
    Private _tmpbuffer As New List(Of Byte)

    
    Sub New(_tcpclient As TcpClient)
        If _tcpclient IsNot Nothing Then
            If _tcpclient.Connected And _tcpclient.GetStream.CanRead Then
                _socket = _tcpclient
                _stream = _tcpclient.GetStream()

                If _stream IsNot Nothing Then
                    Dim readedbytes(_socket.ReceiveBufferSize - 1) As Byte
                    _stream.BeginRead(readedbytes, 0, readedbytes.Length, AddressOf _EndRead, readedbytes)
                Else
                    Throw New SocketException()
                End If
            Else
                Throw New SocketException
            End If
        Else
            Throw New SocketException
        End If
    End Sub

    Private Sub _EndRead(ar As IAsyncResult)
        Dim buf() As Byte = CType(ar.AsyncState, Byte())
        _length = _stream.EndRead(ar)

        If _length = 0 Then
            RemoveClient(_tc)
        Else

            While _stream.DataAvailable
                _stream.Read(buf, 0, _length)
            End While

            RaiseEvent Packet_received(buf)

            If _socket IsNot Nothing And _stream IsNot Nothing Then
                _stream.BeginRead(buf, 0, buf.Length, AddressOf _EndRead, buf)
            Else

            End If
        End If

    End Sub

    Public Sub SendString(ByVal Message As String)
        '   RaiseEvent send_Messagesend(Message)
        Try

            If _stream IsNot Nothing Then
                Dim sw As New StreamWriter(_stream)
                sw.WriteLine(Message)
                sw.Flush()
             Else
                Throw New SocketException
            End If
        Catch ex As IOException
            _stream = Nothing
            _socket = Nothing
            Catch ex As SocketException
            _stream = Nothing
            _socket = Nothing
            End Try
    End Sub


End Class
