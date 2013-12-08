Imports System.IO
Imports System.Xml.Serialization

Public Class Account

    ''' Structure welche eine einzelne Adresse enthält.
    ''' 
    Public Structure accountinfo
        Public Nickname As String
        Public Password As String
        Public lastlogin As String
        Public userlevel As String
        Public lasthostip As String
        Public isbanned As String
        



        Public Sub New(ByVal nick As String, ByVal pass As String, ByVal host As String, ByVal usrlevl As String, ByVal banned As String)
            Nickname = nick
            Password = pass
            userlevel = usrlevl
            lastlogin = GetunixTimeStamp()
            lasthostip = host
            isbanned = banned

        End Sub
    End Structure

    Private _accounts As New List(Of accountinfo)

    Public Sub New()

    End Sub

    Public Sub Add(ByVal nick As String, ByVal pass As String, ByVal host As String, ByVal usrlevl As String, ByVal banned As String)
        _accounts.Add(New accountinfo(nick, pass, host, usrlevl, banned))
    End Sub

    Public Property AllAdresses() As List(Of accountinfo)
        Get
            Return _accounts
        End Get
        Set(ByVal value As List(Of accountinfo))
            _accounts = value
        End Set
    End Property
End Class