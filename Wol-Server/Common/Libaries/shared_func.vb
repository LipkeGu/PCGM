Module shared_func

    Public Function GetunixTimeStamp() As String
        Return CStr((DateTime.Now - New DateTime(1970, 1, 1)).TotalMilliseconds)
    End Function

    Public Function Get_patch_informations(sku As String, version As String, ByVal min_version As String) As String

        Dim ftp_directory As String = "tibsun"
        Dim ftp_username As String = "update"
        Dim ftp_password As String = "world96"
        Dim ftp_patchsrv As String = "westwood-patch.ea.com"
        Dim newVersion As String = min_version

        ' [username] :[ftpserveraddr] [ftpusername] [ftppaswd] [path] [file.rtp] [newversion] [SKU] REQ

        Return ftp_patchsrv & " " & ftp_username & " " & ftp_password & " " & ftp_directory & " " & newVersion & "_" & version & "_" & sku & ".rtp " & newVersion & " " & sku & " REQ"

    End Function

    Public Function GetHostadress(ByVal str As String) As String
        Dim _t() As String = str.Split(CChar(":"))
        Return _t(0)
    End Function

    Public Function Convert_IPAddress_to_Long(ByVal Expression As String) As Integer
        If Expression <> "" Then
            Dim IPAddress As System.Net.IPAddress = System.Net.IPAddress.Parse(Expression)
            With IPAddress
                Return (System.Convert.ToInt32(.GetAddressBytes(3)) << 24) Or (System.Convert.ToInt32(.GetAddressBytes(2)) << 16) Or (System.Convert.ToInt32(.GetAddressBytes(1)) << 8) Or System.Convert.ToInt32(.GetAddressBytes(0))
            End With
        Else
            Return Nothing
        End If
    End Function

    Public Function get_md5hash_of_string(ByVal str As String) As String
        Dim MD5 As New System.Security.Cryptography.MD5CryptoServiceProvider
        Dim Data As Byte()
        Dim Result As Byte()
        Dim Res As String = ""
        Dim Tmp As String = ""

        Data = System.Text.Encoding.ASCII.GetBytes(str)
        Result = MD5.ComputeHash(Data)
        For i As Integer = 0 To Result.Length - 1
            Tmp = Hex(Result(i))
            If Len(Tmp) = 1 Then Tmp = "0" & Tmp
            Res += Tmp
        Next
        Return Res
    End Function

    
End Module
