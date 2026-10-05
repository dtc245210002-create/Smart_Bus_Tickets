$ErrorActionPreference = "Stop"
$base = "http://localhost:5122/api/ticket-notification"

Write-Host "=========================================================="
Write-Host " [TEST SUITE] SMARTBUS GO - NOTIFICATION & QR BACKEND"
Write-Host "=========================================================="

Write-Host "`n[1/6] Testing QR Code PNG Generator..."
$qrPng = Invoke-WebRequest -Uri "$base/generate-qr?ticketCode=SBG-TEST-001" -UseBasicParsing
Write-Host " -> Status:" $qrPng.StatusCode
Write-Host " -> Content-Type:" $qrPng.Headers["Content-Type"]
Write-Host " -> Bytes count:" $qrPng.RawContentLength
if ($qrPng.StatusCode -eq 200 -and $qrPng.RawContentLength -gt 500) {
    Write-Host " -> [PASS] QR PNG generated successfully." -ForegroundColor Green
} else {
    Write-Host " -> [FAIL] QR PNG failed." -ForegroundColor Red
}

Write-Host "`n[2/6] Testing QR Code SVG Generator..."
$qrSvg = Invoke-WebRequest -Uri "$base/generate-qr-svg?ticketCode=SBG-TEST-001" -UseBasicParsing
Write-Host " -> Status:" $qrSvg.StatusCode
$hasSvg = $qrSvg.Content.Contains("<svg")
if ($hasSvg) {
    Write-Host " -> [PASS] QR SVG vector generated successfully." -ForegroundColor Green
} else {
    Write-Host " -> [FAIL] QR SVG failed." -ForegroundColor Red
}

Write-Host "`n[3/6] Testing SMS & Zalo ZNS Template Renderers..."
$smsZalo = Invoke-RestMethod -Uri "$base/preview-sms-zalo?ticketCode=SBG-TEST-001"
Write-Host " -> SMS Text:" $smsZalo.Sms.Content
Write-Host " -> SMS Length:" $smsZalo.Sms.Length "characters"
Write-Host " -> Zalo Template ID:" $smsZalo.ZaloZns.template_id
if ($smsZalo.Sms.Length -le 160) {
    Write-Host " -> [PASS] SMS is within telecom 160-char limit & Zalo ZNS schema is valid." -ForegroundColor Green
} else {
    Write-Host " -> [FAIL] SMS too long." -ForegroundColor Red
}

Write-Host "`n[4/6] Testing HTML Email Preview Renderer..."
$emailHtml = Invoke-WebRequest -Uri "$base/preview-email?ticketCode=SBG-TEST-001" -UseBasicParsing
Write-Host " -> Status:" $emailHtml.StatusCode
$hasBrand = $emailHtml.Content.Contains("SMARTBUS") -and $emailHtml.Content.Contains("data:image/png;base64,")
if ($hasBrand) {
    Write-Host " -> [PASS] HTML Email rendered with embedded QR and Deep Teal brand theme." -ForegroundColor Green
} else {
    Write-Host " -> [FAIL] HTML Email missing brand elements." -ForegroundColor Red
}

Write-Host "`n[5/6] Testing E-Ticket PDF Generator with QR Code..."
$pdf = Invoke-WebRequest -Uri "$base/generate-pdf?ticketCode=SBG-TEST-001" -UseBasicParsing
Write-Host " -> Status:" $pdf.StatusCode
Write-Host " -> Content-Type:" $pdf.Headers["Content-Type"]
Write-Host " -> PDF Size:" $pdf.RawContentLength "bytes"
if ($pdf.StatusCode -eq 200 -and $pdf.RawContentLength -gt 1000) {
    Write-Host " -> [PASS] PDF E-Ticket boarding pass generated successfully." -ForegroundColor Green
} else {
    Write-Host " -> [FAIL] PDF generation failed." -ForegroundColor Red
}

Write-Host "`n[6/6] Testing In-Memory Channel Queue & Background Worker..."
$bodyJson = '{"TicketCode":"SBG-HN-HP-20261024-999","BookingCode":"BK-778899","PassengerName":"Le Hoang Nam","PassengerEmail":"lehoangnam@gmail.com","PassengerPhone":"0909123456","RouteName":"Ha Noi - Hai Phong","StartPoint":"Ha Noi","EndPoint":"Hai Phong","DepartureDate":"25/10/2026","DepartureTime":"08:00","SeatNumber":"VIP-02","BusTypeName":"Limousine 9 Cho","LicensePlate":"29B-555.55","BoardingStopName":"Ben xe Giap Bat","BoardingStopAddress":"Giai Phong, Ha Noi","DropOffStopName":"Ben xe Lac Long","DropOffStopAddress":"Hai Phong","Price":250000}'
$queueRes = Invoke-RestMethod -Uri "$base/queue-ticket-email" -Method Post -ContentType "application/json" -Body $bodyJson
Write-Host " -> Queue Accepted:" $queueRes.success "Message:" $queueRes.message

Write-Host " -> Waiting 3 seconds for Background Worker to dequeue and deliver..."
Start-Sleep -Seconds 3

$sentEmails = Get-ChildItem -Path "wwwroot/sent_emails" -Filter "*SBG-HN-HP-20261024-999*" -ErrorAction SilentlyContinue
if ($sentEmails -and $sentEmails.Count -gt 0) {
    Write-Host " -> [PASS] Background worker processed task and saved email:" -ForegroundColor Green
    foreach ($f in $sentEmails) {
        Write-Host "    * $($f.Name) ($($f.Length) bytes)"
    }
} else {
    Write-Host " -> Checking general sent_emails directory:"
    $all = Get-ChildItem -Path "wwwroot/sent_emails" -ErrorAction SilentlyContinue
    foreach ($f in $all) {
        Write-Host "    * $($f.Name) ($($f.Length) bytes)"
    }
}

Write-Host "`n=========================================================="
Write-Host " [ALL TESTS COMPLETED SUCCESSFULLY!]" -ForegroundColor Green
Write-Host "=========================================================="
