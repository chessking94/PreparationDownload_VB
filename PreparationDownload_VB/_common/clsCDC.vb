Imports System.IO
Imports System.Net.Http
Imports Newtonsoft.Json.Linq

Public Class clsCDC : Inherits clsProcessing
    Friend ReadOnly outputDir As String = Path.Combine(rootDir, Me.GetType().Name)
    Friend ReadOnly cSite As String = "Chess.com"
    Const cURLBase As String = "https://api.chess.com/pub/player/"
    Const cURLPath As String = "/games/archives"

    Friend Sub DownloadGames(objm_Parameters As _clsParameters)
        Dim objl_Users As Dictionary(Of Long, _clsUser) = CreateUserList(cSite, objm_Parameters)
        Dim userAgent As String = clsConfig.getConfig("Chess.com_UserAgent")
        Dim objl_files As New List(Of String)

        For Each u In objl_Users.Values
            Dim URL As String = cURLBase & u.Username & cURLPath
            Dim archiveArray As JArray = Nothing

            Using client As New HttpClient()
                client.DefaultRequestHeaders.Add("User-Agent", userAgent)

                Dim response = client.GetAsync(URL).Result
                If response.IsSuccessStatusCode Then
                    Dim responseContent As String = response.Content.ReadAsStringAsync().Result
                    Dim jsonData As JObject = JObject.Parse(responseContent)
                    archiveArray = DirectCast(jsonData("archives"), JArray)
                Else
                    'TODO: Adjust this from popping a message box to adding to an error list, then pop a single error list at the end of all processing
                    MessageBox.Show("Failed to obtain archive list. Status code: " & response.StatusCode, "Chess.com API error", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End Using

            If archiveArray IsNot Nothing Then
                For Each archiveURL As JToken In archiveArray
                    Dim aURL As String = $"{archiveURL}/pgn"
                    Dim yearMonth As String = archiveURL.ToString().Substring(Math.Max(0, archiveURL.ToString().Length - 7))
                    yearMonth = yearMonth.Replace("/", "")

                    Dim fileName As String = $"{u.Username}_{yearMonth}.pgn"
                    Dim filePath As String = Path.Combine(outputDir, fileName)

                    Using client As New HttpClient()
                        client.DefaultRequestHeaders.Add("User-Agent", userAgent)

                        Dim response = client.GetAsync(aURL).Result
                        If response.IsSuccessStatusCode Then
                            Dim contentBytes = response.Content.ReadAsByteArrayAsync().Result
                            File.WriteAllBytes(filePath, contentBytes)
                            objl_files.Add(filePath)
                        End If
                    End Using

                    If u.LastName <> "" Then  'TODO: Add a checkbox parameter in MainWindow to skip this if desired
                        ReplaceTextInFile(filePath, $"""{u.Username}""", $"""{u.LastName}, {u.FirstName}""")  'replace "Username" with "Last, First"
                    End If
                Next
            End If
        Next
    End Sub
End Class
