#!/bin/sh


# Make the directory to put in SSL certs
sudo mkdir /certs
# sudo chown -R www-data:www-data /certs
sudo chown -R dave:dave /certs
# allow exective permissions
# sudo chmod +x /var/www
sudo chmod +rw /certs


cd /home/dave

# To debug Env variables on live remember to login as www-data
# sudo -u www-data bash

# EOT is End of Transmission
cat <<EOT >> kestrel-osr.service
[Unit]
Description=Website running on ASP.NET 5 

[Service]
WorkingDirectory=/var/www
ExecStart=/usr/bin/dotnet OSR4Rights.Web.dll --urls "http://*:5000"

Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10

# copied from dotnet documentation at
# https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.1#code-try-7
KillSignal=SIGINT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

Environment=AZURE_CLIENT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Environment=AZURE_TENANT_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Environment=AZURE_CLIENT_SECRET=xxxxxxxxxx-xxxx-xxxxxxxxxxxxxxxxxx
Environment=AZURE_SUBSCRIPTION_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

Environment='DB_CONNECTION_STRING=Server=xxxxxxxxx.database.windows.net;Database=osr4rights;User Id=xxxx;Password=xxxxxxxxxxxxxxxxxxx;'

SyslogIdentifier=dotnet-OSR4Rights.Web
User=www-data

[Install]
WantedBy=multi-user.target
EOT

# environmental variable secrets
sudo cp /home/dave/kestrel-osr.service /etc/systemd/system/kestrel-osr.service
# sudo mv /home/dave/kestrel-osr.service /etc/systemd/system/kestrel-osr.service

# for some reason mv didn't move, only copied
sudo rm -f /home/dave/kestrel-osr.service

# Fileshare
sudo mkdir /mnt/osrshare

# look in portal.azure.com
# storage account, connect, linux - password is in there
sudo mount -t cifs //osrstorageaccount.file.core.windows.net/osrshare /mnt/osrshare -o username=osrstorageaccount,password=xxxxxxxxxxxx,serverino,noperm
