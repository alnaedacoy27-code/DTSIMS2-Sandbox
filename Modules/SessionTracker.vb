Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

''' <summary>
''' Records who is logged in, on which PC, since when, and which screen they are in (user_sessions).
''' A person counts as "active" while their heartbeat is fresh (last 30 seconds).
''' </summary>
Public Module SessionTracker

    Private _sessionId As String = ""

    Public Sub StartSession()
        _sessionId = Guid.NewGuid().ToString("N")
        Try
            Db.NonQuery(
                "INSERT INTO user_sessions (session_id, user_id, username, device_name, ip_address, current_module, module_since) " &
                "VALUES (@s, @u, @n, @d, @ip, 'Home', NOW())",
                Db.Params("@s", _sessionId, "@u", AppSession.UserId, "@n", AppSession.Username,
                          "@d", Environment.MachineName, "@ip", LocalIp()))
        Catch
            _sessionId = ""
        End Try
    End Sub

    ''' <summary>Called every few seconds from the live engine (background thread).</summary>
    Public Sub Heartbeat()
        Dim sid As String = _sessionId
        If sid.Length = 0 Then Return
        Try
            Db.NonQuery("UPDATE user_sessions SET last_seen = NOW(), is_active = 1 WHERE session_id = @s", Db.Params("@s", sid))
        Catch
            ' ignore
        End Try
    End Sub

    ''' <summary>"This person just opened the Sales Report" (does not slow the screen down).</summary>
    Public Sub SetModuleAsync(moduleName As String)
        Dim sid As String = _sessionId
        If sid.Length = 0 Then Return

        ' capture who this is now: the work below runs on another thread
        Dim uid As Integer = AppSession.UserId
        Dim uname As String = AppSession.Username
        Dim logIt As Boolean = (moduleName <> "Home")

        ThreadPool.QueueUserWorkItem(
            Sub(state As Object)
                Try
                    Db.NonQuery(
                        "UPDATE user_sessions SET current_module = @m, module_since = NOW(), last_seen = NOW() WHERE session_id = @s",
                        Db.Params("@m", moduleName, "@s", sid))
                    If logIt Then
                        Db.NonQuery(
                            "INSERT INTO audit_trail (user_id, username, action, module, description, device_name) " &
                            "VALUES (@uid, @un, 'OPEN', @m, @d, @dev)",
                            Db.Params("@uid", uid, "@un", uname, "@m", moduleName,
                                      "@d", "Opened " & moduleName, "@dev", Environment.MachineName))
                    End If
                Catch
                    ' ignore
                End Try
            End Sub)
    End Sub

    Public Sub EndSession()
        Dim sid As String = _sessionId
        _sessionId = ""
        If sid.Length = 0 Then Return
        Try
            Db.NonQuery("UPDATE user_sessions SET is_active = 0, logout_at = NOW() WHERE session_id = @s", Db.Params("@s", sid))
            AuditLog.Write("LOGOUT", "Login", "'" & AppSession.Username & "' logged out")
        Catch
            ' ignore
        End Try
    End Sub

    ''' <summary>This PC's IPv4 address, read from the network adapters (no DNS lookup, which can stall for seconds).</summary>
    Private Function LocalIp() As String
        Try
            Dim fallback As String = Nothing
            For Each ni As System.Net.NetworkInformation.NetworkInterface In System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                If ni.OperationalStatus <> System.Net.NetworkInformation.OperationalStatus.Up Then Continue For
                If ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Loopback OrElse
                   ni.NetworkInterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Tunnel Then Continue For

                Dim props As System.Net.NetworkInformation.IPInterfaceProperties = ni.GetIPProperties()
                For Each ua As System.Net.NetworkInformation.UnicastIPAddressInformation In props.UnicastAddresses
                    If ua.Address.AddressFamily <> AddressFamily.InterNetwork Then Continue For
                    If props.GatewayAddresses.Count > 0 Then Return ua.Address.ToString()   ' the real LAN / Wi-Fi adapter
                    If fallback Is Nothing Then fallback = ua.Address.ToString()
                Next
            Next
            Return fallback
        Catch
            ' ignore
        End Try
        Return Nothing
    End Function

End Module
