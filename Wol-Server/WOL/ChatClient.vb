Imports System.Net.Sockets
Imports System.IO
Imports System.Text
Imports System.Linq

Public Class WolClient
    Inherits WolServer
    Public Event ClientState(ByVal msg As String)
    Public Event ConnectionClosed(ByVal msg As String, ByVal e As WolClient)
    Public Event String_MessageReceived(Message As String, ByVal ref As WolClient)
    Public Event GetException(ex As String)

    Public _tc As WolClient = Nothing
    Public _socket As TcpClient = Nothing
    Public _stream As NetworkStream = Nothing
    Public _remaddr As String
    Public _length As Integer

    Dim _tmpbuffer As New List(Of Byte)

#Region "ClientInfos"
    Private _nick As String = ""
    Private _apgar As String = ""
    Private _locale As Integer = 5
    Private _wolversion As String = "0"
    Private _iswolclient As Boolean = False
    Private _codepage As Integer = 1252
    Private _serial As String = ""
    Private _curchan As String = ""
    Private _registered As Boolean = True
    Private _allow_tournament As Boolean = False
    Private _longip As Long
    Private _apgarparam2 As String
    Private _gameid As Integer
    Private _reported_missing_serial As Boolean = False
    Private _gamever As Integer
    Private _opt1 As Integer
    Private _gameoptions As String = ""
    Private _opt2 As Integer
    Private _hostname As String
    Private _hostmask As String = ""
    Private _needspatch As Boolean = False
    Private _lobbycount As Integer = 0
    Private _cver1 As Integer
    Private _cver2 As Integer
    Private _squadid As Integer = 0


    Public Property wolversion As String
        Get
            Return _wolversion
        End Get
        Set(value As String)
            _wolversion = value
        End Set
    End Property

    Public Property TournamentAllowed_State As Boolean
        Get
            Return _allow_tournament
        End Get
        Set(value As Boolean)
            _allow_tournament = value
        End Set
    End Property

    Public ReadOnly Property Hostmask As String
        Get
            Return _nick & "!" & Nick & "@" & Hostname
        End Get
    End Property

    Public Property Serial_Report As Boolean
        Get
            Return _reported_missing_serial
        End Get
        Set(value As Boolean)
            _reported_missing_serial = value
        End Set
    End Property

    Public Property Squadid As String
        Get
            Return CStr(_squadid)
        End Get
        Set(value As String)
            _squadid = CInt(value)
        End Set

    End Property

    Public Property Is_wolhost As Boolean
        Get
            Return _iswolclient
        End Get
        Set(value As Boolean)
            _iswolclient = value
        End Set
    End Property

    Public Property GameID As String
        Set(value As String)
            _gameid = CInt(value)
        End Set
        Get
            Return CStr(_gameid)
        End Get
    End Property

    Public Property Current_Channel As String
        Get
            Return _curchan
        End Get
        Set(value As String)
            _curchan = value
        End Set
    End Property

    Public Property Nick As String
        Get
            Return CStr(_nick)
        End Get
        Set(value As String)
            _nick = value
        End Set
    End Property

    Public Property Cver1 As String
        Get
            Return CStr(_cver1)
        End Get
        Set(value As String)
            _cver1 = CInt(value)
        End Set
    End Property

    Public Property Cver2 As String
        Set(value As String)
            _cver2 = CInt(value)
        End Set
        Get
            Return CStr(_cver2)
        End Get
    End Property

    Public Property Hostname As String
        Get
            Return _hostname
        End Get
        Set(value As String)
            _hostname = value
        End Set
    End Property

    Public Property Registered As Boolean
        Get
            Return _registered
        End Get
        Set(value As Boolean)
            _registered = value
        End Set
    End Property

    Public Property opt1 As String
        Get
            Return CStr(_opt1)
        End Get
        Set(value As String)
            _opt1 = CInt(value)
        End Set
    End Property

    Public Property opt2 As Integer
        Set(value As Integer)
            _opt2 = value
        End Set
        Get
            Return _opt2
        End Get
    End Property

    Public Property codepage As String
        Get
            Return CStr(_codepage)
        End Get
        Set(value As String)
            _codepage = CInt(value)
        End Set
    End Property

    Public Property locale As String
        Get
            Return CStr(_locale)
        End Get
        Set(value As String)
            _locale = CInt(value)
        End Set
    End Property

    Public Property GameVersion As String
        Get
            Return CStr(_gamever)
        End Get
        Set(value As String)
            _gamever = CInt(value)
        End Set
    End Property

    Public Property password As String
        Get
            Return _apgar
        End Get
        Set(value As String)
            _apgar = value
        End Set
    End Property

    Public ReadOnly Property IPasLong As String
        Get
            Return CStr(_longip)
        End Get
    End Property

    Public Property Serial As String
        Get
            Return _serial
        End Get
        Set(value As String)
            _serial = value
        End Set
    End Property

#End Region

    Public Sub New(socket As TcpClient)
        _socket = socket
        If socket.Connected Then
            _longip = Convert_IPAddress_to_Long(_remaddr)
            _stream = socket.GetStream()
            If _stream.CanRead And _stream IsNot Nothing Then
                Dim readedbytes(socket.ReceiveBufferSize - 1) As Byte
                _stream.BeginRead(readedbytes, 0, readedbytes.Length, AddressOf _EndRead, readedbytes)
            Else
                Throw New IOException
            End If
        Else
            Throw New IOException
        End If
    End Sub

    Private Sub _EndRead(ar As IAsyncResult)
        Try
            Dim buf() As Byte = CType(ar.AsyncState, Byte())
            Dim message As String = ""
            _length = _stream.EndRead(ar)
            _longip = Convert_IPAddress_to_Long(_remaddr)

            If _length = 0 Then
                If _stream IsNot Nothing Then
                    _stream.Close()
                    _stream.Dispose()
                End If

                If _socket IsNot Nothing Then
                    _socket.Close()
                End If

                RaiseEvent ConnectionClosed(_remaddr, _tc)
                Exit Sub
            Else
                If Hostname Is Nothing Then
                    Hostname = GetHostadress(_socket.Client.RemoteEndPoint.ToString)
                End If

                If _stream IsNot Nothing Then
                    If _stream.CanRead Then
                        While _stream.DataAvailable And _socket.Connected And _stream.CanRead
                            If _stream Is Nothing Then
                                RaiseEvent ConnectionClosed(_remaddr, _tc)
                            Else
                                _stream.Read(buf, 0, _length)
                            End If
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

                        If message.Length > 1 Then
                            RaiseEvent String_MessageReceived(message, _tc)
                        End If

                        message = Nothing

                        If _socket IsNot Nothing And _stream IsNot Nothing Then
                            _stream.BeginRead(buf, 0, buf.Length, AddressOf _EndRead, buf)
                        Else
                            RaiseEvent ConnectionClosed(_remaddr, _tc)
                        End If
                    Else
                        _stream = Nothing
                        _socket = Nothing
                        RaiseEvent ConnectionClosed(_remaddr, _tc)
                    End If
                Else
                    RaiseEvent ConnectionClosed(_remaddr, _tc)
                End If
            End If
        Catch ex As IOException
            RaiseEvent ConnectionClosed("(" & _remaddr & "):" & ex.Message, _tc)
        Catch ex As SocketException
            RaiseEvent ConnectionClosed("(" & _remaddr & "):" & ex.Message, _tc)
        End Try

    End Sub

    Public Sub Send(ByVal Message As String)
        If _stream IsNot Nothing Then
            Dim sw As New StreamWriter(_stream)
            sw.WriteLine(Message)
            sw.Flush()
            RaiseEvent ClientState("Gesendet:" & Message)
        Else
            Throw New IOException
        End If
    End Sub

End Class
