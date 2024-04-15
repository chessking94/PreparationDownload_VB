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
End Class
