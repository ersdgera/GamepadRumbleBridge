# Тестирование моста вибрации - отправка команд через Named Pipe
param([string]$Command = "200 255", [int]$TimeoutSec = 10, [int]$Retries = 3)

$pipeName = "GamepadRumblePipe"

for ($i = 0; $i -lt $Retries; $i++) {
    try {
        Write-Host "Attempt $($i+1)/$Retries - Connecting to \\.\pipe\$pipeName ..."
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, 
            [System.IO.Pipes.PipeDirection]::InOut, 
            [System.IO.Pipes.PipeOptions]::None, 
            [System.Security.Principal.TokenImpersonationLevel]::Impersonation)
        
        $task = $pipe.ConnectAsync()
        $completed = $task.Wait($TimeoutSec * 1000)
        
        if (-not $completed) { 
            throw "Connection timeout after ${TimeoutSec}s" 
        }
        if ($task.Exception) { 
            throw $task.Exception.InnerException.ToString() 
        }
        
        Write-Host "Connected! Sending: $Command"
        $sw = New-Object System.IO.StreamWriter($pipe)
        $sw.AutoFlush = $true
        $sw.WriteLine($Command)
        $sw.Close()
        $pipe.Close()
        Write-Host "OK - command sent successfully."
        exit 0
    } catch {
        Write-Host "  Error: $_"
        if ($pipe -and -not $pipe.IsClosed) { try { $pipe.Close() } catch {} }
        if ($i -lt $Retries - 1) { Start-Sleep -Seconds 2 }
    }
}
Write-Host "FAILED after $Retries attempts"
exit 1
