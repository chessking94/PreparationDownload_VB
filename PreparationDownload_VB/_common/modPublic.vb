Imports System.IO
Imports System.Text

Module modPublic
    Public Sub RunProcess()
        Dim objl_Classes As New List(Of clsBase)
        Dim IsThreaded As Boolean = False

        If MainWindow.Site = "All" OrElse MainWindow.Site = "Chess.com" Then
            Dim objl_process As New clsProcessing With {.CDC = New clsCDC}
            objl_Classes.Add(objl_process)
        End If

        If MainWindow.Site = "All" OrElse MainWindow.Site = "Lichess" Then
            Dim objl_process As New clsProcessing With {.Lichess = New clsLichess}
            objl_Classes.Add(objl_process)
        End If

        For Each obj_class In objl_Classes
            If IsThreaded Then
                'TODO: This needs some work, want it to download all files simultaneously but that isn't what it does currently
                obj_class.StartThreaded()
                obj_class.WaitForEnd(True)
            Else
                obj_class.StartNonThreaded()
            End If
        Next
    End Sub

    Public Sub RunCommand(command As String, Optional workingDir As String = Nothing, Optional arguments As String = Nothing, Optional permanent As Boolean = False, Optional waitForEnd As Boolean = True)
        'based on https://stackoverflow.com/a/10263144
        Dim p As Process = New Process()
        Dim pi As ProcessStartInfo = New ProcessStartInfo()
        pi.CreateNoWindow = True

        If Not String.IsNullOrWhiteSpace(workingDir) Then pi.WorkingDirectory = workingDir  'TODO: validate directory exists
        pi.Arguments = " " + If(permanent = True, "/K", "/C") + " " + command
        If Not String.IsNullOrWhiteSpace(arguments) Then
            pi.Arguments += " "
            pi.Arguments += arguments
        End If
        pi.FileName = "cmd.exe"
        p.StartInfo = pi
        p.Start()
        If waitForEnd Then p.WaitForExit()
    End Sub

    Public Sub ReplaceTextInFile(fileName As String, originalText As String, newText As String, Optional fileEncoding As String = Nothing)
        Dim encodingType As Encoding
        Try
            encodingType = Encoding.GetEncoding(fileEncoding)
        Catch ex As Exception
            encodingType = Encoding.UTF8
        End Try

        Dim lines As New List(Of String)()
        Using reader As New StreamReader(fileName, encodingType)
            While reader.Peek <> -1
                Dim line As String = reader.ReadLine()
                lines.Add(line)
            End While
        End Using

        For i As Integer = 0 To lines.Count - 1
            lines(i) = lines(i).Replace(originalText, newText)
        Next

        Using writer As New StreamWriter(fileName, False, encodingType)
            For Each line As String In lines
                writer.WriteLine(line)
            Next
        End Using
    End Sub

    Public Function AppendText(str_Input, str_Append) As String
        Dim delim As String = vbCrLf

        If str_Input = "" OrElse str_Input Is Nothing Then
            Return str_Append
        Else
            If str_Append = "" Then
                Return str_Input
            Else
                str_Input += delim
                str_Input += str_Append
                Return str_Input
            End If
        End If
    End Function
End Module
