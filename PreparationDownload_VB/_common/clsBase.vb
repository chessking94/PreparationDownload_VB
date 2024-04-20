Imports Microsoft.VisualBasic.FileIO
Imports System.IO
Imports System.Reflection

Public MustInherit Class clsBase
    Private WithEvents objm_Worker As New System.ComponentModel.BackgroundWorker

    Public Shared projectDir As String = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\.."))
    Public Shared rootDir As String = Path.Combine(SpecialDirectories.Desktop, "Local_Applications", Assembly.GetCallingAssembly().GetName().Name)
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
        Go_Child()
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
#End Region

#Region "Overrides"
    Protected MustOverride Sub Go_Child()

    Protected Overridable Function MustWaitForFinish() As Boolean
        Return True
    End Function
#End Region
End Class
