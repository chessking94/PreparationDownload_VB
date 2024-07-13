Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        ' Check for command-line arguments
        If e.Args.Length > 0 Then
            ' Command-line arguments are present
            MessageBox.Show("Command-line arguments received: " & String.Join(", ", e.Args))
        Else
            ' No command-line arguments, open the main window normally
            Dim mainWindow As New MainWindow()
            'mainWindow.Show()
        End If
    End Sub
End Class
