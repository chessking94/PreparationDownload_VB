Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Reflection

Public MustInherit Class clsBase
    Private WithEvents bgWorker As New ComponentModel.BackgroundWorker

    Public Shared projectDir As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
    Public Shared rootDir As String = Path.Combine(SpecialDirectories.Desktop, "Local_Applications", Assembly.GetCallingAssembly().GetName().Name)
    Public Shared nameDelimiter As String = "$$"
    Public Shared ReadOnly myConfig As New Utilities_NetCore.clsConfig

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
    End Sub
End Class
