$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq "CN=Nesplay" } | Select-Object -First 1
if ($cert) {
    Set-AuthenticodeSignature -FilePath "c:\Users\monch\nesplay\publish\Contra.exe" -Certificate $cert -TimestampServer "http://timestamp.digicert.com"
    Write-Host "Signed successfully!"
} else {
    Write-Host "Certificate not found!"
}
