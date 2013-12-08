Public Class chanprovider

    Public channels As New List(Of Chatchannel)

    Dim lock As New Object

    Public Sub AddChannels(group As Integer)
        SyncLock lock
            Dim ini_func As New INIDatei(My.Application.Info.DirectoryPath & "\conf\Channels.ini")
            Dim _line As String = ini_func.WertLesen("INFO", CStr(group) & "_count")

            If _line <> "" Then
                Dim count As Integer = CInt(_line)
                For i As Integer = 0 To count - 1
                    _line = ini_func.WertLesen(CStr(group), CStr(i))
                    If _line <> "" Then
                        Dim c As New Chatchannel
                        With c
                            .Chan_Name = _line
                            .Chan_Key = ""
                            .Chan_ListType = "0"
                            .Chan_Max_Users = 30
                            .Chan_Min_Users = 0
                            .Chan_Gameid = CStr(group)
                            .Is_Tournament = "0"
                            .Chan_Reserved = ""
                            .Chan_GameEx = ""
                        End With
                        If Not channels.Contains(c) Then
                            channels.Add(c)
                        End If
                    End If
                Next
            End If
        End SyncLock
    End Sub
End Class
