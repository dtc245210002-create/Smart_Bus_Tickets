$urls = @(
    '/',
    '/Account/Login',
    '/Account/Register',
    '/Ticket/Detail',
    '/Ticket/Reschedule',
    '/Trip/Search',
    '/Driver/Login',
    '/api/ticket-notification/preview-sms-zalo',
    '/api/ticket-notification/generate-qr',
    '/api/ticket-notification/preview-email'
)

foreach ($u in $urls) {
    $fullUrl = "http://localhost:5122$u"
    try {
        $resp = Invoke-WebRequest -Uri $fullUrl -UseBasicParsing -TimeoutSec 5
        Write-Host "[$($resp.StatusCode)] $u" -ForegroundColor Green
    } catch {
        $status = $_.Exception.Response.StatusCode.value__
        Write-Host "[$status] $u - $($_.Exception.Message)" -ForegroundColor Red
    }
}
