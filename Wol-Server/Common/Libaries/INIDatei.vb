﻿' v1.0: Entnommen aus: http://dotnet-snippets.de/dns/klasse-fuer-verwendung-von-ini-dateien-SID938.aspx
' änderungen von www.RaphaelWolfer.de
' v1.1: BugFix - Fehler bei "DateiLöschen" behoben (Private/Public)
' v1.2: Konstuktor für Pfad hinzu gefügt
' v1.3: Exceptions bei Dateilese- und Schreibfehlern werden gefangen
Public Class INIDatei
    ' Öffentliche Klassenvariablen, Ändern des Pfades nach der Instanziierung möglich
    Public Pfad As String

    ' Konstruktor für setzen des Pfades
    ' Instanziieren mit z.B.: 
    '   Dim Inipfad As String = My.Application.Info.DirectoryPath & "\beispiel.ini"
    '   Dim ini As New INIDatei(Inipfad)
    Sub New(ByVal Pfad_der_ini As String)
        Pfad = Pfad_der_ini
    End Sub
    ' Instantiierung ohne Pfad wird nicht erlaubt
    'Sub New() 'kein Pfad notwendig...
    'End Sub

    ' DLL-Funktionen zum LESEN der INI deklarieren
    Private Declare Ansi Function GetPrivateProfileString Lib "kernel32" Alias "GetPrivateProfileStringA" ( _
        ByVal lpApplicationName As String, ByVal lpSchlüsselName As String, ByVal lpDefault As String, _
        ByVal lpReturnedString As String, ByVal nSize As Integer, ByVal lpFileName As String) As Integer

    'DLL-Funktion zum SCHREIBEN in die INI deklarieren
    Private Declare Ansi Function WritePrivateProfileString Lib "kernel32" Alias "WritePrivateProfileStringA" ( _
        ByVal lpApplicationName As String, ByVal lpKeyName As String, ByVal lpString As String, _
        ByVal lpFileName As String) As Integer

    'DLL-Funktion zum Löschen einer ganzen Sektion deklarieren
    Private Declare Ansi Function DeletePrivateProfileSection Lib "kernel32" Alias "WritePrivateProfileStringA" ( _
        ByVal Section As String, ByVal NoKey As Integer, ByVal NoSetting As Integer, _
        ByVal FileName As String) As Integer



    Public Function WertLesen(ByVal Sektion As String, ByVal Schlüssel As String, Optional ByVal Standardwert As String = "", Optional ByVal BufferSize As Integer = 1024) As String
        Try
            ' Testen, ob ein Pfad zur INI vorhanden ist
            If Pfad = "" Then
                WertLesen = ""
                Exit Function
            End If

            ' Testen, ob die Datei existiert
            If IO.File.Exists(Pfad) = False Then
                WertLesen = ""
                Exit Function
            End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        ' Auslesen des Wertes
        Dim sTemp As String = Space(BufferSize)
        Dim Length As Integer = GetPrivateProfileString(Sektion, Schlüssel, Standardwert, sTemp, BufferSize, Pfad)

        Return Left(sTemp, Length)

    End Function

    Public Sub WertSchreiben(ByVal Sektion As String, ByVal Schlüssel As String, ByVal Wert As String)
        Dim ff As Integer = FreeFile()

        WritePrivateProfileString(Sektion, Schlüssel, Wert, Pfad)
        FileClose()
    End Sub

    Public Sub SchlüsselLöschen(ByVal Sektion As String, ByVal Schlüssel As String)
        If Pfad = "" Then
            Exit Sub
        End If

        ' Testen, ob die der Order, in dem die INI liegen soll, existiert
        Dim Ordner As String
        Ordner = IO.Path.GetDirectoryName(Pfad)
        If IO.Directory.Exists(Ordner) = False Then
            Exit Sub
        End If

        ' Löschen des Schlüssels durch eine Schreiboperation durchführen
        WritePrivateProfileString(Sektion, Schlüssel, Nothing, Pfad)
    End Sub

    Public Sub SektionLöschen(ByVal Sektion As String)
        ' Testen, ob ein Pfad zur INI vorhanden ist
        If Pfad = "" Then
            Exit Sub
        End If

        ' Testen, ob die Datei existiert
        If IO.File.Exists(Pfad) = False Then
            Exit Sub
        End If

        'Löschen der Sektion durchführen
        DeletePrivateProfileSection(Sektion, 0, 0, Pfad)
    End Sub

    Public Sub BackupAnlegen(ByVal Zielpfad As String, Optional ByVal FehlermeldungAnzeigen As Boolean = False)
        'Als Zielpfad muss ein DATEIpfad angegeben werden, nicht nur der Ordner
        ' (also z.B. "D:\Test\MeinProgrammEinstellungen_Backup.ini"
        ' Testen, ob ein Pfad zur INI (der Quelldatei) vorhanden ist
        If Pfad = "" Then
            If FehlermeldungAnzeigen = True Then
            End If
            Exit Sub
        End If

        ' Testen, ob der Ordner des Zielpfades existiert
        Dim Ordner As String
        Ordner = IO.Path.GetDirectoryName(Pfad)
        If IO.Directory.Exists(Ordner) = False Then
            If FehlermeldungAnzeigen = True Then
            End If
            Exit Sub
        End If
        ' Kopie der INI erstellen
        IO.File.Copy(Pfad, Zielpfad)
    End Sub

    Public Sub DateiLöschen(Optional ByVal FehlermeldungAnzeigen As Boolean = False)
        ' Testen, ob ein Pfad zur INI (der Quelldatei) vorhanden ist
        If Pfad = "" Then
            If FehlermeldungAnzeigen = True Then
            End If
            Exit Sub
        End If

        ' Testen, ob die Datei existiert
        If IO.File.Exists(Pfad) = False Then
            If FehlermeldungAnzeigen = True Then

            End If
            Exit Sub
        End If

        ' Löschen durchführen
        IO.File.Delete(Pfad)
    End Sub

End Class