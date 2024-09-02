Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text

Public Class clsProcessing : Inherits clsBase
    Public Property CDC As clsCDC
    Public Property Lichess As clsLichess

    Protected Overrides Sub Go_Child()
        Dim parameters As New _clsParameters
        With parameters
            .FirstName = MainWindow.FirstName
            .LastName = MainWindow.LastName
            .Username = MainWindow.Username
            .Site = MainWindow.Site
            .TimeControl = MainWindow.TimeControl
            .Color = MainWindow.Color
            .StartDate = MainWindow.StartDate
            .EndDate = MainWindow.EndDate
        End With
        parameters.Clean()

        If MainWindow.WriteLog Then
            WriteLogEntry(parameters)
        End If

        Dim stopwatch As New Stopwatch()
        stopwatch.Start()

        If Lichess IsNot Nothing Then
            'statusbar = "Downloading Lichess Games"
            BeforeDownload(Lichess.outputDir)
            Lichess.DownloadGames(parameters)
            AfterDownload(Lichess.cSite, Lichess.outputDir)
        End If

        If CDC IsNot Nothing Then
            'statusbar = "Downloading Chess.com Games"
            BeforeDownload(CDC.outputDir)
            CDC.DownloadGames(parameters)
            AfterDownload(CDC.cSite, CDC.outputDir)
        End If

        ProcessGames(parameters)
        stopwatch.Stop()
        parameters.ProcessSeconds = Math.Round(stopwatch.ElapsedMilliseconds / 1000)

        If MainWindow.WriteLog Then
            WriteLogEntry(parameters)
        End If
    End Sub

    Private Sub WriteLogEntry(ByRef pi_Parameters As _clsParameters)
        Dim player As String = If(pi_Parameters.Username = "", $"{pi_Parameters.LastName}, {pi_Parameters.FirstName}", pi_Parameters.Username)
        Dim site As String = If(pi_Parameters.Site = "All", "", pi_Parameters.Site)
        Dim timeControl As String = If(pi_Parameters.TimeControl = "All", "", pi_Parameters.TimeControl)
        Dim color As String = If(pi_Parameters.Color = "Both", "", pi_Parameters.Color)
        Dim startDate As String = If(pi_Parameters.StartDate = Date.MinValue, "", pi_Parameters.StartDate.ToString("yyyy-MM-dd"))
        Dim endDate As String = If(pi_Parameters.EndDate = Date.MinValue, "", pi_Parameters.EndDate.ToString("yyyy-MM-dd"))
        Dim outPath As String = rootDir

        Dim command As New Data.SqlClient.SqlCommand With {
            .Connection = Connection(strv_Application:=Assembly.GetCallingAssembly().GetName().Name),
            .CommandType = Data.CommandType.Text
        }

        If pi_Parameters.DownloadID = 0 Then
            With command
                .CommandText = clsSqlQueries.InsertLog()
                .Parameters.AddWithValue("@Player", player)
                .Parameters.AddWithValue("@Site", If(site = "", DBNull.Value, site))
                .Parameters.AddWithValue("@TimeControl", If(timeControl = "", DBNull.Value, timeControl))
                .Parameters.AddWithValue("@Color", If(color = "", DBNull.Value, color))
                .Parameters.AddWithValue("@StartDate", If(startDate = "", DBNull.Value, startDate))
                .Parameters.AddWithValue("@EndDate", If(endDate = "", DBNull.Value, endDate))
                .Parameters.AddWithValue("@OutPath", outPath)
            End With

        Else
            With command
                .CommandText = clsSqlQueries.UpdateLog()
                .Parameters.AddWithValue("@Seconds", pi_Parameters.ProcessSeconds)
                .Parameters.AddWithValue("@Games", pi_Parameters.GameCount)
                .Parameters.AddWithValue("@ID", pi_Parameters.DownloadID)
            End With
        End If

        command.ExecuteNonQuery()

        'TODO: rework this block to avoid a second query?
        If pi_Parameters.DownloadID = 0 Then
            command.Parameters.Clear()
            command.CommandText = clsSqlQueries.GetLastLog()
            With command.ExecuteReader
                While .Read
                    pi_Parameters.DownloadID = .Item("DownloadID")
                End While
                .Close()
            End With
        End If

        command.Dispose()
    End Sub

    Private Sub BeforeDownload(pi_outputDir As String)
        If Directory.Exists(pi_outputDir) Then
            Directory.Delete(pi_outputDir, True)
        End If

        Directory.CreateDirectory(pi_outputDir)
    End Sub

    Private Sub AfterDownload(pi_Site As String, pi_outputDir As String)
        'merge all files
        Dim mergeName As String = $"{pi_Site}_Merged_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"copy /B *.pgn {mergeName} >nul", pi_outputDir)

        'clean with pgn-extract - TODO: Figure out a way to package pgn-extract with this project, so it doesn't have to be called as an external dependency
        Dim cleanName As String = $"{pi_Site}_Cleaned_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"pgn-extract -N -V -D -pl2 --quiet --nosetuptags --output {cleanName} {mergeName} >nul", pi_outputDir)

        If pi_Site = "Chess.com" Then
            Dim allGames As New Dictionary(Of Long, List(Of String))
            Dim standardGames As New List(Of Long)
            Dim ctr As Long = 1

            Dim newGame As Boolean = True
            Dim tagsComplete As Boolean = False
            Dim IsVariant As Boolean = False
            Using reader As New StreamReader(Path.Combine(pi_outputDir, cleanName))
                Dim line As String = Nothing
                Dim allLines As New List(Of String)
                While Not reader.EndOfStream
                    line = reader.ReadLine()
                    If line = "" Then
                        If Not newGame Then
                            If tagsComplete Then
                                allGames.Add(ctr, New List(Of String)(allLines))  'since I am clearing the list on the next line, I need to use a new list here to persist the dictionary addition
                                allLines.Clear()

                                If Not IsVariant Then
                                    standardGames.Add(ctr)
                                End If

                                newGame = True
                                tagsComplete = False
                                IsVariant = False
                                ctr += 1
                            Else
                                allLines.Add(line)
                                tagsComplete = True
                            End If
                        End If
                    Else
                        newGame = False
                        allLines.Add(line)
                        If line.ToUpper().Contains("VARIANT") Then
                            IsVariant = True
                        End If
                    End If
                End While
            End Using

            Using writer As New StreamWriter(Path.Combine(pi_outputDir, cleanName), False, Encoding.UTF8)
                For Each game In standardGames
                    For Each line As String In allGames(game)
                        writer.WriteLine(line)
                    Next
                    writer.WriteLine(vbCrLf)  'This is needed for proper spacing of the PGN
                Next
            End Using
        End If

        'post-process clean-up
        File.Move(Path.Combine(pi_outputDir, cleanName), Path.Combine(rootDir, cleanName))
        Directory.Delete(pi_outputDir, True)
    End Sub

    Private Sub ProcessGames(ByRef pi_Parameters As _clsParameters)
        Dim playerName As String = GetPlayerNameForFile(pi_Parameters)

        'merge the site files into a single file
        'statusbar = '"Merging Game Files"
        Dim mergeName As String = $"{playerName}_Merged_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"copy /B *.pgn {mergeName} >nul", rootDir)

        'extract time control games
        Dim timeControlName As String = mergeName
        If pi_Parameters.TimeControl <> "All" Then
            'statusbar = '"Applying Time Control Parameters"
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
            'statusbar = '"Applying Start Date Parameter"
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
            'statusbar = '"Applying End Date Parameter"
            Dim endDatePGNFormat As String = FormatDateForPGN(pi_Parameters.EndDate)

            'create temporary tag file
            Dim endTagName As String = "EndDateTag.txt"
            Dim endTagFile As String = Path.Combine(rootDir, endTagName)
            Using writer As New StreamWriter(endTagFile)
                writer.WriteLine($"Date >= ""{endDatePGNFormat}""")
            End Using

            'filter end date
            endDateName = $"ED_{startDateName}"
            RunCommand($"pgn-extract --quiet -t{endTagName} --output {endDateName} {startDateName} >nul", rootDir)
        End If

        'sort games
        'statusbar = "Sorting Games"
        Dim sortName As String = SortGameFile(endDateName, pi_Parameters)

        Dim keepFiles As New List(Of String)
        If sortName <> "" Then
            'set final file names
            Dim baseName As String = SetBaseOutputName(pi_Parameters)
            Dim whiteName As String = $"{baseName}_White.pgn"
            Dim blackName As String = $"{baseName}_Black.pgn"
            Dim combinedName As String = $"{baseName}_Combined.pgn"

            'split into White/Black game files
            'statusbar = '"Splitting Into White/Black Files"
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

            Dim countFile As String = ""
            Dim whiteList As New List(Of String) From {"Both", "White"}
            If whiteList.Contains(pi_Parameters.Color) Then
                RunCommand($"pgn-extract --quiet -t{whiteTagName} --output {whiteName} {sortName} >nul", rootDir)
                countFile = Path.Combine(rootDir, whiteName)
                keepFiles.Add(countFile)
            End If

            Dim blackList As New List(Of String) From {"Both", "Black"}
            If blackList.Contains(pi_Parameters.Color) Then
                RunCommand($"pgn-extract --quiet -t{blackTagName} --output {blackName} {sortName} >nul", rootDir)
                countFile = Path.Combine(rootDir, blackName)
                keepFiles.Add(countFile)
            End If

            File.Move(Path.Combine(rootDir, sortName), Path.Combine(rootDir, combinedName))
            If pi_Parameters.Color = "Both" Then
                countFile = Path.Combine(rootDir, combinedName)
                keepFiles.Add(countFile)
            End If

            'statusbar = '"Counting Games"
            pi_Parameters.GameCount = CountGames(countFile)
        End If

        If pi_Parameters.GameCount = 0 Then
            MainWindow.ErrorList = AppendText(MainWindow.ErrorList, $"No games found for these parameters")
        End If

        'clean up directory
        Dim filesInRoot As String() = Directory.GetFiles(rootDir)
        For Each f As String In filesInRoot
            If Not keepFiles.Contains(f) Then
                File.Delete(f)
            End If
        Next
    End Sub

    Private Function SortGameFile(pi_fileName As String, ByRef pi_Parameters As _clsParameters) As String
        Dim ctr As Long = 1

        'in the long run, I should use a class instead of multiple dictionaries. something for Chess_NetCore
        Dim allDates As New Dictionary(Of Long, Date)
        Dim allGames As New Dictionary(Of Long, List(Of String))

        Dim newGame As Boolean = True
        Dim tagsComplete As Boolean = False
        Using reader As New StreamReader(Path.Combine(rootDir, pi_fileName))
            Dim line As String = Nothing
            Dim allLines As New List(Of String)
            While Not reader.EndOfStream
                line = reader.ReadLine()
                If line = "" Then
                    If Not newGame Then
                        If tagsComplete Then
                            allGames.Add(ctr, New List(Of String)(allLines))  'since I am clearing the list on the next line, I need to use a new list here to persist the dictionary addition
                            allLines.Clear()

                            newGame = True
                            tagsComplete = False
                            ctr += 1
                        Else
                            allLines.Add(line)
                            tagsComplete = True
                        End If
                    End If
                Else
                    newGame = False
                    allLines.Add(line)
                    If line.Contains("[Date """) Then
                        Dim dateString As String = line.Substring(7, 10)
                        Dim gameDate As Date = Date.ParseExact(dateString, "yyyy.MM.dd", CultureInfo.InvariantCulture)
                        allDates.Add(ctr, gameDate)
                    End If
                End If
            End While
        End Using

        Dim newFileName As String = ""
        If allDates.Count > 0 Then
            pi_Parameters.FirstGameDate = allDates.Values.Min()

            Dim objl_DatesSorted = allDates.OrderBy(Function(pair) pair.Value)
            newFileName = $"Sorted_{pi_fileName}"

            Using writer As New StreamWriter(Path.Combine(rootDir, newFileName), False, Encoding.UTF8)
                For Each game In objl_DatesSorted
                    For Each line As String In allGames(game.Key)
                        writer.WriteLine(line)
                    Next
                    writer.WriteLine(vbCrLf)  'This is needed for proper spacing of the PGN
                Next
            End Using
        End If

        Return newFileName
    End Function

    Friend Function CreateUserList(pi_Site As String, ByRef pi_Parameters As _clsParameters)
        Dim command As New Data.SqlClient.SqlCommand With {.Connection = Connection(strv_Application:=Assembly.GetCallingAssembly().GetName().Name)}

        If pi_Parameters.GetUsername Then
            command.CommandText = clsSqlQueries.FirstLast()
            command.Parameters.AddWithValue("@Source", pi_Site)
            command.Parameters.AddWithValue("@LastName", pi_Parameters.LastName)
            command.Parameters.AddWithValue("@FirstName", pi_Parameters.FirstName)
        Else
            command.CommandText = clsSqlQueries.Username()
            command.Parameters.AddWithValue("@Source", pi_Site)
            command.Parameters.AddWithValue("@Username", pi_Parameters.Username)
        End If

        Dim users As New Dictionary(Of Long, _clsUser)
        With command.ExecuteReader
            While .Read
                If .Item("LastName") <> "" Then
                    Dim user As New _clsUser
                    user.LastName = .Item("LastName")
                    user.FirstName = .Item("FirstName")
                    user.Username = .Item("Username")

                    users.Add(.Item("PlayerID"), user)

                    If pi_Parameters.LastName = "" Then
                        pi_Parameters.LastName = user.LastName
                        pi_Parameters.FirstName = user.FirstName
                    End If
                End If
            End While
            .Close()
        End With
        command.Dispose()

        If users?.Count = 0 Then
            If pi_Parameters.GetUsername Then
                Throw New MissingMemberException($"Unable to determine {pi_Site} username")
            Else
                Dim user As New _clsUser With {.Username = pi_Parameters.Username}
                users.Add(0, user)
            End If
        End If

        Return users
    End Function

    Private Function GetPlayerNameForFile(pi_Parameters As _clsParameters) As String
        If MainWindow.ReplaceUsername AndAlso pi_Parameters.LastName <> "" Then
            Return pi_Parameters.LastName & nameDelimiter & pi_Parameters.FirstName
        Else
            Return pi_Parameters.Username
        End If
    End Function

    Private Function GetTimeControlLimits(pi_timeControlName As String, pi_limit As String) As String
        'TODO: Add new OnlineMinSeconds and OnlineMaxSeconds to ChessWarehouse.dim.TimeControls, can't right now since server problem and source control
        Dim minSeconds As Long = 0
        Dim maxSeconds As Long = 0
        Select Case pi_timeControlName
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

        Select Case pi_limit
            Case "Min"
                Return minSeconds
            Case "Max"
                Return maxSeconds
            Case Else
                Return 0
        End Select
    End Function

    Private Function FormatDateForPGN(pi_dateValue As Date) As String
        Return pi_dateValue.ToString("yyyy.MM.dd")
    End Function

    Private Function SetBaseOutputName(pi_Parameters As _clsParameters) As String
        Dim baseName As String = GetPlayerNameForFile(pi_Parameters)
        baseName = baseName.Replace(nameDelimiter, "")

        baseName = $"{baseName}_{pi_Parameters.TimeControl}"

        If pi_Parameters.StartDate <> Date.MinValue Then
            baseName = $"{baseName}_{pi_Parameters.StartDate.ToString("yyyyMMdd")}"
        Else
            baseName = $"{baseName}_{pi_Parameters.FirstGameDate.ToString("yyyyMMdd")}"
        End If

        If pi_Parameters.EndDate <> Date.MinValue Then
            baseName = $"{baseName}_{pi_Parameters.EndDate.ToString("yyyyMMdd")}"
        Else
            baseName = $"{baseName}_{Date.Today.ToString("yyyyMMdd")}"
        End If

        Return baseName
    End Function

    Private Function CountGames(pi_fileName As String) As Long
        Dim gameCount As Long = 0
        Using reader As New StreamReader(pi_fileName)
            Dim line As String = Nothing
            While Not reader.EndOfStream
                line = reader.ReadLine()
                If line.Contains("[Event """) Then
                    gameCount += 1
                End If
            End While
        End Using

        Return gameCount
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
        Public Property DownloadID As Long = 0
        Public Property GameCount As Long = 0
        Public Property ProcessSeconds As Double = 0
        Public Property FirstGameDate As Date

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
