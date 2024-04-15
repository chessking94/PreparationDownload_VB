Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Reflection

Public MustInherit Class clsBase
    Private WithEvents objm_Worker As New System.ComponentModel.BackgroundWorker

    Public Shared projectDir As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
    Public Shared rootDir As String = SpecialDirectories.Desktop & Path.DirectorySeparatorChar & Assembly.GetCallingAssembly().GetName().Name
    Public Shared nameDelimiter As String = "$$"

#Region "Friends"
    Friend Sub StartThreaded()
        objm_Worker.RunWorkerAsync()
    End Sub

    Friend Sub StartNonThreaded()
        Go()
    End Sub
#End Region

#Region "Private"
    Private Sub Go() Handles objm_Worker.DoWork
        Dim StartDate As Date = Date.Now

        Try
            Go_Child()
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
    End Sub
#End Region

#Region "Public"
    Public Sub WaitForEnd(boov_LastCall As Boolean)
        If boov_LastCall OrElse MustWaitForFinish() Then
            While objm_Worker.IsBusy
                System.Threading.Thread.Sleep(1000)
            End While
        End If
    End Sub

    Public Shared Function ConnectionString(Optional strv_Database As String = "ChessWarehouse", Optional strv_Application As String = "") As String
        Return _
            "Server=localhost" &
            ";Database=" & strv_Database &
            ";Integrated Security=SSPI" &
            ";Application Name=" & strv_Application &
            ";MultipleActiveResultSets=True"
    End Function

    Public Shared Function Connection(Optional strv_Database As String = "ChessWarehouse", Optional strv_Application As String = "") As System.Data.SqlClient.SqlConnection
        Dim objl_Connection As New System.Data.SqlClient.SqlConnection(ConnectionString(strv_Database, strv_Application))
        objl_Connection.Open()
        Return objl_Connection
    End Function
#End Region

#Region "Overrides"
    Protected MustOverride Sub Go_Child()

    Protected Overridable Function MustWaitForFinish() As Boolean
        Return True
    End Function
#End Region
End Class
