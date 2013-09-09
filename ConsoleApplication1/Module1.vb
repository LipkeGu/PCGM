﻿Module main
    'Dim WithEvents servserv As New ClassLibrary1.ServerServer
   
    Dim WithEvents WOL As New ClassLibrary1.wol_main
    Sub Main()
        WOL.INIT()

        While Console.ReadLine <> "!exit"
            Exit Sub
        End While
    End Sub

    Private Sub WOL_Report_debug(message As String) Handles WOL.Report_debug
        Console.WriteLine("[DEBUG] -> " & message)
    End Sub

    Private Sub WOL_Report_error(message As String) Handles WOL.Report_error
        Console.WriteLine("[ERROR]" & message)
    End Sub

    Private Sub WOL_Report_info(message As String) Handles WOL.Report_info
        Console.WriteLine("[INFO]" & message)
    End Sub
End Module
