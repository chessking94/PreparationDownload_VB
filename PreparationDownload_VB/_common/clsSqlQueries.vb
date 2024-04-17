Public Class clsSqlQueries
    Public Shared Function Username() As String
        Return _
            "
SELECT
PlayerID,
ISNULL(LastName, '') AS LastName,
ISNULL(FirstName, '') AS FirstName,
Username

FROM UsernameXRef

WHERE Source = @Source
AND Username = @Username
            "
    End Function

    Public Shared Function FirstLast() As String
        Return _
            "
SELECT
PlayerID,
LastName,
FirstName,
Username

FROM UsernameXRef

WHERE Source = @Source
AND LastName = @LastName
AND FirstName = @FirstName
            "
    End Function

    Public Shared Function InsertLog() As String
        Return _
            "
INSERT INTO DownloadLog (Player, Site, TimeControl, Color, StartDate, EndDate, OutPath)
VALUES (@Player, @Site, @TimeControl, @Color, @StartDate, @EndDate, @OutPath)
            "
    End Function

    Public Shared Function UpdateLog() As String
        Return _
            "
UPDATE DownloadLog
SET DownloadStatus = 'Complete', DownloadSeconds = @Seconds, DownloadGames = @Games
WHERE DownloadID = @ID
            "
    End Function

    Public Shared Function GetLastLog() As String
        Return "SELECT TOP(1) DownloadID FROM DownloadLog ORDER BY DownloadID DESC"
    End Function
End Class
