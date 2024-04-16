Imports System.IO
Imports System.Reflection

Public Class clsProcessing : Inherits clsBase
    Public Property CDC As clsCDC
    Public Property Lichess As clsLichess

    Protected Overrides Sub Go_Child()
        Dim objl_Parameters As New _clsParameters
        With objl_Parameters
            .FirstName = MainWindow.FirstName
            .LastName = MainWindow.LastName
            .Username = MainWindow.Username
            .Site = MainWindow.Site
            .TimeControl = MainWindow.TimeControl
            .Color = MainWindow.Color
            .StartDate = MainWindow.StartDate
            .EndDate = MainWindow.EndDate
        End With
        objl_Parameters.Clean()

        If MainWindow.WriteLog Then
            'TODO: Option to insert record to database for logging
        End If

        If Lichess IsNot Nothing Then
            BeforeDownload(Lichess.outputDir)
            Lichess.DownloadGames(objl_Parameters)
            AfterDownload(Lichess.cSite, Lichess.outputDir)
        End If

        If CDC IsNot Nothing Then
            BeforeDownload(CDC.outputDir)
            CDC.DownloadGames(objl_Parameters)
            AfterDownload(CDC.cSite, CDC.outputDir)
        End If

        ProcessGames(objl_Parameters)

        If MainWindow.WriteLog Then
            'TODO: Update previously created database logging record if necessary
        End If
    End Sub

    Friend Sub BeforeDownload(outputDir As String)
        If Directory.Exists(outputDir) Then
            Directory.Delete(outputDir, True)
        End If

        Directory.CreateDirectory(outputDir)
    End Sub

    Friend Sub AfterDownload(Site As String, outputDir As String)
        'merge all files
        Dim mergeName As String = $"{Site}_Merged_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"copy /B *.pgn {mergeName} >nul", outputDir)

        'clean with pgn-extract - TODO: Figure out a way to package pgn-extract with this project, so it doesn't have to be called as an external dependency
        Dim cleanName As String = $"{Site}_Cleaned_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"pgn-extract -N -V -D -pl2 --quiet --nosetuptags --output {cleanName} {mergeName} >nul", outputDir)

        If Site = "Chess.com" Then
            'TODO: Need to remove non-standard games - can this be a function/sub in clsCDC instead of here?
        End If

        'post-process clean-up
        File.Move(Path.Combine(outputDir, cleanName), Path.Combine(rootDir, cleanName))
        Directory.Delete(outputDir, True)
    End Sub

    Friend Sub ProcessGames(pi_Parameters As _clsParameters)
        Dim playerName As String = GetPlayerNameForFile(pi_Parameters)

        'merge the site files into a single file
        Dim mergeName As String = $"{playerName}_Merged_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"copy /B *.pgn {mergeName} >nul", rootDir)

        'extract time control games
        Dim timeControlName As String = mergeName
        If pi_Parameters.TimeControl <> "All" Then
            Dim minSeconds As Long = GetTimeControlLimits(pi_Parameters.TimeControl, "Min")
            Dim maxSeconds As Long = GetTimeControlLimits(pi_Parameters.TimeControl, "Max")

            'create temporarary tag files
            Dim minTagName As String = "TimeControlTagMin.txt"
            Dim minTagFile As String = Path.Combine(rootDir, minTagName)
            Using writer As New StreamWriter(minTagFile)
                writer.WriteLine($"TimeControl >= ""{minSeconds}""")
            End Using

            Dim maxTagName As String = "TimeControlTagMax.txt"
            Dim maxTagFile As String = Path.Combine(rootDir, maxTagName)
            Using writer As New StreamWriter(maxTagFile)
                writer.WriteLine($"TimeControl <= ""{maxSeconds}""")
            End Using

            'filter min
            Dim tempTimeControlFile As String = Path.Combine(rootDir, $"temp{pi_Parameters.TimeControl}_{mergeName}.pgn")
            RunCommand($"pgn-extract --quiet -t{minTagName} --output {tempTimeControlFile} {mergeName} >nul", rootDir)

            'filter max
            timeControlName = $"{pi_Parameters.TimeControl}_{mergeName}"
            RunCommand($"pgn-extract --quiet -t{maxTagName} --output {timeControlName} {tempTimeControlFile} >nul", rootDir)
        End If

        'extract start date games
        Dim startDateName As String = timeControlName
        If pi_Parameters.StartDate <> Date.MinValue Then
            Dim startDatePGNFormat As String = FormatDateForPGN(pi_Parameters.StartDate)

            'create temporary tag file
            Dim startTagName As String = "StartDateTag.txt"
            Dim startTagFile As String = Path.Combine(rootDir, startTagName)
            Using writer As New StreamWriter(startTagFile)
                writer.WriteLine($"Date >= ""{startDatePGNFormat}""")
            End Using

            'filter start date
            startDateName = $"SD_{timeControlName}"
            RunCommand($"pgn-extract --quiet -t{startTagName} --output {startDateName} {timeControlName} >nul", rootDir)
        End If

        'extract end date games
        Dim endDateName As String = startDateName
        If pi_Parameters.EndDate <> Date.MinValue Then
            Dim endDatePGNFormat As String = FormatDateForPGN(pi_Parameters.EndDate)

            'create temporary tag file
            Dim endTagName As String = "EndDateTag.txt"
            Dim endTagFile As String = Path.Combine(rootDir, endTagName)
            Using writer As New StreamWriter(endTagFile)
                writer.WriteLine($"Date >= ""{endDatePGNFormat}""")
            End Using

            'filter start date
            endDateName = $"ED_{startDateName}"
            RunCommand($"pgn-extract --quiet -t{endTagName} --output {endDateName} {startDateName} >nul", rootDir)
        End If

        'sort games - TODO: how to do? Maybe there's a PGN parsing library somewhere (or pgn-extract can do it)
        Dim sortName As String = endDateName

        'set final file names
        Dim baseName As String = SetBaseOutputName(pi_Parameters)
        Dim whiteName As String = $"{baseName}_White.pgn"
        Dim blackName As String = $"{baseName}_Black.pgn"
        Dim combinedName As String = $"{baseName}_Combined.pgn"
        Dim keepFiles As New List(Of String)

        'split into White/Black game files
        'create temporary tag files
        Dim whiteTagName As String = "WhiteTag.txt"
        Dim whiteTagFile As String = Path.Combine(rootDir, whiteTagName)
        Using writer As New StreamWriter(whiteTagFile)
            writer.WriteLine($"White ""{playerName.Replace(nameDelimiter, ", ")}""")
        End Using

        Dim blackTagName As String = "BlackTag.txt"
        Dim blackTagFile As String = Path.Combine(rootDir, blackTagName)
        Using writer As New StreamWriter(blackTagFile)
            writer.WriteLine($"Black ""{playerName.Replace(nameDelimiter, ", ")}""")
        End Using

        Dim whiteList As New List(Of String) From {"Both", "White"}
        If whiteList.Contains(pi_Parameters.Color) Then
            RunCommand($"pgn-extract --quiet -t{whiteTagName} --output {whiteName} {sortName} >nul", rootDir)
            keepFiles.Add(Path.Combine(rootDir, whiteName))
        End If

        Dim blackList As New List(Of String) From {"Both", "Black"}
        If blackList.Contains(pi_Parameters.Color) Then
            RunCommand($"pgn-extract --quiet -t{blackTagName} --output {blackName} {sortName} >nul", rootDir)
            keepFiles.Add(Path.Combine(rootDir, blackName))
        End If

        File.Move(Path.Combine(rootDir, sortName), Path.Combine(rootDir, combinedName))
        If pi_Parameters.Color = "Both" Then
            keepFiles.Add(Path.Combine(rootDir, combinedName))
        End If

        'clean up directory
        Dim filesInRoot As String() = Directory.GetFiles(rootDir)
        For Each f As String In filesInRoot
            If Not keepFiles.Contains(f) Then
                File.Delete(f)
            End If
        Next

        'TODO: count games for logging
    End Sub

    Friend Function CreateUserList(Site As String, objm_Parameters As _clsParameters)
        Dim objl_CMD As New Data.SqlClient.SqlCommand With {.Connection = Connection(strv_Application:=Assembly.GetCallingAssembly().GetName().Name)}

        If objm_Parameters.GetUsername Then
            objl_CMD.CommandText = clsSqlQueries.FirstLast()
            objl_CMD.Parameters.AddWithValue("@Source", Site)
            objl_CMD.Parameters.AddWithValue("@LastName", objm_Parameters.LastName)
            objl_CMD.Parameters.AddWithValue("@FirstName", objm_Parameters.FirstName)
        Else
            objl_CMD.CommandText = clsSqlQueries.Username()
            objl_CMD.Parameters.AddWithValue("@Source", Site)
            objl_CMD.Parameters.AddWithValue("@Username", objm_Parameters.Username)
        End If

        Dim objl_Users As New Dictionary(Of Long, _clsUser)
        With objl_CMD.ExecuteReader
            While .Read
                If .Item("LastName") <> "" Then
                    Dim objl_User As New _clsUser
                    objl_User.LastName = .Item("LastName")
                    objl_User.FirstName = .Item("FirstName")
                    objl_User.Username = .Item("Username")

                    objl_Users.Add(.Item("PlayerID"), objl_User)
                End If
            End While
            .Close()
        End With
        objl_CMD.Dispose()

        If objl_Users?.Count = 0 Then
            If objm_Parameters.GetUsername Then
                Throw New MissingMemberException($"Unable to determine {Site} username")
            Else
                Dim objl_User As New _clsUser With {.Username = objm_Parameters.Username}
                objl_Users.Add(0, objl_User)
            End If
        End If

        Return objl_Users
    End Function

    Friend Function GetPlayerNameForFile(pi_Parameters As _clsParameters) As String
        If MainWindow.ReplaceUsername AndAlso pi_Parameters.LastName <> "" Then
            Return pi_Parameters.LastName & nameDelimiter & pi_Parameters.FirstName
        Else
            Return pi_Parameters.Username
        End If
    End Function

    Friend Function GetTimeControlLimits(timeControlName As String, limit As String) As String
        'TODO: Add new OnlineMinSeconds and OnlineMaxSeconds to ChessWarehouse.dim.TimeControls, can't right now since server problem and source control
        Dim minSeconds As Long = 0
        Dim maxSeconds As Long = 0
        Select Case timeControlName
            Case "Bullet"
                minSeconds = 60
                maxSeconds = 179
            Case "Blitz"
                minSeconds = 180
                maxSeconds = 600
            Case "Rapid"
                minSeconds = 601
                maxSeconds = 1799
            Case "Classical"
                minSeconds = 1800
                maxSeconds = 86399
            Case "Correspondence"
                minSeconds = 86400
                maxSeconds = 1209600
        End Select

        Select Case limit
            Case "Min"
                Return minSeconds
            Case "Max"
                Return maxSeconds
            Case Else
                Return 0
        End Select
    End Function

    Friend Function FormatDateForPGN(dateValue As Date) As String
        Return dateValue.ToString("yyyy.MM.dd")
    End Function

    Friend Function SetBaseOutputName(pi_Parameters As _clsParameters) As String
        Dim baseName As String = GetPlayerNameForFile(pi_Parameters)
        baseName = baseName.Replace(nameDelimiter, "")

        baseName = $"{baseName}_{pi_Parameters.TimeControl}"

        If pi_Parameters.StartDate <> Date.MinValue Then
            baseName = $"{baseName}_{pi_Parameters.StartDate.ToString("yyyyMMdd")}"
        Else
            baseName = $"{baseName}_yyyyMMdd"  'TODO: This date should be the earliest game in the file
        End If

        If pi_Parameters.EndDate <> Date.MinValue Then
            baseName = $"{baseName}_{pi_Parameters.EndDate.ToString("yyyyMMdd")}"
        Else
            baseName = $"{baseName}_{Date.Today.ToString("yyyyMMdd")}"
        End If

        Return baseName
    End Function

    Public Class _clsParameters
        Public Property FirstName As String
        Public Property LastName As String
        Public Property Username As String
        Public Property Site As String
        Public Property TimeControl As String
        Public Property Color As String
        Public Property StartDate As Date
        Public Property EndDate As Date
        Public Property GetUsername As Boolean = True

        Friend Sub Clean()
            FirstName = FirstName.Trim()
            LastName = LastName.Trim()
            Username = Username.Trim
            If Username <> "" Then
                GetUsername = False
            End If
        End Sub
    End Class

    Public Class _clsUser
        Public Property LastName As String
        Public Property FirstName As String
        Public Property Username As String
    End Class
End Class
