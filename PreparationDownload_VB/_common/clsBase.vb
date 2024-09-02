Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Reflection

Public MustInherit Class clsBase
    Private WithEvents objm_Worker As New ComponentModel.BackgroundWorker

    Public Shared projectDir As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
    Public Shared rootDir As String = Path.Combine(SpecialDirectories.Desktop, "Local_Applications", Assembly.GetCallingAssembly().GetName().Name)
    Public Shared nameDelimiter As String = "$$"
    Public Shared ReadOnly objg_Config As New Utilities_NetCore.clsConfig

    Friend Sub StartThreaded()
        objm_Worker.RunWorkerAsync()
    End Sub

    Friend Sub StartNonThreaded()
        Go()
    End Sub

    Private Sub Go() Handles objm_Worker.DoWork
        Go_Child()
    End Sub

    Public Sub WaitForEnd(boov_LastCall As Boolean)
        If boov_LastCall OrElse MustWaitForFinish() Then
            While objm_Worker.IsBusy
                Threading.Thread.Sleep(1000)
            End While
        End If
    End Sub

    Protected MustOverride Sub Go_Child()

    Protected Overridable Function MustWaitForFinish() As Boolean
        Return True
    End Function

    Friend Sub initializeConfig()
        objg_Config.configFile = Path.Combine(projectDir, "appsettings.json")
        objg_Config.buildConfig()
    End Sub
End Class
