$content = [System.IO.File]::ReadAllText("Work\SampleNet\CSample.cs")
$bytes = [System.IO.File]::ReadAllBytes("Work\SampleNet\CSample.cs")

# Find the method
$methodStart = $content.IndexOf("if (dimPd.IsEmpty())")
if ($methodStart -eq -1) {
    Write-Host "Method not found"
    exit
}

Write-Host "Method found at position: $methodStart"

# Check bytes around the method
$start = [Math]::Max(0, $methodStart - 30)
$end = [Math]::Min($bytes.Length, $methodStart + 80)

Write-Host "Checking bytes $start to $end"
for ($i = $start; $i -lt $end; $i++) {
    $b = $bytes[$i]
    if ($b -eq 13) {
        Write-Host -NoNewline "\r"
    } elseif ($b -eq 10) {
        Write-Host -NoNewline "\n"
    } elseif ($b -ge 32 -and $b -le 126) {
        Write-Host -NoNewline ([char]$b)
    } else {
        Write-Host -NoNewline "[$($b.ToString('X2'))]"
    }
}
Write-Host ""

# Count line endings
$crlfCount = 0
$lfOnlyCount = 0

for ($i = 0; $i -lt $content.Length; $i++) {
    if ($content[$i] -eq "`n") {
        if ($i -gt 0 -and $content[$i - 1] -eq "`r") {
            $crlfCount++
        } else {
            $lfOnlyCount++
        }
    }
}

Write-Host "CRLF count: $crlfCount"
Write-Host "LF-only count: $lfOnlyCount"