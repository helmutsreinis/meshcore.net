# PowerShell Integration Examples for Meshcore.NET

This document provides PowerShell script samples demonstrating how to incorporate the Meshcore.NET library into PowerShell scripts for various use cases.

## Table of Contents
- [Setup and Prerequisites](#setup-and-prerequisites)
- [Basic Connection Examples](#basic-connection-examples)
- [Device Management Scripts](#device-management-scripts)
- [Contact Management Scripts](#contact-management-scripts)
- [Messaging Scripts](#messaging-scripts)
- [Monitoring and Automation Scripts](#monitoring-and-automation-scripts)
- [Advanced Use Cases](#advanced-use-cases)

---

## Setup and Prerequisites

### Installing the Library

```powershell
# Method 1: Install from NuGet (when available)
Install-Package Meshcore.NET

# Method 2: Reference the compiled DLL
# Build the project first, then add reference in your script
Add-Type -Path "C:\path\to\meshcore-lib.dll"
```

### Loading the Library in PowerShell

```powershell
# Load the assembly
Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Import required namespaces
using namespace meshcore_lib
using namespace meshcore_lib.connection
using namespace meshcore_lib.connection.payloads
using namespace meshcore_lib.utils
```

---

## Basic Connection Examples

### Example 1: Simple BLE Connection Setup

```powershell
# SimpleBleConnection.ps1
# Demonstrates basic BLE connection to a Meshcore device

param(
    [Parameter(Mandatory=$true)]
    [string]$DeviceAddress
)

# Load the library
Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Define custom connection class inline using C#
$connectionCode = @"
using System;
using meshcore_lib.connection;

public class PsBleConnection : Connection
{
    private System.IO.Ports.SerialPort _port;
    
    public PsBleConnection(string portName)
    {
        _port = new System.IO.Ports.SerialPort(portName, 115200);
        _port.DataReceived += OnDataReceived;
        _port.Open();
    }
    
    protected override void Close()
    {
        if (_port != null && _port.IsOpen)
            _port.Close();
    }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        _port.Write(data, 0, data.Length);
    }
    
    private void OnDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
    {
        var buffer = new byte[_port.BytesToRead];
        _port.Read(buffer, 0, buffer.Length);
        onFrameReceived(buffer);
    }
}
"@

Add-Type -TypeDefinition $connectionCode -ReferencedAssemblies @(
    "System.dll",
    ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"
)

# Create connection instance
$connection = New-Object PsBleConnection -ArgumentList "COM3"

# Subscribe to connection event
$connectedHandler = {
    Write-Host "Connected to Meshcore device!" -ForegroundColor Green
}
$connection.add_Connected($connectedHandler)

# Trigger connection
$connection.onConnected()

# Get device info
$deviceInfo = $connection.getSelfInfo()
Write-Host "Device Name: $($deviceInfo.AdvName)"
Write-Host "Public Key: $([BitConverter]::ToString($deviceInfo.PublicKey))"

# Keep script running
Write-Host "Press any key to disconnect..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Cleanup
$connection.Close()
```

---

## Device Management Scripts

### Example 2: Configure Device Settings

```powershell
# ConfigureDevice.ps1
# Script to configure a Meshcore device with specific settings

param(
    [string]$DeviceName = "MyMeshNode",
    [double]$Latitude = 37.7749,
    [double]$Longitude = -122.4194,
    [int]$TxPower = 20,
    [string]$ComPort = "COM3"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Create connection (using previously defined PsBleConnection)
# ... connection setup code ...

Write-Host "Configuring device..." -ForegroundColor Cyan

try {
    # Set device name
    Write-Host "Setting device name to: $DeviceName"
    $connection.setAdvertName($DeviceName)
    Start-Sleep -Milliseconds 500
    
    # Set location (convert to microdegrees)
    $latMicro = [int]($Latitude * 1000000)
    $lonMicro = [int]($Longitude * 1000000)
    Write-Host "Setting location: $Latitude, $Longitude"
    $connection.setAdvertLatLong($latMicro, $lonMicro)
    Start-Sleep -Milliseconds 500
    
    # Set TX power
    Write-Host "Setting TX power to: $TxPower"
    $connection.setTxPower([byte]$TxPower)
    Start-Sleep -Milliseconds 500
    
    # Configure for long range
    Write-Host "Configuring radio for long range..."
    $connection.setRadioParams(
        915000000,  # 915 MHz
        125000,     # 125 kHz bandwidth
        12,         # SF12 - maximum range
        8           # 4/8 coding rate
    )
    
    Write-Host "Configuration complete!" -ForegroundColor Green
    
    # Verify settings
    $info = $connection.getSelfInfo()
    Write-Host "`nCurrent Settings:"
    Write-Host "  Name: $($info.AdvName)"
    Write-Host "  Frequency: $($info.RadioFreq) Hz"
    Write-Host "  Bandwidth: $($info.RadioBw) Hz"
    Write-Host "  Spreading Factor: $($info.RadioSf)"
    
} catch {
    Write-Host "Error configuring device: $_" -ForegroundColor Red
}

$connection.Close()
```

### Example 3: Device Health Monitor

```powershell
# DeviceHealthMonitor.ps1
# Monitors device battery, signal strength, and connectivity

param(
    [string]$ComPort = "COM3",
    [int]$IntervalSeconds = 60
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Starting device health monitor..." -ForegroundColor Cyan
Write-Host "Checking every $IntervalSeconds seconds. Press Ctrl+C to stop.`n"

# Battery event handler
$batteryHandler = {
    param($sender, $milliVolts)
    $volts = $milliVolts / 1000.0
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    $color = if ($volts -gt 3.7) { "Green" } 
             elseif ($volts -gt 3.3) { "Yellow" } 
             else { "Red" }
    
    Write-Host "[$timestamp] Battery: " -NoNewline
    Write-Host ("{0:F2}V" -f $volts) -ForegroundColor $color
}
$connection.add_BatteryVoltageResponse($batteryHandler)

# Main monitoring loop
try {
    while ($true) {
        # Request battery voltage
        $connection.sendCommandGetBatteryVoltage()
        
        # Get device time
        $deviceTime = $connection.getDeviceTime()
        if ($deviceTime) {
            $dt = [DateTimeOffset]::FromUnixTimeSeconds($deviceTime).LocalDateTime
            Write-Host "Device Time: $dt"
        }
        
        # Check for waiting messages
        $connection.sendCommandSyncNextMessage()
        
        Start-Sleep -Seconds $IntervalSeconds
    }
} finally {
    $connection.Close()
}
```

---

## Contact Management Scripts

### Example 4: Export All Contacts

```powershell
# ExportContacts.ps1
# Exports all contacts to a CSV file

param(
    [string]$ComPort = "COM3",
    [string]$OutputFile = "contacts.csv"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Retrieving contacts..." -ForegroundColor Cyan

# Get all contacts
$contacts = $connection.getContacts()

Write-Host "Found $($contacts.Count) contacts"

# Convert to PowerShell objects
$contactList = @()
foreach ($contact in $contacts) {
    $contactList += [PSCustomObject]@{
        Name = $contact.AdvName
        PublicKey = [BitConverter]::ToString($contact.PublicKey)
        Type = $contact.Type
        Latitude = $contact.AdvLat / 1000000.0
        Longitude = $contact.AdvLon / 1000000.0
        LastAdvert = [DateTimeOffset]::FromUnixTimeSeconds($contact.LastAdvert).LocalDateTime
        PathLength = $contact.OutPathLen
    }
}

# Export to CSV
$contactList | Export-Csv -Path $OutputFile -NoTypeInformation

Write-Host "Contacts exported to: $OutputFile" -ForegroundColor Green

# Display summary
$contactList | Format-Table -AutoSize

$connection.Close()
```

### Example 5: Bulk Import Contacts

```powershell
# ImportContacts.ps1
# Imports contacts from a CSV file

param(
    [string]$ComPort = "COM3",
    [string]$InputFile = "contacts.csv"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

if (-not (Test-Path $InputFile)) {
    Write-Host "File not found: $InputFile" -ForegroundColor Red
    exit 1
}

Write-Host "Importing contacts from: $InputFile" -ForegroundColor Cyan

$contacts = Import-Csv -Path $InputFile
$successCount = 0
$failCount = 0

foreach ($contact in $contacts) {
    Write-Host "Adding: $($contact.Name)..." -NoNewline
    
    try {
        # Convert public key from hex string
        $pubKeyHex = $contact.PublicKey -replace '-', ''
        $pubKeyBytes = [byte[]]::new($pubKeyHex.Length / 2)
        for ($i = 0; $i -lt $pubKeyHex.Length; $i += 2) {
            $pubKeyBytes[$i / 2] = [Convert]::ToByte($pubKeyHex.Substring($i, 2), 16)
        }
        
        # Prepare path
        $outPath = [byte[]]::new(64)
        
        # Add contact
        $connection.sendCommandAddUpdateContact(
            $pubKeyBytes,
            [byte]$contact.Type,
            0,
            0,
            $outPath,
            $contact.Name,
            0,
            [uint]($contact.Latitude * 1000000),
            [uint]($contact.Longitude * 1000000)
        )
        
        Start-Sleep -Milliseconds 200
        Write-Host " OK" -ForegroundColor Green
        $successCount++
        
    } catch {
        Write-Host " FAILED: $_" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`nImport complete: $successCount succeeded, $failCount failed"

$connection.Close()
```

---

## Messaging Scripts

### Example 6: Send Message to Contact

```powershell
# SendMessage.ps1
# Send a text message to a contact

param(
    [Parameter(Mandatory=$true)]
    [string]$ContactName,
    
    [Parameter(Mandatory=$true)]
    [string]$Message,
    
    [string]$ComPort = "COM3"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Looking for contact: $ContactName..." -ForegroundColor Cyan

# Find contact
$contact = $connection.findContactByName($ContactName)

if ($null -eq $contact) {
    Write-Host "Contact not found: $ContactName" -ForegroundColor Red
    $connection.Close()
    exit 1
}

Write-Host "Found: $($contact.AdvName)"
Write-Host "Sending message..." -NoNewline

# Send message
$result = $connection.sendTextMessage(
    $contact.PublicKey,
    $Message,
    [meshcore_lib.Constants+TxtType]::Plain
)

if ($null -ne $result) {
    Write-Host " OK" -ForegroundColor Green
    Write-Host "Queue position: $($result.QueuePos)"
    Write-Host "Estimated timeout: $($result.EstTimeout)ms"
} else {
    Write-Host " FAILED" -ForegroundColor Red
}

$connection.Close()
```

### Example 7: Message Receiver with Auto-Reply

```powershell
# MessageReceiver.ps1
# Listens for incoming messages and auto-replies

param(
    [string]$ComPort = "COM3",
    [string]$AutoReplyMessage = "Message received, thank you!"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Starting message receiver..." -ForegroundColor Cyan
Write-Host "Auto-reply: $AutoReplyMessage`n"

# Message received handler
$messageHandler = {
    param($sender, $msg)
    
    $timestamp = Get-Date -Format "HH:mm:ss"
    $senderKey = [BitConverter]::ToString($msg.PubKeyPrefix)
    
    Write-Host "[$timestamp] Message from $senderKey" -ForegroundColor Green
    Write-Host "  Text: $($msg.Text)"
    Write-Host "  Type: $($msg.TxtType)"
    
    # Find sender contact
    $contact = $connection.findContactByPublicKeyPrefix($msg.PubKeyPrefix)
    
    if ($null -ne $contact) {
        Write-Host "  Sender: $($contact.AdvName)"
        
        # Send auto-reply
        Write-Host "  Sending auto-reply..." -NoNewline
        $result = $connection.sendTextMessage(
            $contact.PublicKey,
            $AutoReplyMessage,
            [meshcore_lib.Constants+TxtType]::Plain
        )
        
        if ($null -ne $result) {
            Write-Host " OK" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
}

$connection.add_ContactMsgRecv($messageHandler)

# Check for waiting messages
Write-Host "Checking for waiting messages..."
$messages = $connection.getWaitingMessages()
Write-Host "Found $($messages.Count) waiting messages`n"

Write-Host "Listening for messages. Press Ctrl+C to stop.`n"

# Keep running
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    $connection.Close()
}
```

---

## Monitoring and Automation Scripts

### Example 8: Network Activity Logger

```powershell
# NetworkLogger.ps1
# Logs all network activity to a file

param(
    [string]$ComPort = "COM3",
    [string]$LogFile = "mesh-network.log"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] $Message"
    Add-Content -Path $LogFile -Value $logEntry
    Write-Host $logEntry
}

Write-Log "Network logger started"

# Advertisement received handler
$advertHandler = {
    param($sender, $pubKey)
    $keyStr = [BitConverter]::ToString($pubKey)
    Write-Log "ADVERT: $keyStr"
}
$connection.add_AdvertPush($advertHandler)

# New device handler
$newDeviceHandler = {
    param($sender, $advert)
    Write-Log "NEW_DEVICE: $($advert.Contact.AdvName) ($([BitConverter]::ToString($advert.Contact.PublicKey)))"
}
$connection.add_NewAdvertPush($newDeviceHandler)

# Path updated handler
$pathHandler = {
    param($sender, $pubKey)
    $keyStr = [BitConverter]::ToString($pubKey)
    Write-Log "PATH_UPDATE: $keyStr"
}
$connection.add_PathUpdatedPush($pathHandler)

# Message received handlers
$contactMsgHandler = {
    param($sender, $msg)
    Write-Log "MSG_CONTACT: From $([BitConverter]::ToString($msg.PubKeyPrefix)): $($msg.Text)"
}
$connection.add_ContactMsgRecv($contactMsgHandler)

$channelMsgHandler = {
    param($sender, $msg)
    Write-Log "MSG_CHANNEL: Ch$($msg.ChannelIdx): $($msg.Text)"
}
$connection.add_ChannelMsgRecv($channelMsgHandler)

# Send confirmed handler
$confirmedHandler = {
    param($sender, $payload)
    Write-Log "SEND_CONFIRMED: Tag=$($payload.Tag), Timestamp=$($payload.Timestamp)"
}
$connection.add_SendConfirmedPush($confirmedHandler)

Write-Host "`nLogging to: $LogFile"
Write-Host "Press Ctrl+C to stop.`n"

try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    Write-Log "Network logger stopped"
    $connection.Close()
}
```

### Example 9: Scheduled Message Broadcaster

```powershell
# ScheduledBroadcast.ps1
# Broadcasts messages on a schedule

param(
    [string]$ComPort = "COM3",
    [int]$IntervalMinutes = 60,
    [string]$Message = "Automated broadcast from PowerShell"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Scheduled broadcaster starting..." -ForegroundColor Cyan
Write-Host "Interval: Every $IntervalMinutes minutes"
Write-Host "Message: $Message`n"

$broadcastCount = 0

try {
    while ($true) {
        $broadcastCount++
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        
        Write-Host "[$timestamp] Broadcast #$broadcastCount" -ForegroundColor Yellow
        
        # Get all contacts
        $contacts = $connection.getContacts()
        Write-Host "  Sending to $($contacts.Count) contacts..."
        
        $successCount = 0
        foreach ($contact in $contacts) {
            try {
                $result = $connection.sendTextMessage(
                    $contact.PublicKey,
                    "$Message (Broadcast #$broadcastCount)",
                    [meshcore_lib.Constants+TxtType]::Plain
                )
                
                if ($null -ne $result) {
                    $successCount++
                }
            } catch {
                Write-Host "    Failed to send to $($contact.AdvName): $_" -ForegroundColor Red
            }
            
            Start-Sleep -Milliseconds 100
        }
        
        Write-Host "  Sent to $successCount/$($contacts.Count) contacts" -ForegroundColor Green
        
        # Wait for next broadcast
        $nextBroadcast = (Get-Date).AddMinutes($IntervalMinutes)
        Write-Host "  Next broadcast at: $($nextBroadcast.ToString('HH:mm:ss'))`n"
        
        Start-Sleep -Seconds ($IntervalMinutes * 60)
    }
} finally {
    Write-Host "`nTotal broadcasts: $broadcastCount"
    $connection.Close()
}
```

---

## Advanced Use Cases

### Example 10: Remote Command Executor

```powershell
# RemoteCommandExecutor.ps1
# Executes commands received via mesh messages

param(
    [string]$ComPort = "COM3",
    [string[]]$AllowedCommands = @("status", "battery", "contacts", "location")
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Remote command executor started" -ForegroundColor Cyan
Write-Host "Allowed commands: $($AllowedCommands -join ', ')`n"

function Execute-MeshCommand {
    param(
        [string]$Command,
        [byte[]]$SenderKey
    )
    
    $contact = $connection.findContactByPublicKeyPrefix($SenderKey)
    $senderName = if ($contact) { $contact.AdvName } else { "Unknown" }
    
    Write-Host "Executing command '$Command' from $senderName..." -ForegroundColor Yellow
    
    $response = switch ($Command.ToLower()) {
        "status" {
            $info = $connection.getSelfInfo()
            "Status: Online. Device: $($info.AdvName)"
        }
        "battery" {
            # Need to wait for response
            "Battery status requested"
        }
        "contacts" {
            $contacts = $connection.getContacts()
            "Total contacts: $($contacts.Count)"
        }
        "location" {
            $info = $connection.getSelfInfo()
            $lat = $info.AdvLat / 1000000.0
            $lon = $info.AdvLon / 1000000.0
            "Location: $lat, $lon"
        }
        default {
            "Unknown command: $Command"
        }
    }
    
    # Send response back
    if ($contact) {
        $connection.sendTextMessage(
            $contact.PublicKey,
            $response,
            [meshcore_lib.Constants+TxtType]::Plain
        )
    }
    
    Write-Host "  Response: $response" -ForegroundColor Green
}

# Message handler
$cmdHandler = {
    param($sender, $msg)
    
    $text = $msg.Text.Trim()
    
    # Check if it's a command (starts with /)
    if ($text.StartsWith("/")) {
        $command = $text.Substring(1)
        
        if ($AllowedCommands -contains $command.ToLower()) {
            Execute-MeshCommand -Command $command -SenderKey $msg.PubKeyPrefix
        } else {
            Write-Host "Rejected unauthorized command: $command" -ForegroundColor Red
        }
    }
}

$connection.add_ContactMsgRecv($cmdHandler)

Write-Host "Listening for commands. Press Ctrl+C to stop.`n"

try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    $connection.Close()
}
```

### Example 11: Mesh Network Mapper

```powershell
# NetworkMapper.ps1
# Creates a visual map of the mesh network

param(
    [string]$ComPort = "COM3",
    [string]$OutputFile = "network-map.html"
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

Write-Host "Mapping mesh network..." -ForegroundColor Cyan

# Get self info
$self = $connection.getSelfInfo()
$selfLat = $self.AdvLat / 1000000.0
$selfLon = $self.AdvLon / 1000000.0

# Get all contacts
$contacts = $connection.getContacts()

Write-Host "Found $($contacts.Count) nodes in the network"

# Build HTML map
$html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Mesh Network Map</title>
    <style>
        body { font-family: Arial; margin: 20px; }
        .node { margin: 10px; padding: 10px; border: 1px solid #ccc; border-radius: 5px; }
        .self { background-color: #d4edda; }
        .contact { background-color: #f8f9fa; }
        h1 { color: #333; }
    </style>
</head>
<body>
    <h1>Mesh Network Map</h1>
    <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    
    <div class="node self">
        <h3>🟢 $($self.AdvName) (Self)</h3>
        <p>Location: $selfLat, $selfLon</p>
        <p>Frequency: $($self.RadioFreq) Hz</p>
        <p>Public Key: $([BitConverter]::ToString($self.PublicKey))</p>
    </div>
    
    <h2>Network Contacts ($($contacts.Count))</h2>
"@

foreach ($contact in $contacts) {
    $lat = $contact.AdvLat / 1000000.0
    $lon = $contact.AdvLon / 1000000.0
    $lastSeen = [DateTimeOffset]::FromUnixTimeSeconds($contact.LastAdvert).LocalDateTime
    
    $html += @"
    <div class="node contact">
        <h3>📡 $($contact.AdvName)</h3>
        <p>Type: $($contact.Type)</p>
        <p>Location: $lat, $lon</p>
        <p>Path Length: $($contact.OutPathLen) hops</p>
        <p>Last Seen: $lastSeen</p>
        <p>Public Key: $([BitConverter]::ToString($contact.PublicKey))</p>
    </div>
"@
}

$html += @"
</body>
</html>
"@

$html | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "Network map saved to: $OutputFile" -ForegroundColor Green
Start-Process $OutputFile

$connection.Close()
```

### Example 12: Automated Contact Sync with Backup

```powershell
# ContactSyncBackup.ps1
# Automatically syncs and backs up contacts

param(
    [string]$ComPort = "COM3",
    [string]$BackupDir = ".\ContactBackups",
    [int]$BackupIntervalHours = 24
)

Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"

# Ensure backup directory exists
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}

Write-Host "Contact sync and backup service started" -ForegroundColor Cyan
Write-Host "Backup directory: $BackupDir"
Write-Host "Backup interval: Every $BackupIntervalHours hours`n"

function Backup-Contacts {
    param($Connection)
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $filename = Join-Path $BackupDir "contacts-$timestamp.json"
    
    Write-Host "Creating backup..." -NoNewline
    
    $contacts = $Connection.getContacts()
    
    $backup = @{
        Timestamp = $timestamp
        ContactCount = $contacts.Count
        Contacts = @()
    }
    
    foreach ($contact in $contacts) {
        $backup.Contacts += @{
            Name = $contact.AdvName
            PublicKey = [BitConverter]::ToString($contact.PublicKey)
            Type = $contact.Type
            Latitude = $contact.AdvLat / 1000000.0
            Longitude = $contact.AdvLon / 1000000.0
            LastAdvert = $contact.LastAdvert
            PathLength = $contact.OutPathLen
        }
    }
    
    $backup | ConvertTo-Json -Depth 10 | Out-File -FilePath $filename
    
    Write-Host " OK" -ForegroundColor Green
    Write-Host "Backed up $($contacts.Count) contacts to: $filename"
    
    # Clean up old backups (keep last 30 days)
    $cutoffDate = (Get-Date).AddDays(-30)
    Get-ChildItem -Path $BackupDir -Filter "contacts-*.json" |
        Where-Object { $_.CreationTime -lt $cutoffDate } |
        Remove-Item -Force
}

# Connection setup...
# $connection = New-Object PsBleConnection -ArgumentList $ComPort

# New contact handler
$newContactHandler = {
    param($sender, $advert)
    Write-Host "New contact discovered: $($advert.Contact.AdvName)" -ForegroundColor Yellow
    Backup-Contacts -Connection $sender
}
$connection.add_NewAdvertPush($newContactHandler)

# Initial backup
Backup-Contacts -Connection $connection

try {
    while ($true) {
        Start-Sleep -Seconds ($BackupIntervalHours * 3600)
        Backup-Contacts -Connection $connection
    }
} finally {
    $connection.Close()
}
```

---

## Running the Scripts

### Basic Execution

```powershell
# Run with default parameters
.\SimpleBleConnection.ps1 -DeviceAddress "COM3"

# Run with custom parameters
.\SendMessage.ps1 -ContactName "Alice" -Message "Hello!" -ComPort "COM4"
```

### Running as a Background Job

```powershell
# Start as background job
Start-Job -ScriptBlock {
    & ".\NetworkLogger.ps1" -ComPort "COM3"
}

# Check job status
Get-Job

# Get job output
Receive-Job -Id 1

# Stop job
Stop-Job -Id 1
Remove-Job -Id 1
```

### Scheduled Task Setup

```powershell
# Create scheduled task to run script daily
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" `
    -Argument "-File C:\Scripts\ScheduledBroadcast.ps1"

$trigger = New-ScheduledTaskTrigger -Daily -At 9am

Register-ScheduledTask -TaskName "MeshBroadcast" `
    -Action $action `
    -Trigger $trigger `
    -Description "Daily mesh network broadcast"
```

---

## Tips and Best Practices

### Error Handling

```powershell
# Always wrap in try/catch
try {
    $connection = New-Object PsBleConnection -ArgumentList "COM3"
    # ... operations ...
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    # Log error
    $_ | Out-File -Append -FilePath "error.log"
} finally {
    # Always cleanup
    if ($connection) {
        $connection.Close()
    }
}
```

### Logging

```powershell
# Create a logging function
function Write-MeshLog {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    Add-Content -Path "mesh.log" -Value $logMessage
    
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }
    
    Write-Host $logMessage -ForegroundColor $color
}
```

### Performance Considerations

```powershell
# Use throttling for bulk operations
$contacts = $connection.getContacts()
$contacts | ForEach-Object -ThrottleLimit 5 -Parallel {
    # Process contacts in parallel with limit
}

# Add delays between operations
foreach ($contact in $contacts) {
    # ... operation ...
    Start-Sleep -Milliseconds 100  # Prevent overwhelming the device
}
```

---

## Troubleshooting

### Common Issues

**Issue**: "Cannot find type [meshcore_lib.Connection]"
```powershell
# Solution: Ensure the DLL is loaded
Add-Type -Path ".\meshcore-lib\bin\Debug\net9.0\meshcore-lib.dll"
```

**Issue**: Serial port access denied
```powershell
# Solution: Run PowerShell as Administrator
# Or: Close other applications using the port
```

**Issue**: Events not firing
```powershell
# Solution: Ensure connection is established
$connection.onConnected()

# Give events time to register
Start-Sleep -Milliseconds 500
```

---

**Last Updated**: 2024  
**Library Version**: 1.0.0
