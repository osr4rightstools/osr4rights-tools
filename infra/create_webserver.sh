#!/bin/sh

# Creates Ubuntu webserver to run osr4rightstools.org 

# disable auto upgrades by apt - in dev mode only
cd /home/dave

cat <<EOT >> 20auto-upgrades
APT::Periodic::Update-Package-Lists "0";
APT::Periodic::Download-Upgradeable-Packages "0";
APT::Periodic::AutocleanInterval "0";
APT::Periodic::Unattended-Upgrade "1";
EOT

# sudo cp /home/dave/20auto-upgrades /etc/apt/apt.conf.d/20auto-upgrades
sudo mv /home/dave/20auto-upgrades /etc/apt/apt.conf.d/20auto-upgrades

# go with newer apt which gets dependency updates too (like linux-azure)
sudo apt update -y
sudo apt upgrade -y
  
# Install .NET 5 on Ubutu 20.04 LTS
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# nginx
sudo apt-get install nginx -y

sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-5.0

# create document root for published files 
sudo mkdir /var/www

# create gitsource folder and clone
sudo mkdir /gitsource
cd /gitsource
sudo git clone https://github.com/osr4rightstools/osr4rights-tools .

# nginx config
# ssl certs will already be in /certs
# copied in with create-kestrel-osr-with-secrets.sh file
sudo cp /gitsource/infra/nginx.conf /etc/nginx/sites-available/default
sudo nginx -s reload

# compile and publish the web app
sudo dotnet publish /gitsource/src/OSR4Rights.Web --configuration Release --output /var/www

# change ownership of the published files to what it will run under
sudo chown -R www-data:www-data /var/www
# allow exective permissions
sudo chmod +x /var/www

# cookie keys to allow machine to restart and for it to 'remember' cookies
# todo - store these in blob storage?
sudo mkdir /var/osr-cookie-keys
sudo chown -R www-data:www-data /var/osr-cookie-keys
# allow read and write
sudo chmod +rw /var/osr-cookie-keys

# fileStores
sudo mkdir /tusFileStore
sudo chown -R www-data:www-data /tusFileStore
# todo - make less
# sudo chmod +rwx /tusFileStore
# sudo chmod +rw /tusFileStore

sudo mkdir /osrFileStore
sudo chown -R www-data:www-data /osrFileStore
# todo - make less
# sudo chmod +rwx /osrFileStore

# auto start on machine reboot
sudo systemctl enable kestrel-osr.service

# start the Kestrel web app using systemd using kestrel-blc.service text files
sudo systemctl start kestrel-osr.service

# https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0#configure-the-firewall
sudo apt-get install ufw

sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

sudo ufw enable

  # a nice shortcut sym link
# sudo ln -s /usr/local/openresty/nginx/ /home/dave/nginx
sudo ln -s /var/www/logs/ /home/dave/logs

# sudo snap install bpytop

