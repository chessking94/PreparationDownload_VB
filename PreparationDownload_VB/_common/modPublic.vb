Imports System.IO
Imports System.Text

Module modPublic
    Public Sub RunProcess()
        'archive any old files that may be sitting in directory, just in case
        Dim archiveDir As String = Path.Combine(clsBase.rootDir, "Archive")
        If Not Directory.Exists(archiveDir) Then
            Directory.CreateDirectory(archiveDir)
        End If

        Dim existingFiles As String() = Directory.GetFiles(clsBase.rootDir)
        For Each srcFile As String In existingFiles
            Dim destFile As String = Path.Combine(archiveDir, Path.GetFileName(srcFile))
            File.Move(srcFile, destFile)
        Next

        'do the fun part now
        Dim processes As New clsProcessing
        processes.initializeConfig()
        If MainWindow.Site = "All" OrElse MainWindow.Site = "Chess.com" Then
            processes.CDC = New clsCDC
        End If

        If MainWindow.Site = "All" OrElse MainWindow.Site = "Lichess" Then
            processes.Lichess = New clsLichess
        End If

        Dim IsThreaded As Boolean = False
        If IsThreaded Then
            'TODO: This needs some work, want it to download all files simultaneously but that isn't what it does currently
            'probably would need to use a background worker or something
            processes.StartThreaded()
            processes.WaitForEnd(True)
        Else
            processes.StartNonThreaded()
        End If
    End Sub

    Public Sub RunCommand(pi_command As String, Optional pi_workingDir As String = Nothing, Optional pi_arguments As String = Nothing, Optional pi_permanent As Boolean = False, Optional pi_waitForEnd As Boolean = True)
        'based on https://stackoverflow.com/a/10263144
        Dim process As Process = New Process()
        Dim processInfo As ProcessStartInfo = New ProcessStartInfo()
        processInfo.CreateNoWindow = True

        If Not Directory.Exists(pi_workingDir) Then
            Throw New DirectoryNotFoundException
        End If

        If Not String.IsNullOrWhiteSpace(pi_workingDir) Then processInfo.WorkingDirectory = pi_workingDir
        processInfo.Arguments = " " + If(pi_permanent = True, "/K", "/C") + " " + Command()

        If Not String.IsNullOrWhiteSpace(pi_arguments) Then
            processInfo.Arguments += " "
            processInfo.Arguments += pi_arguments
        End If
        processInfo.FileName = "cmd.exe"
        process.StartInfo = processInfo
        process.Start()
        If pi_waitForEnd Then process.WaitForExit()
    End Sub

    Public Sub ReplaceTextInFile(pi_fileName As String, pi_originalText As String, pi_newText As String, Optional pi_fileEncoding As String = Nothing)
        Dim encodingType As Encoding
        Try
            encodingType = Encoding.GetEncoding(pi_fileEncoding)
        Catch ex As Exception
            encodingType = Encoding.UTF8
        End Try

        Dim lines As New List(Of String)()
        Using reader As New StreamReader(pi_fileName, encodingType)
            While reader.Peek <> -1
                Dim line As String = reader.ReadLine()
                lines.Add(line)
            End While
        End Using

        For i As Integer = 0 To lines.Count - 1
            lines(i) = lines(i).Replace(pi_originalText, pi_newText)
        Next

        Using writer As New StreamWriter(pi_fileName, False, encodingType)
            For Each line As String In lines
                writer.WriteLine(line)
            Next
        End Using
    End Sub

    Public Function AppendText(pi_Input As String, pi_Append As String, Optional pi_Delimiter As String = vbCrLf) As String
        If String.IsNullOrWhiteSpace(pi_Input) Then
            Return pi_Append
        Else
            If pi_Append = "" Then
                Return pi_Input
            Else
                pi_Input += pi_Delimiter
                pi_Input += pi_Append
                Return pi_Input
            End If
        End If
    End Function

    Public Function ConnectionString(Optional pi_Database As String = "ChessWarehouse", Optional pi_Application As String = "") As String
        Return _
            "Server=localhost" &
            ";Database=" & pi_Database &
            ";Integrated Security=SSPI" &
            ";Application Name=" & pi_Application &
            ";MultipleActiveResultSets=True"
    End Function

    Public Function Connection(Optional pi_Database As String = "ChessWarehouse", Optional pi_Application As String = "") As System.Data.SqlClient.SqlConnection
        Dim connection As New System.Data.SqlClient.SqlConnection(ConnectionString(pi_Database, pi_Application))  'TODO: Switch to Microsoft.Data.SqlClient
        connection.Open()
        Return connection
    End Function
End Module
