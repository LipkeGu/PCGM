Imports System.Net.Sockets
Imports System.IO
Imports System.Text
Imports System.Linq

Public Class LadderClient
    Inherits LadderServer

    Public Event ConnectionClosed(ByVal msg As String, ByVal e As LadderClient)
    Public Event String_MessageReceived(Message As String, ByVal ref As LadderClient)
    Public Event send_Messagesend(ByVal ref As LadderClient)
    Public _tc As LadderClient = Nothing
    Public _socket As TcpClient
    Public _stream As NetworkStream = Nothing
    Public _remaddr As String
    Public _length As Integer
    Public Event GetException(ex As String)
    Private _tmpbuffer As New List(Of Byte)

    Private Function GetHostadress(ByVal str As String) As String
        Dim _t() As String = str.Split(CChar(":"))
        Return _t(0)
    End Function

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
        Dim message As String = ""
        _length = _stream.EndRead(ar)

        If _length = 0 Then
            RaiseEvent ConnectionClosed(_remaddr, _tc)
            Throw New SocketException
        Else
            If _stream IsNot Nothing Then
                If _stream.CanRead Then
                    While _stream.DataAvailable
                        _stream.Read(buf, 0, _length)

                    End While

                    If buf.Length > 1 AndAlso _length > 2 AndAlso buf(_length - 2) = 13 AndAlso buf(_length - 1) = 10 Then
                        If Not _tmpbuffer.Count = 0 Then
                            _tmpbuffer.AddRange(buf.ToList().GetRange(0, _length))
                            message = message & Encoding.ASCII.GetString(_tmpbuffer.ToArray())
                            _tmpbuffer.Clear()
                        Else
                            message = message & Encoding.ASCII.GetString(buf, 0, _length)
                        End If
                    Else
                        _tmpbuffer.AddRange(buf.ToList().GetRange(0, _length))


                    End If
                    message = message & Encoding.ASCII.GetString(buf, 0, _length)
                    If message.Length > 0 Then
                        RaiseEvent String_MessageReceived(message, _tc)
                    End If
                    If _socket IsNot Nothing And _stream IsNot Nothing Then
                        _stream.BeginRead(buf, 0, buf.Length, AddressOf _EndRead, buf)
                    Else
                        RaiseEvent ConnectionClosed(_remaddr, _tc)
                    End If
                Else
                    Throw New SocketException
                    RaiseEvent ConnectionClosed(_remaddr, _tc)
                End If
            Else
                Throw New SocketException
                RaiseEvent ConnectionClosed(_remaddr, _tc)
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
                RaiseEvent send_Messagesend(_tc)
            Else
                Throw New SocketException
            End If
        Catch ex As IOException
            _stream = Nothing
            _socket = Nothing
            RaiseEvent ConnectionClosed("(" & _remaddr & "):" & ex.Message, _tc)
        Catch ex As SocketException
            _stream = Nothing
            _socket = Nothing
            RaiseEvent ConnectionClosed("(" & _remaddr & "):" & ex.Message, _tc)
        End Try
    End Sub


End Class
