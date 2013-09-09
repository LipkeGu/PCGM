Imports System.Xml.Serialization
Imports System.IO

Public Class xmldatei
    ''' Funktion zum Speichern - via XML-Serialisieren.
    ''' 
    Public Sub SaveXML(ByVal c As Account, ByVal filename As String)
        'Um die Daten Speichern zu können muss das Objekt an den XMLSerializer übergeben werden.
        'In diesem Fall die Instanz der Klasse ClassDresses
        Dim xml As New XmlSerializer(GetType(Account))
        Using fs As New FileStream(filename, FileMode.Create)
            xml.Serialize(fs, c)
            fs.Close()
        End Using
    End Sub

    ''' Funktion zum Laden - via XML-Serialisieren.
    ''' 
    ''' 
    Public Function LoadXML(ByVal filename As String) As Account
        'Um die Daten laden zu können muss der Typ der Klasse bekannt sein - hier ClassAdresses
        'Auch der Rückgabewert muss vom Typ der selben Klasse sein.
        Dim xml As New XmlSerializer(GetType(Account))
        Dim ret As Account
        Using fs As New FileStream(filename, FileMode.Open)
            ret = CType(xml.Deserialize(fs), Account)
            fs.Close()
        End Using
        Return ret
    End Function
End Class