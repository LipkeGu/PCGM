Imports System.Text

Public Class Versioncheck
    Public Function GetAllowedVersionbySKU(ByVal Client_Type As String, ByVal ClientID As String) As Double
        Dim _temp As Double = Nothing
        Dim Ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\Version.ini")
        _temp = CDbl(Ini_func.WertLesen(Client_Type, ClientID))

        Return _temp

    End Function

    Public Function GetClient_GroupbySKU(ByVal client_type As String, ByVal sku As String) As String
        Dim _temp As String = Nothing
        Dim Ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\ClientGroups.ini")
        _temp = Ini_func.WertLesen(client_type, sku)

        Return _temp

    End Function

End Class
