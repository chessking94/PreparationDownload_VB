Imports System.IO
Imports System.Net.Http

Public Class clsLichess : Inherits clsProcessing
    Friend ReadOnly outputDir As String = Path.Combine(rootDir, Me.GetType().Name)
    Friend ReadOnly cSite As String = "Lichess"
    Const cURLBase As String = "https://lichess.org/api/games/user/"
    Const cURLParam As String = "?perfType=bullet,blitz,rapid,classical,correspondence&clocks=true&evals=true&sort=dateAsc"

    Friend Sub DownloadGames(objm_Parameters As _clsParameters)
        Dim objl_Users As Dictionary(Of Long, _clsUser) = CreateUserList(cSite, objm_Parameters)
        Dim apiKey As String = objg_Config.getConfig("Lichess_APIToken")
        Dim objl_files As New List(Of String)

        For Each u In objl_Users.Values
            Dim URL As String = cURLBase & u.Username & cURLParam
            Dim fileName As String = $"{u.Username}_{Date.Now.ToString("yyyyMMddHHmmss")}.pgn"  'Username_yyyymmddHHMMSS.pgn
            Dim filePath As String = Path.Combine(outputDir, fileName)

            Using client As New HttpClient()
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}")

                Dim response = client.GetAsync(URL).Result
                If response.IsSuccessStatusCode Then
                    Dim contentBytes = response.Content.ReadAsByteArrayAsync().Result
                    File.WriteAllBytes(filePath, contentBytes)
                    objl_files.Add(filePath)
                Else
                    MainWindow.ErrorList = AppendText(MainWindow.ErrorList, $"Lichess game download API error: {response.StatusCode}")
                End If
            End Using

            'Lichess games do not have a populated TimeControl tag for Correspondence games, add a default
            ReplaceTextInFile(filePath, "[TimeControl ""-""]", "[TimeControl ""1/86400""]")

            If MainWindow.ReplaceUsername AndAlso u.LastName <> "" Then
                ReplaceTextInFile(filePath, $"""{u.Username}""", $"""{u.LastName}, {u.FirstName}""")  'replace "Username" with "Last, First"
            End If
        Next
    End Sub
End Class
