Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text

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
            WriteLogEntry(objl_Parameters)
        End If

        Dim stopwatch As New Stopwatch()
        stopwatch.Start()

        If Lichess IsNot Nothing Then
            'statusbar = "Downloading Lichess Games"
            BeforeDownload(Lichess.outputDir)
            Lichess.DownloadGames(objl_Parameters)
            AfterDownload(Lichess.cSite, Lichess.outputDir)
        End If

        If CDC IsNot Nothing Then
            'statusbar = "Downloading Chess.com Games"
            BeforeDownload(CDC.outputDir)
            CDC.DownloadGames(objl_Parameters)
            AfterDownload(CDC.cSite, CDC.outputDir)
        End If

        ProcessGames(objl_Parameters)

        stopwatch.Stop()
        objl_Parameters.ProcessSeconds = Math.Round(stopwatch.ElapsedMilliseconds / 1000)

        If MainWindow.WriteLog Then
            WriteLogEntry(objl_Parameters)
        End If
    End Sub

    Private Sub WriteLogEntry(ByRef pi_Parameters As _clsParameters)
        Dim player As String = If(pi_Parameters.Username = "", $"{pi_Parameters.LastName}, {pi_Parameters.FirstName}", pi_Parameters.Username)
        Dim site As String = If(pi_Parameters.Site = "All", DBNull.Value, pi_Parameters.Site)
        Dim timeControl As String = If(pi_Parameters.TimeControl = "All", DBNull.Value, pi_Parameters.TimeControl)
        Dim color As String = If(pi_Parameters.Color = "Both", DBNull.Value, pi_Parameters.Color)
        Dim startDate As String = If(pi_Parameters.StartDate = Date.MinValue, DBNull.Value, pi_Parameters.StartDate.ToString("yyyy-MM-dd"))
        Dim endDate As String = If(pi_Parameters.EndDate = Date.MinValue, DBNull.Value, pi_Parameters.EndDate.ToString("yyyy-MM-dd"))
        Dim outPath As String = rootDir

        Dim objl_CMD As New Data.SqlClient.SqlCommand With {
            .Connection = Connection(strv_Application:=Assembly.GetCallingAssembly().GetName().Name),
            .CommandType = Data.CommandType.Text
        }

        If pi_Parameters.DownloadID = 0 Then
            With objl_CMD
                .CommandText = clsSqlQueries.InsertLog()
                .Parameters.AddWithValue("@Player", player)
                .Parameters.AddWithValue("@Site", site)
                .Parameters.AddWithValue("@TimeControl", timeControl)
                .Parameters.AddWithValue("@Color", color)
                .Parameters.AddWithValue("@StartDate", startDate)
                .Parameters.AddWithValue("@EndDate", endDate)
                .Parameters.AddWithValue("@OutPath", outPath)
            End With

        Else
            With objl_CMD
                .CommandText = clsSqlQueries.UpdateLog()
                .Parameters.AddWithValue("@Seconds", pi_Parameters.ProcessSeconds)
                .Parameters.AddWithValue("@Games", site)
                .Parameters.AddWithValue("@ID", pi_Parameters.DownloadID)
            End With
        End If

        objl_CMD.ExecuteNonQuery()

        If pi_Parameters.DownloadID = 0 Then
            objl_CMD.Parameters.Clear()
            objl_CMD.CommandText = clsSqlQueries.GetLastLog()
            With objl_CMD.ExecuteReader
                While .Read
                    pi_Parameters.DownloadID = .Item("DownloadID")
                End While
                .Close()
            End With
        End If

        objl_CMD.Dispose()
    End Sub

    Private Sub BeforeDownload(outputDir As String)
        If Directory.Exists(outputDir) Then
            Directory.Delete(outputDir, True)
        End If

        Directory.CreateDirectory(outputDir)
    End Sub

    Private Sub AfterDownload(Site As String, outputDir As String)
        'merge all files
        Dim mergeName As String = $"{Site}_Merged_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"copy /B *.pgn {mergeName} >nul", outputDir)

        'clean with pgn-extract - TODO: Figure out a way to package pgn-extract with this project, so it doesn't have to be called as an external dependency
        Dim cleanName As String = $"{Site}_Cleaned_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"
        RunCommand($"pgn-extract -N -V -D -pl2 --quiet --nosetuptags --output {cleanName} {mergeName} >nul", outputDir)

        If Site = "Chess.com" Then
            'TODO: Need to remove non-standard games
            'might be a fun exercise to write my own pgn parser, the main .NET parser I found (pgn.net) is almost 10 years old
        End If

        'post-process clean-up
        File.Move(Path.Combine(outputDir, cleanName), Path.Combine(rootDir, cleanName))
        Directory.Delete(outputDir, True)
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

        'sort games - TODO: how to do? Maybe there's a PGN parsing library somewhere (or pgn-extract can do it)
        'statusbar = "Sorting Games"
        Dim sortName As String = endDateName
        'SortGameFile(Path.Combine(rootDir, sortName))

        'set final file names
        Dim baseName As String = SetBaseOutputName(pi_Parameters)
        Dim whiteName As String = $"{baseName}_White.pgn"
        Dim blackName As String = $"{baseName}_Black.pgn"
        Dim combinedName As String = $"{baseName}_Combined.pgn"
        Dim keepFiles As New List(Of String)

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

        'clean up directory
        Dim filesInRoot As String() = Directory.GetFiles(rootDir)
        For Each f As String In filesInRoot
            If Not keepFiles.Contains(f) Then
                File.Delete(f)
            End If
        Next

        'statusbar = '"Counting Games"
        pi_Parameters.GameCount = CountGames(countFile)
    End Sub

    Private Sub SortGameFile(fileName As String)
        'TODO: May need to make this a function and return a _Sorted filename
        Dim ctr As Long = 1
        Dim objl_Lines As New List(Of String)

        'in the long run, I should use a class instead of multiple dictionaries. something for Chess_NetCore
        Dim objl_Dates As New Dictionary(Of Long, Date)
        Dim objl_Games As New Dictionary(Of Long, List(Of String))

        Dim newGame As Boolean = True
        Dim tagsComplete As Boolean = False
        Using reader As New StreamReader(fileName)
            Dim line As String = Nothing
            While Not reader.EndOfStream
                line = reader.ReadLine()
                If line = "" Then
                    If Not newGame Then
                        If tagsComplete Then
                            objl_Games.Add(ctr, objl_Lines)
                            objl_Lines.Clear()

                            newGame = True
                            tagsComplete = False
                            ctr += 1
                        Else
                            objl_Lines.Add(line)
                            tagsComplete = True
                        End If
                    End If
                Else
                    newGame = False
                    objl_Lines.Add(line)
                    If line.Contains("[Date """) Then
                        Dim dateString As String = line.Substring(7, 10)
                        Dim gameDate As Date = Date.ParseExact(dateString, "yyyy.MM.dd", CultureInfo.InvariantCulture)
                        objl_Dates.Add(ctr, gameDate)
                    End If
                End If
            End While
        End Using

        Dim objl_DatesSorted = objl_Dates.OrderBy(Function(pair) pair.Value)

        For Each game In objl_DatesSorted
            Using writer As New StreamWriter(fileName, False, Encoding.UTF8)
                For Each line As String In objl_Games(game.Key)  'TODO: this doesn't seem to be returning the value in objl_Games, so files write as empty
                    writer.WriteLine(line)
                Next
                writer.WriteLine(vbCrLf)
            End Using
        Next
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

    Private Function GetPlayerNameForFile(pi_Parameters As _clsParameters) As String
        If MainWindow.ReplaceUsername AndAlso pi_Parameters.LastName <> "" Then
            Return pi_Parameters.LastName & nameDelimiter & pi_Parameters.FirstName
        Else
            Return pi_Parameters.Username
        End If
    End Function

    Private Function GetTimeControlLimits(timeControlName As String, limit As String) As String
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

    Private Function FormatDateForPGN(dateValue As Date) As String
        Return dateValue.ToString("yyyy.MM.dd")
    End Function

    Private Function SetBaseOutputName(pi_Parameters As _clsParameters) As String
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

    Private Function CountGames(fileName As String) As Long
        Dim gameCount As Long = 0
        Using reader As New StreamReader(fileName)
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
