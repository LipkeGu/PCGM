Public Class Chatchannel
    Inherits chanprovider

    Public sockets As New List(Of WolClient)
    Private _name As String
    Private _topic As String
    Private _key As String
    Private _gameid As String
    Private _owner As WolClient
    Public bans As New List(Of banlist)
    Private _listtype As Integer
    Private _minUsers As Integer
    Private _maxUsers As Integer
    Private _tournament As String
    Private _ingame As Integer
    Private _ipaddr As String
    Private _ipaslong As String
    Private _flags As Long = 388
    Private _reserved As String
    Private _latency As Integer
    Private _hidden As Integer
    Private _location As String
    Private _exInfo As String
    Private _extension As String

#Region "Channel-Modes"
    Protected _is_Moderated As Boolean = False
    Protected _is_inviteonly As Boolean = False
    Protected _is_permanent As Boolean = False
#End Region

    Public Structure banlist
        Dim hostmask As String
    End Structure


    '  Known channel game types:
    '  0 = Westwood Chat channels, 
    '  1 = Command & Conquer Win95 channels,
    '  2 = Red Alert Win95 channels,
    '  3 = Red Alert Counterstrike channels, 
    '  4 = Red Alert Aftermath channels, 
    '  5 = CnC Sole Survivor channels,
    '  12 = C&C Renegade channels, 
    '  14 = Dune 2000 channels, 
    '  16 = Nox channels, 
    '  18 = Tiberian Sun channels,
    '  21 = Red Alert 1 v 3.03 channels, 
    '  31 = Emperor: Battle for Dune, 
    '  33 = Red Alert 2,
    '  37 = Nox Quest channels, 
    '  38, = Quickgame channels
    '  39, = Quickgame channels
    '  40 = Quickgame channels, 
    '  41 = Yuri's Revenge

    Public ReadOnly Property GetOwnerIPaslong As String
        Get
            Return _ipaslong
        End Get
    End Property

    Public ReadOnly Property GetOwner As WolClient
        Get
            Return _owner
        End Get
    End Property

    Public WriteOnly Property SetOwnerIPasLong As String
        Set(value As String)
            _ipaslong = value
        End Set
    End Property

    Public WriteOnly Property SetListType As String
        Set(value As String)
            _listtype = CInt(value)
        End Set
    End Property

    Public WriteOnly Property SetKey As String
        Set(value As String)
            _key = value
        End Set
    End Property

    Public ReadOnly Property GetTopic As String
        Get
            Return _topic
        End Get
    End Property

    Public WriteOnly Property SetIP As String
        Set(value As String)
            _ipaddr = value
        End Set
    End Property

    Public WriteOnly Property SetTopic As String
        Set(value As String)
            _topic = value
        End Set
    End Property

    Public WriteOnly Property SetGameEx As String
        Set(value As String)
            _extension = value
        End Set
    End Property

    Public ReadOnly Property GetGameEx As String
        Get
            Return _extension
        End Get
    End Property

    Public ReadOnly Property GetName As String
        Get
            Return _name
        End Get
    End Property

    Public ReadOnly Property LIstType As String
        Get
            Return CStr(_listtype)
        End Get
    End Property

    Public ReadOnly Property GetGameid As String
        Get
            Return CStr(_gameid)
        End Get
    End Property

    Public WriteOnly Property SetGameID As String
        Set(value As String)
            _gameid = value
        End Set
    End Property

    Public ReadOnly Property Min_Users As String
        Get
            Return CStr(_minUsers)
        End Get
    End Property

    Public WriteOnly Property SetTournament As String
        Set(value As String)
            _tournament = value
        End Set
    End Property

    Public WriteOnly Property SetMinUsers As Integer
        Set(value As Integer)
            _minUsers = value
        End Set
    End Property

    Public WriteOnly Property SetMasUsers As Integer
        Set(value As Integer)
            _maxUsers = value
        End Set
    End Property

    Public WriteOnly Property Owner As WolClient
        Set(value As WolClient)
            _owner = value
        End Set
    End Property

    Public ReadOnly Property Max_Users As String
        Get
            Return CStr(_maxUsers)
        End Get
    End Property

    Public ReadOnly Property GetFlags As String
        Get
            Return CStr(_flags)
        End Get

    End Property

    Public WriteOnly Property SetName As String
        Set(value As String)
            _name = value
        End Set
    End Property

    Public ReadOnly Property key As String
        Get
            Return _key
        End Get
    End Property

    Public ReadOnly Property ISTournament As String
        Get
            Return _tournament
        End Get
    End Property

    Public ReadOnly Property num_Users As String
        Get
            Return CStr(sockets.Count)
        End Get
    End Property

    Public ReadOnly Property GetReserved As String
        Get
            Return CStr(_reserved)
        End Get
    End Property

    Public WriteOnly Property Setreserved As String
        Set(value As String)
            _reserved = value
        End Set
    End Property

    Public ReadOnly Property GetIP As String
        Get
            Return _ipaddr
        End Get
    End Property



    Public CHAN_PERMANENT As Integer = 4
    Public CHAN_LOBBY As Integer = 128
    Public CHAN_OFFICIAL As Integer = 256
End Class
