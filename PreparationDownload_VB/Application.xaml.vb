Class Application
    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException can be handled in this file.

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim mainWindow As New MainWindow()
        mainWindow.Show()

        'If e.Args.Length > 0 Then
        '    'arguments, run without interactivity
        '    MainWindow.UseArguments(e.Args)

        '    'TODO: need to terminate the program at the end, doesn't seem to naturally
        '    Environment.Exit(0)
        'Else
        '    'no arguments, open the interactive main window
        '    MainWindow.Show()
        'End If
    End Sub
End Class
