$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Nesplay" -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5)
$pwd = ConvertTo-SecureString -String "nesplay123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "c:\Users\monch\nesplay\nesplay_cert.pfx" -Password $pwd
Write-Host "Certificate thumbprint: $($cert.Thumbprint)"
