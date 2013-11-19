Imports System.IO
Public Class AccProvider
    Dim xml_func As New xmldatei
    Dim lock As Object

    Public Function DoesUserExist(username As String) As Boolean
        Dim _line As String = My.Application.Info.DirectoryPath & "\Users\wol_" & username & ".xml"
        If _line.Contains("\\") Then _line.Replace("\\", "\")
        If File.Exists(_line) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function GetPassword(client_type As String, ByVal username As String) As String

        If File.Exists(My.Application.Info.DirectoryPath & "\Users\wol_" & username & ".xml") Then
            Dim acc As Account = xml_func.LoadXML(My.Application.Info.DirectoryPath & "\Users\wol_" & username & ".xml")
            SyncLock lock
                For Each a As Account.accountinfo In acc.AllAdresses
                    If a.Nickname = username Then
                        If a.Password <> "" Then
                            Return a.Password
                            Exit Function
                        Else
                            Throw New InvalidOperationException("AccountManager::GetPassword: Der Account besitzt ein leeres Password!!! (Kaputter Account?!?!): " & username)
                            Return Nothing
                        End If
                    Else
                        Return Nothing
                    End If
                Next
            End SyncLock
        Else
            Throw New InvalidOperationException("AccountManager::GetPassword: Das hätte nicht passieren sollen... ; Requested Account-Infos for: " & username)
            Return Nothing
        End If

    End Function

    Public Function AddUser(ByVal client_type As String, ByVal Username As String, ByVal Password As String, socket As WolClient) As Integer

        If Username <> "" AndAlso Password <> "" Then
            Dim a As New Account
            a.Add(Username, Password, socket.Gethostname, "1", "No")
            xml_func.SaveXML(a, My.Application.Info.DirectoryPath & "\Users\wol_" & Username & ".xml")
            Return 0
        Else
            Return 0
        End If
    End Function

    Public Function DelUser(ByVal Username As String, ByVal socket As WolClient) As Boolean
        If File.Exists(My.Application.Info.DirectoryPath & "\Users\wol_" & Username & ".xml") Then
            File.Delete(My.Application.Info.DirectoryPath & "\Users\wol_" & Username & ".xml")
            Return True
        Else
            Return False
        End If
    End Function

End Class
