Imports Microsoft.Data.SqlClient
Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Reflection

Public MustInherit Class clsBase
    Private WithEvents bgWorker As New ComponentModel.BackgroundWorker

    Friend Shared myConfig As New Utilities_NetCore.clsConfig
    Friend Shared projectDir As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
    Friend Shared db_Connection As New SqlConnection

    Friend Shared rootDir As String = Path.Combine(SpecialDirectories.Desktop, "Local_Applications", Assembly.GetCallingAssembly().GetName().Name)
    Friend Shared nameDelimiter As String = "$$"

    Friend Sub StartThreaded()
        bgWorker.RunWorkerAsync()
    End Sub

    Friend Sub StartNonThreaded()
        Go()
    End Sub

    Private Sub Go() Handles bgWorker.DoWork
        Go_Child()
    End Sub

    Public Sub WaitForEnd(pi_LastCall As Boolean)
        If pi_LastCall OrElse MustWaitForFinish() Then
            While bgWorker.IsBusy
                Threading.Thread.Sleep(1000)
            End While
        End If
    End Sub

    Protected MustOverride Sub Go_Child()

    Protected Overridable Function MustWaitForFinish() As Boolean
        Return True
    End Function

    Friend Sub initializeConfig()
        myConfig.configFile = Path.Combine(projectDir, "appsettings.json")
        myConfig.buildConfig()

#If DEBUG Then
        Dim connectionString As String = myConfig.getConfig("connectionStringProd")
#Else
        Dim connectionString As String = myConfig.getConfig("connectionStringProd")
#End If

        db_Connection = Utilities_NetCore.Connection(connectionString)
    End Sub
End Class
